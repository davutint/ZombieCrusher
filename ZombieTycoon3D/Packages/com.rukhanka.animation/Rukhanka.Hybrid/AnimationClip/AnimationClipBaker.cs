#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Rukhanka.Hybrid.RTP;
using Rukhanka.Toolbox;
using Unity.Assertions;
using FixedStringName = Unity.Collections.FixedString512Bytes;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Entities;
using AnimationClip = UnityEngine.AnimationClip;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{ 
[BurstCompile]
public partial class AnimationClipBaker
{
	//	To reduce huge memory consumption during baking of many animations, I will reuse temporarty buffers
	NativeList<KeyFrame> keyFramesList = new (Allocator.Temp);
	NativeList<Track> trackList = new (Allocator.Temp);
	NativeList<uint> trackGroupHashes = new (Allocator.Temp);
	NativeList<uint> trackGroupOffsets = new (Allocator.Temp);
#if RUKHANKA_DEBUG_INFO
	NativeList<FixedString128Bytes> trackGroupNames = new (Allocator.Temp);
#endif
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	struct ParsedCurveBinding
	{
		public BindingType bindingType;
		public uint channelIndex;
		public string boneName;
		public string channelName;

		public bool IsValid() => boneName.Length > 0;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	(string, string) SplitPath(string path)
	{
		var arr = path.Split('/');
		Assert.IsTrue(arr.Length > 0);
		var rv = (arr.Last(), arr.Length > 1 ? arr[^2] : "");
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	BindingType PickGenericBindingTypeByString(string bindingString) => bindingString switch
	{
		"m_LocalPosition" => BindingType.Translation,
		"m_LocalRotation" => BindingType.Quaternion,
		"localEulerAngles" => BindingType.EulerAngles,
		"localEulerAnglesRaw" => BindingType.EulerAngles,
		"m_LocalScale" => BindingType.Scale,
		_ => BindingType.Unknown
	};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	uint ChannelIndexFromString(string c) => c switch
	{
		"x" => 0,
		"y" => 1,
		"z" => 2,
		"w" => 3,
		_   => 0
	};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	string ConstructBoneClipName(string name, string path, Type animatedObjectType)
	{
		if (animatedObjectType == typeof(UnityEngine.Animator))
			return SpecialBones.AnimatorTypeName;
		
		var rv = name;
		if (animatedObjectType != typeof(UnityEngine.SkinnedMeshRenderer))
		{
			//	Empty name string is unnamed root bone
			if (name.Length == 0 && path.Length == 0)
			{
				rv = SpecialBones.UnnamedRootBoneName;
			}
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int2 CopyKeyFrames(Keyframe[] keysArr, ref NativeList<KeyFrame> outKeyFrames)
	{
		var rv = new int2(outKeyFrames.Length, keysArr.Length);
		foreach (var k in keysArr)
		{
			var kf = new KeyFrame()
			{
				time = k.time,
				inTan = math.select(0, k.inTangent, math.isfinite(k.inTangent)),
				outTan = math.select(0, k.outTangent, math.isfinite(k.outTangent)),
				v = k.value
			};
			outKeyFrames.Add(kf);
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	Track CreateTrackData(int2 keyFrameRange, ParsedCurveBinding pb)
	{
		var rv = new Track();
		rv.keyFrameRange = keyFrameRange;
	#if RUKHANKA_DEBUG_INFO
		rv.name = $"{pb.boneName}.{pb.channelName}";
	#endif
		//	For unknown binding types treat props as channel name hash
		if (pb.bindingType == BindingType.Unknown)
		{
			rv.props = Track.CalculateHash(pb.channelName);
		}
		else
		{
			rv.bindingType = pb.bindingType;
			rv.channelIndex = pb.channelIndex;
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void InsertTrackIntoGroup(Track t, string trackName)
	{
		//	Tracks with same path hash will go into same track group. This is needed to speedup access of bone data
		//	(no need to query each attribute hash)
		
		var nameHash = trackName.CalculateHash32();
		//	Search for name in already existing groups
		var i = trackGroupHashes.IndexOf(nameHash);
		
		if (i < 0)
		{
			//	Create new track group and add track into it
			trackGroupHashes.Add(nameHash);
			trackGroupOffsets.Add((uint)trackList.Length);
			trackList.Add(t);
		#if RUKHANKA_DEBUG_INFO
			trackGroupNames.Add(trackName);
		#endif
		}
		else
		{
			//	Insert track into existing group
			var startIndex = (int)trackGroupOffsets[i];
			trackList.InsertRange(startIndex, 1);
			trackList[startIndex] = t;
			
			//	Shift all other groups to the right
			for (var l = 0; l < trackGroupOffsets.Length; ++l)
			{
				ref var tgo = ref trackGroupOffsets.ElementAt(l);
				if (tgo > startIndex)
					tgo += 1;
			}
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	ParsedCurveBinding ParseGenericCurveBinding(EditorCurveBinding b)
	{
		var rv = new ParsedCurveBinding();

		var t = b.propertyName.Split('.');
		var propName = t[0];
		var channel = t.Length > 1 ? t[1] : "";

		rv.channelIndex = ChannelIndexFromString(channel);
		rv.bindingType = PickGenericBindingTypeByString(propName);
		rv.channelName = b.propertyName;
		var nameAndPath = SplitPath(b.path);
		rv.boneName = ConstructBoneClipName(nameAndPath.Item1, nameAndPath.Item2, b.type);

		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int GetHumanBoneIndexForHumanName(in HumanDescription hd, FixedStringName humanBoneName)
	{
		var humanBoneIndexInAvatar = Array.FindIndex(hd.human, x => x.humanName == humanBoneName);
		return humanBoneIndexInAvatar;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	ParsedCurveBinding ParseHumanoidCurveBinding(EditorCurveBinding b, Avatar avatar)
	{
		if (!humanoidMappingTable.TryGetValue(b.propertyName, out var rv))
			return ParseGenericCurveBinding(b);

		var hd = avatar.humanDescription;
		var humanBoneIndexInAvatar = GetHumanBoneIndexForHumanName(hd, rv.boneName);
		if (humanBoneIndexInAvatar < 0)
			return rv;

		if (rv.bindingType == BindingType.HumanMuscle)
		{
			var humanBoneDef = hd.human[humanBoneIndexInAvatar];
			rv.boneName = humanBoneDef.boneName;
		}

		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	ParsedCurveBinding ParseCurveBinding(AnimationClip ac, EditorCurveBinding b, Avatar avatar)
	{
		var rv = ac.isHumanMotion ?
			ParseHumanoidCurveBinding(b, avatar) :
			ParseGenericCurveBinding(b);

		return  rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	KeyFrame AddKeyFrameFromFloatValue(float2 key, float v)
	{
		var kf = new KeyFrame()
		{
			time = key.x,
			inTan = key.y,
			outTan = key.y,
			v = v
		};
		return kf;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	void ComputeTangents(in Span<KeyFrame> keyFrames)
	{
		for (int i = 0; i < keyFrames.Length; ++i)
		{
			var p0 = i == 0 ? keyFrames[0] : keyFrames[i - 1];
			var p1 = keyFrames[i];
			var p2 = i == keyFrames.Length - 1 ? keyFrames[i] : keyFrames[i + 1];

			var outV = math.normalizesafe(new float2(p2.time, p2.v) - new float2(p1.time, p1.v));
			var outTan = outV.x > 0.0001f ? outV.y / outV.x : 0;

			var inV = math.normalizesafe(new float2(p1.time, p1.v) - new float2(p0.time, p0.v));
			var inTan = inV.x > 0.0001f ? inV.y / inV.x : 0;

			var dt = math.abs(inTan) + math.abs(outTan);
			var f = dt > 0 ? math.abs(inTan) / dt : 0;

			var avgTan = math.lerp(inTan, outTan, f);

			var k = keyFrames[i];
			k.outTan = avgTan;
			k.inTan = avgTan;
			keyFrames[i] = k;
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	NativeList<float> CreateKeyframeTimes(float animationLength, float dt, float frameTime)
	{
		var numFrames = (int)math.ceil(animationLength / dt) + 1;
		var rv = new NativeList<float>(numFrames, Allocator.Temp);
		
		if (frameTime >= 0)
		{
			rv.Add(frameTime);	
			return rv;
		}

		var curTime = 0.0f;
		for (var i = 0; i < numFrames; ++i)
		{
			rv.Add(curTime);
			curTime += dt;
		}
		
		if (rv.Length > 0)
			rv[^1] = math.min(animationLength, rv[^1]);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void ReadCurvesFromTransform(Transform tr, in NativeArray<Track> trackSpan, int keyIndex, float time)
	{
		quaternion q = tr.localRotation;
		float3 t = tr.localPosition;

		Span<float> vArr = stackalloc float[7];
		vArr[0] = t.x;
		vArr[1] = t.y;
		vArr[2] = t.z;
		vArr[3] = q.value.x;
		vArr[4] = q.value.y;
		vArr[5] = q.value.z;
		vArr[6] = q.value.w;

		for (int l = 0; l < vArr.Length; ++l)
		{
			var kfIndex = trackSpan[l].keyFrameRange.x + keyIndex;
			var kf = AddKeyFrameFromFloatValue(time, vArr[l]);
			keyFramesList[kfIndex] = kf;
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe void SampleUnityAnimation(AnimationClip ac, Animator anm, ValueTuple<Transform, uint>[] trs, bool applyRootMotion, float frameTime)
	{
		if (trs.Length == 0)
			return;
		
		var sampleAnimationFrameTime = 1 / 60.0f;
		var keysList = CreateKeyframeTimes(ac.length, sampleAnimationFrameTime, frameTime);

		var tracks = new []
		{
			new Track(BindingType.Translation, 0),
			new Track(BindingType.Translation, 1),
			new Track(BindingType.Translation, 2),
			new Track(BindingType.Quaternion, 0),
			new Track(BindingType.Quaternion, 1),
			new Track(BindingType.Quaternion, 2),
			new Track(BindingType.Quaternion, 3),
		};
		
		var rac = anm.runtimeAnimatorController;
		var origPos = anm.transform.position;
		var origRot = anm.transform.rotation;
		var origRootMotion = anm.applyRootMotion;
		var prevAnmCulling = anm.cullingMode;
		
		anm.runtimeAnimatorController = null;
		anm.cullingMode = AnimatorCullingMode.AlwaysAnimate;
		anm.applyRootMotion = true;
		anm.transform.position = Vector3.zero;
		anm.transform.rotation = quaternion.identity;
		
		var newTracks = new NativeArray<Track>(tracks.Length * trs.Length, Allocator.Temp);
		for (int k = 0; k < newTracks.Length; ++k)
		{
			var nt = tracks[k % tracks.Length];
			nt.keyFrameRange = new int2(keyFramesList.Length, keysList.Length);
		#if RUKHANKA_DEBUG_INFO
			nt.name = $"{trs[k / tracks.Length].Item1.name}.{nt.bindingType}.{nt.channelIndex}";
		#endif
			keyFramesList.Resize(keyFramesList.Length + keysList.Length, NativeArrayOptions.ClearMemory);
			newTracks[k] = nt;
		}
		
		for (int i = 0; i < keysList.Length; ++i)
		{
			var time = keysList[i];
			ac.SampleAnimation(anm.gameObject, time);

			for (int l = 0; l < trs.Length; ++l)
			{
				var tr = trs[l].Item1;
				var trackSpan = newTracks.GetSubArray(l * tracks.Length, tracks.Length);
				ReadCurvesFromTransform(tr, trackSpan, i, time);
			}
		}
		
		for (int l = 0; l < trs.Length; ++l)
		{
			var transformHash = trs[l].Item2;
			var idx = trackGroupHashes.IndexOf(transformHash);
			if (idx < 0)
			{
				trackGroupHashes.Add(transformHash);
				trackGroupOffsets.Add((uint)trackList.Length);
			#if RUKHANKA_DEBUG_INFO
				trackGroupNames.Add(trs[l].Item1.name);
			#endif
			}
			else
			{
				trackGroupOffsets[idx] = (uint)trackList.Length;
			}
			
			trackList.AddRange(newTracks.GetSubArray(l * tracks.Length, tracks.Length));
		}
		
		for (int m = 0; m < newTracks.Length; ++m)
		{
			var nt = newTracks[m];
			var trackKeyFrames = new Span<KeyFrame>(keyFramesList.GetUnsafePtr() + nt.keyFrameRange.x, nt.keyFrameRange.y);
			ComputeTangents(trackKeyFrames);
		}

		anm.cullingMode = prevAnmCulling;
		anm.runtimeAnimatorController = rac;
		anm.transform.position = origPos;
		anm.transform.rotation = origRot;
		anm.applyRootMotion = origRootMotion;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	(Transform, uint) GetRootBoneTransform(Animator anm)
	{
		if (anm.avatar.isHuman)
		{
			var hipsTransform = anm.GetBoneTransform(HumanBodyBones.Hips);
			var hd = anm.avatar.humanDescription;
			var humanBoneIndexInDesc = GetHumanBoneIndexForHumanName(hd, "Hips");
			var rigHipsBoneName = new FixedStringName(hd.human[humanBoneIndexInDesc].boneName).CalculateHash32();
			return (hipsTransform, rigHipsBoneName);
		}

		var rootBoneName =  anm.avatar.GetRootMotionNodeName();
		var rootBoneNameHash = new FixedStringName(rootBoneName).CalculateHash32();
		var rootBoneTransform = Rukhanka.Toolbox.TransformUtils.FindChildRecursively(anm.transform, rootBoneName);
		return (rootBoneTransform, rootBoneNameHash);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void SampleMissingCurves(AnimationClip ac, Animator anm, float frameTime)
	{
		var trs = new List<ValueTuple<Transform, uint>>();
		var entityRootTransform = anm.transform;
		var rootBoneTransformData = GetRootBoneTransform(anm);

		if (anm.isHuman)
			trs.Add(rootBoneTransformData);

		//	Sample curves for non-rootmotion animations
		SampleUnityAnimation(ac, anm, trs.ToArray(), false, frameTime);
		
		//	Sample root motion curves
		trs.Clear();
		
		var entityRootHash = SpecialBones.UnnamedRootBoneName.CalculateHash32();
		entityRootHash = AnimationProcessSystem.ComputeBoneAnimationJob.ModifyBoneHashForRootMotion(entityRootHash);
		trs.Add((entityRootTransform, entityRootHash));
		
		//	Modify bone hash to separate root motion tracks and ordinary tracks
		rootBoneTransformData.Item2 = AnimationProcessSystem.ComputeBoneAnimationJob.ModifyBoneHashForRootMotion(rootBoneTransformData.Item2);
		trs.Add(rootBoneTransformData);
		
		SampleUnityAnimation(ac, anm, trs.ToArray(), true, frameTime);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void BakeAnimationEvents(BlobBuilder bb, ref AnimationClipBlob acb, AnimationClip ac)
	{
		if (ac.events.Length == 0)
			return;

		var eventsArr = bb.Allocate(ref acb.events, ac.events.Length);
		for (var i = 0; i < eventsArr.Length; ++i)
		{
			var ae = ac.events[i];
			ref var bakedEvent = ref eventsArr[i];
		#if RUKHANKA_DEBUG_INFO
			if (ae.functionName.Length > 0)
				bb.AllocateString(ref bakedEvent.name, ae.functionName);
			if (ae.stringParameter.Length > 0)
				bb.AllocateString(ref bakedEvent.stringParam, ae.stringParameter);
		#endif
			bakedEvent.nameHash = new FixedStringName(ae.functionName).CalculateHash32();
			bakedEvent.time = ae.time / ac.length;
			bakedEvent.floatParam = ae.floatParameter;
			bakedEvent.intParam = ae.intParameter;
			bakedEvent.stringParamHash = new FixedStringName(ae.stringParameter).CalculateHash32();
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	AnimationClip[] Deduplicate(AnimationClip[] animationClips)
	{
		var dedupList = new List<AnimationClip>();
		var dupSet = new NativeHashSet<int>(animationClips.Length, Allocator.Temp);

		foreach (var a in animationClips)
		{
			if (a != null && !dupSet.Add(a.GetInstanceID()))
				continue;

			dedupList.Add(a);
		}
		return dedupList.ToArray();
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int BuildAnimationBakeList(IBaker baker, AnimationClip[] animationClips, Avatar avatar, out NativeArray<BlobAssetReference<AnimationClipBlob>> alreadyBakedList)
	{
		alreadyBakedList = new (animationClips.Length, Allocator.Temp);
		var rv = 0;
		for (var i = 0; i < animationClips.Length; ++i)
		{
			var ac = animationClips[i];
			if (ac == null)
				continue;
			
			//	Check for blob asset store first
			var animationHash = BakingUtils.ComputeAnimationHash(ac, avatar);
			var isAnimationExists = baker.TryGetBlobAssetReference<AnimationClipBlob>(animationHash, out var acb);
			if (!isAnimationExists)
			{
				//	Try cached baked animation
				acb = BlobCache.LoadBakedAnimationFromCache(ac, avatar);
				if (acb == BlobAssetReference<AnimationClipBlob>.Null)
				{
					rv += 1;
				}
				else
				{
					//	Don't forget to add loaded animation to blob asset store
					baker.AddBlobAssetWithCustomHash(ref acb, animationHash);
				}
			}
			
			alreadyBakedList[i] = acb;
		}
		
		//	Return count of animations need to perform full bake
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public NativeArray<BlobAssetReference<AnimationClipBlob>> BakeAnimations(IBaker baker, AnimationClip[] animationClips, Avatar avatar, GameObject animatedObjectRoot)
	{
		if (animationClips == null || animationClips.Length == 0)
			return default;
		
		animationClips = Deduplicate(animationClips);
		
		//	Firstly create list of animations that need to be baked (not present in cache and in not in blob asset store)
		var numClipsToBake = BuildAnimationBakeList(baker, animationClips, avatar, out var alreadyBakedAnimations);
		
		//	If nothing to bake, just return already baked list
		if (numClipsToBake == 0)
			return alreadyBakedAnimations;
		
		//	Now bake animations that require full rebake
		//	Need to make instance of object because when we will sample animations object placement can be modified.
		//	Also prefabs will not update its transforms
		GameObject objectCopy = null;
		Animator animatorCopy = null;
		if (avatar != null)
		{
			objectCopy = GameObject.Instantiate(animatedObjectRoot);
			objectCopy.hideFlags = HideFlags.HideAndDontSave;
			animatorCopy = objectCopy.GetComponent<Animator>();
			if (animatorCopy == null)
				animatorCopy = objectCopy.AddComponent<Animator>();
			animatorCopy.avatar = avatar;
		}
		
		for (var i = 0; i < animationClips.Length; ++i)
		{
			var clipBlob = alreadyBakedAnimations[i];
			var a = animationClips[i];
			if (clipBlob != BlobAssetReference<AnimationClipBlob>.Null)
				continue;
			
			var animationHash = BakingUtils.ComputeAnimationHash(a, avatar);
			var isAnimationExists = baker.TryGetBlobAssetReference(animationHash, out clipBlob);
			if (!isAnimationExists)
			{
				clipBlob = CreateAnimationBlobAsset(a, animatorCopy, animationHash);
				baker.AddBlobAssetWithCustomHash(ref clipBlob, animationHash);
			}
			else
			{
				Debug.Log($"Animation '{a.name}' is duplicate!");
			}
			
			alreadyBakedAnimations[i] = clipBlob;
			baker.DependsOn(a);
		}
		
		if (objectCopy != null)
			GameObject.DestroyImmediate(objectCopy);
		
		return alreadyBakedAnimations;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public BlobAssetReference<AnimationClipBlob> CreateAnimationBlobAsset(AnimationClip ac, Animator animator, Hash128 animationHash)
	{
		var avatar = animator?.avatar;
		var acSettings = AnimationUtility.GetAnimationClipSettings(ac);
		
		var bb = new BlobBuilder(Allocator.Temp);
		ref var rv = ref bb.ConstructRoot<AnimationClipBlob>();
		
#if RUKHANKA_DEBUG_INFO
		var startTimeMarker = Time.realtimeSinceStartupAsDouble;
		if (ac.name.Length > 0)
			bb.AllocateString(ref rv.name, ac.name);
#endif
		
		rv.length = ac.length;
		rv.looped = ac.isLooping;
		rv.hash = animationHash;
		rv.loopPoseBlend = acSettings.loopBlend;
		rv.cycleOffset = acSettings.cycleOffset;

		BakeAnimationEvents(bb, ref rv, ac);
		BakeTrackSet(bb, ref rv.clipTracks, out var maxTrackKeyframeLength, -1, ac, animator, avatar);
		if (acSettings.additiveReferencePoseClip != null)
			BakeTrackSet(bb, ref rv.additiveReferencePoseFrame, out _, acSettings.additiveReferencePoseTime, acSettings.additiveReferencePoseClip, animator, avatar);
		
	#if RUKHANKA_DEBUG_INFO
		var dt = Time.realtimeSinceStartupAsDouble - startTimeMarker;
		rv.bakingTime = (float)dt;
	#endif
		
		rv.maxTrackKeyframeLength = maxTrackKeyframeLength;
		var bar = bb.CreateBlobAssetReference<AnimationClipBlob>(Allocator.Persistent);
		
		//	Save baked animation into cache
		BlobCache.SaveBakedAnimationToCache(ac, avatar, bar);
		
		return bar;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	Keyframe[] GetKeyFramesArray(UnityEngine.AnimationCurve animationCurve, float frameTime)
	{
		if (frameTime < 0)
			return animationCurve.keys;
		
		var oneFrameAnimation = new Keyframe[]
		{
			new (0, animationCurve.Evaluate(frameTime))
		};
		return oneFrameAnimation;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void BakeTrackSet(BlobBuilder bb, ref TrackSet outTrackSet, out uint maxTrackKeyframeLength, float frameTime, AnimationClip ac, Animator animator, Avatar avatar)
	{
		var bindings = AnimationUtility.GetCurveBindings(ac);

		keyFramesList.Clear();
		trackList.Clear();
		trackGroupHashes.Clear();
		trackGroupOffsets.Clear();
	#if RUKHANKA_DEBUG_INFO
		trackGroupNames.Clear();
	#endif
		
		maxTrackKeyframeLength = 0u;
		foreach (var b in bindings)
		{
			var ec = AnimationUtility.GetEditorCurve(ac, b);
			var pb = ParseCurveBinding(ac, b, animator?.avatar);
			var inKeyframes = GetKeyFramesArray(ec, frameTime);
			var keyFramesRange = CopyKeyFrames(inKeyframes, ref keyFramesList);
			var trackData = CreateTrackData(keyFramesRange, pb);
			InsertTrackIntoGroup(trackData, pb.boneName);
			
			maxTrackKeyframeLength = math.max(maxTrackKeyframeLength, (uint)keyFramesRange.y);
		}
		
		if (avatar != null)
		{
			//	Sample root and hips curves and from unity animations. Maybe sometime I will figure out all RootT/RootQ and body pose generation formulas and this step could be replaced with generation.
			SampleMissingCurves(ac, animator, frameTime);
		}
		
		// Create special end empty track group because track group tracks count is calculated as trackGroup[i + 1].trackCount - trackGroup[i].trackCount
		trackGroupHashes.Add(0xffffffff);
		trackGroupOffsets.Add((uint)trackList.Length);
	#if RUKHANKA_DEBUG_INFO
		trackGroupNames.Add("RUKHANKA_TRAILING_TRACK");
	#endif
		
		CreateTrackSetBlob(ref bb, ref outTrackSet, ac.name);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	unsafe void CreateTrackSetBlob(ref BlobBuilder bb, ref TrackSet outTrackSet, string animationName)
	{
		var keyFramesBlobArray = bb.Allocate(ref outTrackSet.keyframes, keyFramesList.Length);
		UnsafeUtility.MemCpy(keyFramesBlobArray.GetUnsafePtr(), keyFramesList.GetUnsafeReadOnlyPtr(), keyFramesList.Length * UnsafeUtility.SizeOf<KeyFrame>());
		
		var tracksBlobArray = bb.Allocate(ref outTrackSet.tracks, trackList.Length);
		UnsafeUtility.MemCpy(tracksBlobArray.GetUnsafePtr(), trackList.GetUnsafeReadOnlyPtr(), trackList.Length * UnsafeUtility.SizeOf<Track>());
		
		var trackGroupsBlobArray = bb.Allocate(ref outTrackSet.trackGroups, trackGroupOffsets.Length);
		UnsafeUtility.MemCpy(trackGroupsBlobArray.GetUnsafePtr(), trackGroupOffsets.GetUnsafeReadOnlyPtr(), trackGroupOffsets.Length * UnsafeUtility.SizeOf<uint>());
		
		//	Make a hash table from track group hashes
		var phtIsCreated = Perfect2HashTable.Build(trackGroupHashes.AsArray(), out var pht, out var seed);
		Assert.IsTrue(phtIsCreated, $"Cannot create track perfect hash table for animation {animationName}");
		
		var phtBlobArray = bb.Allocate(ref outTrackSet.trackGroupPHT.pht, pht.Length);
		UnsafeUtility.MemCpy(phtBlobArray.GetUnsafePtr(), pht.GetUnsafeReadOnlyPtr(), pht.Length * UnsafeUtility.SizeOf<uint2>());
		
	#if RUKHANKA_DEBUG_INFO
		var trackGroupDebugInfoArr = bb.Allocate(ref outTrackSet.trackGroupDebugInfo, trackGroupHashes.Length);
		for (var i = 0; i < trackGroupHashes.Length; ++i)
		{
			var trackGroupInfoBlob = new TrackGroupInfo()
			{
				hash = trackGroupHashes[i],
				name = trackGroupNames[i]
			};
			trackGroupDebugInfoArr[i] = trackGroupInfoBlob;
		}
	#endif
		
		outTrackSet.trackGroupPHT.seed = seed;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void MakeAdditiveReferencePoseFrame(ref NativeList<BoneClip> bc, AnimationClipSettings acs, Avatar avatar)
	{
		/*
		if (acs.additiveReferencePoseClip == null)
			return;
		
		for (var k = 0; k < bc.Length; ++k)
		{
			var bcc = bc[k];
			for (var m = 0; m < bcc.animationCurves.Length; ++m)
			{
				ref var ac = ref bcc.animationCurves.ElementAt(m);
			}
		}
		
		var clipPose = SampleAnimation(acs.additiveReferencePoseClip, avatar, acs.additiveReferencePoseTime);
		foreach (var c in clipPose)
		{
			for (var k = 0; k < bc.Length; ++k)
			{
				var bcc = bc[k];
				if (c.pb.boneName != bcc.name)
					continue;
				
				for (var m = 0; m < bcc.animationCurves.Length; ++m)
				{
					ref var ac = ref bcc.animationCurves.ElementAt(m);
					
					if (ac.bindingType == BindingType.Scale || (ac.bindingType == BindingType.Quaternion && ac.channelIndex == 3))
					    ac.additiveReferenceValue = 1;
					    
					if (ac.bindingType == c.pb.bindingType && ac.channelIndex == c.pb.channelIndex)
					{
						ac.additiveReferenceValue = c.value;
						break;
					}
				}
			}
		}
			*/
	}
}
}

#endif