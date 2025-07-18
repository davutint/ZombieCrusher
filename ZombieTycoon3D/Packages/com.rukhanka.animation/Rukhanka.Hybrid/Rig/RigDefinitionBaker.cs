using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using FixedStringName = Unity.Collections.FixedString512Bytes;
using System.Reflection;
using System.Collections.Generic;
using System;
using Unity.Assertions;
using Unity.Mathematics;
using Rukhanka.Toolbox;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[TemporaryBakingType]
internal struct BoneEntitiesToRemove : IBufferElementData
{
	public Entity boneEntity;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[TemporaryBakingType]
internal struct BoneEntityRef: IBufferElementData
{
	public Entity boneEntity;
	public int rigBoneIndex;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal class InternalSkeletonBone
{
	public Transform boneTransform;
	public string name;
	public string parentName;
	public Vector3 position;
	public Quaternion rotation;
	public Vector3 scale;
}

//=================================================================================================================//

public partial class RigDefinitionBaker: Baker<RigDefinitionAuthoring>
{
	static FieldInfo parentBoneNameField;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static RigDefinitionBaker()
	{
		parentBoneNameField = typeof(SkeletonBone).GetField("parentName", BindingFlags.NonPublic | BindingFlags.Instance);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public override void Bake(RigDefinitionAuthoring a)
	{
		var e = GetEntity(TransformUsageFlags.Dynamic);
		
		var animator = GetComponent<Animator>();
		if (a.rigConfigSource == RigDefinitionAuthoring.RigConfigSource.FromAnimator)
		{
			Assert.IsNotNull(animator, "Rig is configured to use parameters from Unity.Animator, but no one found. Please switch to manual configuration mode, or attach Animator to the authoring GameObject.");
			a.avatar = animator.avatar;
			a.animationCulling = animator.cullingMode != AnimatorCullingMode.AlwaysAnimate;
			a.applyRootMotion = animator.applyRootMotion;
		}
		
		DependsOn(a.avatar);
		
		AddBuffer<AnimationToProcessComponent>(e);
		CreateRigDefinitionFromRigAuthoring(e, a);	
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	InternalSkeletonBone CreateSkeletonBoneFromTransform(Transform t, string parentName)
	{
		var bone = new InternalSkeletonBone();
		bone.boneTransform = t;
		bone.name = t.name;
		bone.position = t.localPosition;
		bone.rotation = t.localRotation;
		bone.scale = t.localScale;
		bone.parentName = parentName;
		return bone;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void TransformHierarchyWalk(Transform parent, List<InternalSkeletonBone> sb)
	{
		for (int i = 0; i < parent.childCount; ++i)
		{
			var c = parent.GetChild(i);
			var ct = c.transform;
			var bone = CreateSkeletonBoneFromTransform(ct, parent.name);
			sb.Add(bone);

			TransformHierarchyWalk(ct, sb);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	List<InternalSkeletonBone> CreateAvatarFromObjectHierarchy(GameObject root)
	{
		//	Manually fill all bone transforms
		var sb = new List<InternalSkeletonBone>();
		var rootBone = CreateSkeletonBoneFromTransform(root.transform, "");
		sb.Add(rootBone);

		TransformHierarchyWalk(root.transform, sb);
		return sb;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int GetRigRootBoneIndex(Avatar avatar, List<InternalSkeletonBone> rigBones)
	{
		if (avatar == null)
			return 0;
		
		var rootBoneName = avatar.GetRootMotionNodeName();
		if (avatar.isHuman)
		{
			var hd = avatar.humanDescription;
			var humanBoneIndexInDesc = Array.FindIndex(hd.human, x => x.humanName == "Hips");
			rootBoneName = hd.human[humanBoneIndexInDesc].boneName;
		}
		var rv = rigBones.FindIndex(x => x.name == rootBoneName);
		return math.max(rv, 0);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int DeepestHierarchyBoneCount(in BlobBuilderArray<RigBoneInfo> rigBones)
	{
		var rv = 0;
		for (var i = 0; i < rigBones.Length; ++i)
		{
			var numBonesInHierarchy = 1;
			var curBoneIndex = rigBones[i].parentBoneIndex;
			while (curBoneIndex >= 0)
			{
				curBoneIndex = rigBones[curBoneIndex].parentBoneIndex;
				++numBonesInHierarchy;
			}
			rv = math.max(rv, numBonesInHierarchy);
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	bool IsBoneInOptimizationMask(string skeletonBonesPath, AvatarMask mask)
	{
		if (mask == null)
			return true;
		
		for (var i = 0; i < mask.transformCount; ++i)
		{
			var maskPath = mask.GetTransformPath(i);
			var pathActive = mask.GetTransformActive(i);
			if (maskPath == skeletonBonesPath)
				return pathActive;
		}
		return true;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	string[] MakeSkeletonBonesFullPaths(SkeletonBone[] skeletonBones)
	{
		var rv = new string[skeletonBones.Length];
		if (skeletonBones.Length == 0)
			return rv;
		
		//	First bone need to be empty, because it contains root transform name, but avatar masks have it empty string
		rv[0] = "";
		for (var i = 1; i < skeletonBones.Length; ++i)
		{
			var sb = skeletonBones[i];
			var parentName = (string)parentBoneNameField.GetValue(sb);
			var parentIndex = Array.FindIndex(skeletonBones, 0, i, x => x.name == parentName);
			var fullName = sb.name;
			if (parentIndex > 0)
				fullName = rv[parentIndex] + "/" + sb.name;
			rv[i] = fullName;
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	List<InternalSkeletonBone> CreateInternalRigRepresentation(Avatar avatar, RigDefinitionAuthoring rigDef)
	{
		if (avatar == null)
		{
			return CreateAvatarFromObjectHierarchy(rigDef.gameObject);
		}
		
		var skeleton = avatar.humanDescription.skeleton;
		
		//	Validate avatar optimization mask
		var rigOptimizationMask = rigDef.avatarOptimizationMask;
		if (rigOptimizationMask != null)
		{
			if (rigOptimizationMask.transformCount != skeleton.Length)
			{
				Debug.LogWarning($"'{rigOptimizationMask.name}' bone count ({rigOptimizationMask.transformCount}) does not match rig avatar '{avatar.name}' bone count ({skeleton.Length}). Avatar mask was created for different avatar and ignored.");
				rigOptimizationMask = null;
			}
		}
		
		var skeletonBonesFullPaths = MakeSkeletonBonesFullPaths(skeleton);
		
		var rv = new List<InternalSkeletonBone>();
		for (var i = 0; i < skeleton.Length; ++i)
		{
			var boneIsObjectRoot = i == 0;
			var sb = skeleton[i];
			
			if (!IsBoneInOptimizationMask(skeletonBonesFullPaths[i], rigOptimizationMask))
				continue;
			
			var isb = new InternalSkeletonBone()
			{
				boneTransform = boneIsObjectRoot ? rigDef.transform : TransformUtils.FindChildRecursively(rigDef.transform, sb.name),
				name = sb.name,
				position = sb.position,
				rotation = sb.rotation,
				scale = sb.scale,
				parentName = (string)parentBoneNameField.GetValue(sb)
			};
			rv.Add(isb);
		}

		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateRigDefinitionFromRigAuthoring(Entity rigEntity, RigDefinitionAuthoring rigDef)
	{
		var avatar = rigDef.avatar;
		
		var skeletonBones = CreateInternalRigRepresentation(avatar, rigDef);
		if (skeletonBones.Count == 0)
		{
			Debug.LogError($"Unity avatar '{avatar.name}' setup is incorrect. Follow <a href=\"https://docs.rukhanka.com/getting_started#rig-definition\">documentation</a> about avatar setup process please.");
			return;
		}
		
		var rv = new RigDefinitionComponent();
		rv.applyRootMotion = rigDef.applyRootMotion;
		
		var rigBlobHash = rigDef.CalculateRigHash();
		var rigBlobExist = TryGetBlobAssetReference<RigDefinitionBlob>(rigBlobHash, out var rigBlob);
		if (!rigBlobExist)
		{
			rigBlob = CreateRigBlob(avatar, rigDef, skeletonBones, rigBlobHash);
			AddBlobAssetWithCustomHash(ref rigBlob, rigBlobHash);
		}
		
		rv.rigBlob = rigBlob;
		AddComponent(rigEntity, rv);
		AddBuffer<RootMotionAnimationStateComponent>(rigEntity);
		var rmvc = new RootMotionVelocityComponent()
		{
			worldVelocity = float3.zero,
			removeBuiltinEntityMovement = rigDef.rootMotionMode == RigDefinitionAuthoring.RootMotionMode.DisableBuiltinMovement
		};
		AddComponent(rigEntity, rmvc);

		if (rigDef.hasAnimationEvents)
		{
			AddBuffer<AnimationEventComponent>(rigEntity);
			AddBuffer<PreviousProcessedAnimationComponent>(rigEntity);
		}
			
		if (rigDef.animationCulling)
		{
			AddComponent<CullAnimationsTag>(rigEntity);	
		}
		
		var isGPUAnimator = rigDef.animationEngine == RigDefinitionAuthoring.AnimationEngine.GPU;
	#if !RUKHANKA_NO_DEFORMATION_SYSTEM
		AddComponent<GPUAnimationEngineTag>(rigEntity);
		SetComponentEnabled<GPUAnimationEngineTag>(rigEntity, isGPUAnimator);
	#endif
		
		ProcessBoneStrippingMask(rigEntity, rigDef, skeletonBones);
		CreateBoneEntityRefs(rigEntity, skeletonBones, rigDef);
	}
	

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateBoneEntityRefs(Entity e, List<InternalSkeletonBone> skeletonBones, RigDefinitionAuthoring rigDef)
	{
		var transformFlags = TransformUsageFlags.Dynamic;
		var manualBoneStripping = rigDef.boneEntityStrippingMode == RigDefinitionAuthoring.BoneEntityStrippingMode.Manual;

		var boneEntityRefArr = AddBuffer<BoneEntityRef>(e);
		for (var i = 0; i < skeletonBones.Count; ++i)
		{
			var boneTransformFlags = transformFlags | (manualBoneStripping && i != 0 ? TransformUsageFlags.WorldSpace : 0);
			var skeletonBone = skeletonBones[i];
			var boneEntity = GetEntityForBone(skeletonBone.boneTransform, boneTransformFlags, rigDef.boneEntityStrippingMode == RigDefinitionAuthoring.BoneEntityStrippingMode.Automatic);
			boneEntityRefArr.Add(new BoneEntityRef() {boneEntity = boneEntity, rigBoneIndex = i});
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CheckForDuplicatedBones(ref RigDefinitionBlob rdb)
	{
		var duplicateBoneChecker = new NativeHashSet<uint>(rdb.bones.Length, Allocator.Temp);
		for (var i = 0; i < rdb.bones.Length; ++i)
		{
			ref var bone = ref rdb.bones[i];
			if (!duplicateBoneChecker.Add(bone.hash))
			{
			#if RUKHANKA_DEBUG_INFO
				Debug.LogError($"RigDefinitionBaker: Duplicate bone with name '{bone.name.ToString()}' in rig '{rdb.name.ToString()}'! This is not allowed!");
			#else
				Debug.LogError($"RigDefinitionBaker: Duplicate bone with hash '{bone.hash}' in rig '{rdb.hash}'! This is not allowed! Enable 'RUKHANKA_DEBUG_INFO' to see bone names.");
			#endif
			}
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	BlobAssetReference<RigDefinitionBlob> CreateRigBlob(Avatar avatar, RigDefinitionAuthoring rigDef, List<InternalSkeletonBone> skeletonBones, Hash128 rigHash)
	{
	#if RUKHANKA_DEBUG_INFO
		var startTimeMarker = Time.realtimeSinceStartupAsDouble;
	#endif
		
		var bb = new BlobBuilder(Allocator.Temp);
		ref var c = ref bb.ConstructRoot<RigDefinitionBlob>();
		c.hash = rigHash;
		c.rootBoneIndex = GetRigRootBoneIndex(avatar, skeletonBones);
		
	#if RUKHANKA_DEBUG_INFO
		var rigName = rigDef.gameObject.name;
		if (rigName.Length > 0)
			bb.AllocateString(ref c.name, rigName);
	#endif

		var bonesArrayBlob = bb.Allocate(ref c.bones, skeletonBones.Count);
		for (int i = 0; i < skeletonBones.Count; ++i)
		{
			ref var boneBlob = ref bonesArrayBlob[i];
			CreateRigBoneBlob(bb, ref boneBlob, skeletonBones, i);
		}
		
		var rigIsHuman = avatar != null && avatar.isHuman;
		if (rigIsHuman)
		{
			CreateHumanoidData(bb, ref c, bonesArrayBlob, avatar, skeletonBones);
		}
		
		
	#if RUKHANKA_DEBUG_INFO
		var dt = Time.realtimeSinceStartupAsDouble - startTimeMarker;
		c.bakingTime = (float)dt;
	#endif
		
		var rv = bb.CreateBlobAssetReference<RigDefinitionBlob>(Allocator.Persistent);
		CheckForDuplicatedBones(ref rv.Value);
        
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateHumanoidData(BlobBuilder bb, ref RigDefinitionBlob rdb, BlobBuilderArray<RigBoneInfo> bonesArr, Avatar avatar, List<InternalSkeletonBone> skeletonBones)
	{
		ref var hdb = ref bb.Allocate(ref rdb.humanData);
		var humanToRigArr = bb.Allocate(ref hdb.humanBoneToSkeletonBoneIndices, (int)HumanBodyBones.LastBone);
		var humanRotArr = bb.Allocate(ref hdb.humanRotData, skeletonBones.Count);
		
		for (int j = 0; j < humanToRigArr.Length; ++j)
			humanToRigArr[j] = -1;

		for (int l = 0; l < humanRotArr.Length; ++l)
		{
			ref var hrd = ref humanRotArr[l];
			
			var humanRigIndex = CreateHumanoidBoneRotationData(ref hrd, avatar, skeletonBones[l].name);
			if (humanRigIndex >= 0)
			{
				humanToRigArr[humanRigIndex] = l;
				//	Make muscle neutral ref pose
				ref var rbi = ref bonesArr[l];
				rbi.refPose.rot = math.mul(hrd.preRot, hrd.postRot);
				//	Set human body part for this bone
				rbi.humanBodyPart = humanPartToAvatarMaskPartRemapTable[humanRigIndex];
			}
		
			SetHumanBodyBodyPartForNonAssignedBones(bonesArr);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	AvatarMaskBodyPart GetAvatarMaskBodyPartFromParent(int boneIndex, BlobBuilderArray<RigBoneInfo> bonesArr)
	{
		if (boneIndex < 0)
			return (AvatarMaskBodyPart)(-1);
		
		ref var rb = ref bonesArr[boneIndex];
		if (rb.humanBodyPart >= 0)
			return rb.humanBodyPart;
		
		return GetAvatarMaskBodyPartFromParent(rb.parentBoneIndex, bonesArr);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void SetHumanBodyBodyPartForNonAssignedBones(BlobBuilderArray<RigBoneInfo> bonesArr)
	{
		//	Root bone is a special case
		bonesArr[0].humanBodyPart = AvatarMaskBodyPart.Root;

		//	For other bones search for parent with body part is set and set it to the same value
		for (int i = 1; i < bonesArr.Length; ++i)
		{
			bonesArr[i].humanBodyPart = GetAvatarMaskBodyPartFromParent(i, bonesArr);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int CreateHumanoidBoneRotationData(ref HumanRotationData hrd, Avatar a, string boneName)
	{
		hrd = HumanRotationData.Identity();
		
		var hd = a.humanDescription;
		var humanBoneInSkeletonIndex = Array.FindIndex(hd.human, x => x.boneName == boneName);
		if (humanBoneInSkeletonIndex < 0)
			return -1;
			
		var humanBones = HumanTrait.BoneName;
		var humanBoneDef = hd.human[humanBoneInSkeletonIndex];
		var humanBoneId = Array.FindIndex(humanBones, x => x == humanBoneDef.humanName);
		Debug.Assert(humanBoneId >= 0);

		hrd.preRot = a.GetPreRotation(humanBoneId);
		hrd.postRot = math.inverse(a.GetPostRotation(humanBoneId));
		hrd.sign = a.GetLimitSign(humanBoneId);

		var minA = humanBoneDef.limit.min;
		var maxA = humanBoneDef.limit.max;
		if (humanBoneDef.limit.useDefaultValues)
		{
			minA.x = HumanTrait.GetMuscleDefaultMin(HumanTrait.MuscleFromBone(humanBoneId, 0));
			minA.y = HumanTrait.GetMuscleDefaultMin(HumanTrait.MuscleFromBone(humanBoneId, 1));
			minA.z = HumanTrait.GetMuscleDefaultMin(HumanTrait.MuscleFromBone(humanBoneId, 2));

			maxA.x = HumanTrait.GetMuscleDefaultMax(HumanTrait.MuscleFromBone(humanBoneId, 0));
			maxA.y = HumanTrait.GetMuscleDefaultMax(HumanTrait.MuscleFromBone(humanBoneId, 1));
			maxA.z = HumanTrait.GetMuscleDefaultMax(HumanTrait.MuscleFromBone(humanBoneId, 2));
		}
		hrd.minMuscleAngles = math.radians(minA);
		hrd.maxMuscleAngles = math.radians(maxA);
		
		return humanBoneId;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe Entity GetEntityForBone(Transform t, TransformUsageFlags boneFlags, bool automaticBoneStripping)
	{
		if (t == null || t.GetComponent<SkinnedMeshRenderer>() != null)
			return Entity.Null;

		var rv = automaticBoneStripping ? _State.BakedEntityData->GetEntity(t) : GetEntity(t, boneFlags);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateRigBoneBlob(BlobBuilder bb, ref RigBoneInfo rbi, List<InternalSkeletonBone> skeletonBones, int boneIndex)
	{
		var boneIsObjectRoot = boneIndex == 0;
		var skeletonBone = skeletonBones[boneIndex];

		var name = skeletonBone.name;
		// Special handling of hierarchy root
		if (boneIsObjectRoot)
		{
			name = SpecialBones.UnnamedRootBoneName;
		}

		var boneName = new FixedStringName(name);
		var boneHash = boneName.CalculateHash32();
		rbi.hash = boneHash;
		rbi.refPose = CreateBoneTransformFromSkeletonBone(skeletonBone);
		rbi.humanBodyPart = (AvatarMaskBodyPart)(-1);
		var parentBoneIndex = skeletonBones.FindIndex(x => x.name == skeletonBone.parentName);
		rbi.parentBoneIndex = parentBoneIndex;
		
#if RUKHANKA_DEBUG_INFO
		if (name.Length > 0)
			bb.AllocateString(ref rbi.name, name);
#endif
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	BoneTransform CreateBoneTransformFromSkeletonBone(InternalSkeletonBone skeletonBone)
	{
		var pose = new BoneTransform()
		{
			pos = skeletonBone.position,
			rot = skeletonBone.rotation,
			scale = skeletonBone.scale,
		};
		
		if (skeletonBone.boneTransform != null)
		{
			pose = new BoneTransform()
			{
				pos = skeletonBone.boneTransform.localPosition,
				rot = skeletonBone.boneTransform.localRotation,
				scale = skeletonBone.boneTransform.localScale,
			};
		}
		return pose;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void ProcessBoneStrippingMask(Entity rigEntity, RigDefinitionAuthoring rda, List<InternalSkeletonBone> rigBones)
	{
		//	Bone stripping mask is processed only in "Manual" stripping mode
		if (rda.boneEntityStrippingMode != RigDefinitionAuthoring.BoneEntityStrippingMode.Manual || rda.boneStrippingMask == null)
			return;

		var m = rda.boneStrippingMask;
		var bonesToRemove = AddBuffer<BoneEntitiesToRemove>(rigEntity);
        
		for (int i = 0; i < m.transformCount; ++i)
		{
			var isActive = m.GetTransformActive(i);
			if (isActive) continue;
			
			var path = m.GetTransformPath(i);
			var boneIndex = 0;
			for (; boneIndex < rigBones.Count && !path.EndsWith(rigBones[boneIndex].name); ++boneIndex) { }

			if (boneIndex < rigBones.Count)
			{
				var boneEntity = GetEntity(rigBones[boneIndex].boneTransform, TransformUsageFlags.None);
				bonesToRemove.Add(new BoneEntitiesToRemove() { boneEntity = boneEntity });
			}
		}
	}

}
}
