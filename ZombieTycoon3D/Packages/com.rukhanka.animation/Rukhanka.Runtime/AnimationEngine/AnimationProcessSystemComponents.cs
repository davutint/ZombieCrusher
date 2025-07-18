using Rukhanka.Toolbox;
using Unity.Entities;
using Unity.Mathematics;
#if RUKHANKA_WITH_NETCODE
using Unity.NetCode;
#endif
using FixedStringName = Unity.Collections.FixedString512Bytes;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
[InternalBufferCapacity(4)]
[ChunkSerializable]
public struct AnimationToProcessComponent: IBufferElementData
{
	public float weight;
	public float time;
	public BlobAssetReference<AnimationClipBlob> animation;
	public BlobAssetReference<AvatarMaskBlob> avatarMask;
	public AnimationBlendingMode blendMode;
	public float layerWeight;
	public int layerIndex;
	public uint motionId;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct AnimatorEntityRefComponent: IComponentData
{
	public int boneIndexInAnimationRig;
	public Entity animatorEntity;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if RUKHANKA_WITH_NETCODE
[GhostComponent(PrefabType = GhostPrefabType.Client)]
#endif
public struct AnimatedSkinnedMeshComponent: IComponentData
{
	public uint nameHash;
	public Entity animatedRigEntity;
	public Entity rootBoneEntity;
	public int rootBoneIndexInRig;
	public BlobAssetReference<SkinnedMeshInfoBlob> smrInfoBlob;
	
	public bool IsGPUAnimator(ComponentLookup<GPUAnimationEngineTag> gpuAnimationEngineTagLookup)
	{
		return gpuAnimationEngineTagLookup.HasComponent(animatedRigEntity) && gpuAnimationEngineTagLookup.IsComponentEnabled(animatedRigEntity);
	}
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if RUKHANKA_WITH_NETCODE
[GhostComponent(PrefabType = GhostPrefabType.Client)]
#endif
public struct SkinnedMeshRenderEntity: IBufferElementData
{
	public Entity value;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if RUKHANKA_WITH_NETCODE
[GhostComponent(PrefabType = GhostPrefabType.Client)]
#endif
public struct AnimatedRendererComponent: IComponentData
{
	public Entity animatorEntity;
	public Entity skinnedMeshEntity;
}
    
/////////////////////////////////////////////////////////////////////////////////


#if RUKHANKA_WITH_NETCODE
[GhostComponent(PrefabType = GhostPrefabType.Client)]
#endif
public struct ShouldUpdateBoundingBoxTag: IComponentData, IEnableableComponent { }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct SkinnedMeshBounds: IComponentData, IEnableableComponent
{
	public AABB value;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct RootMotionAnimationStateComponent: IBufferElementData, IEnableableComponent
{
	public uint uniqueMotionId;
	public BoneTransform animationState;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct RootMotionVelocityComponent: IComponentData
{
	public bool removeBuiltinEntityMovement;
	public float3 worldVelocity;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct AnimationEventComponent: IBufferElementData, IEnableableComponent
{
	public AnimationEventComponent(ref AnimationEventBlob aeb)
	{
		nameHash = aeb.nameHash;
		floatParam = aeb.floatParam;
		intParam = aeb.intParam;
		stringParamHash = aeb.stringParamHash;
	}
	
	public uint nameHash;
	public float floatParam;
	public int intParam;
	public uint stringParamHash;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[ChunkSerializable]
public struct PreviousProcessedAnimationComponent: IBufferElementData
{
	public uint motionId;
	public float animationTime;
	public BlobAssetReference<AnimationClipBlob> animation;
}


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//	Define some special bone names
public static class SpecialBones
{
	//	Special bone hashes need to be recalculated in case of hashing function change!
	//	I cannot add hash computation code here, because burst will fail to compile
	public static readonly string UnnamedRootBoneName = "RUKHANKA_UnnamedRootBone";
	public static readonly string AnimatorTypeName = "RUKHANKA_Animator";
	public static readonly uint AnimatorTypeNameHash = 0xde00343e;
}
}

