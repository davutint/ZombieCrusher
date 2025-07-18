
using System;
using System.Runtime.CompilerServices;
using Rukhanka.Toolbox;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct RuntimeAnimationData: IComponentData
{
	public struct AnimatedEntityBoneDataProps
	{
		public int bonePoseOffset;
		public int boneFlagsOffset;
		public int rigBoneCount;
		
		public static AnimatedEntityBoneDataProps MakeInvalid()
		{
			return new AnimatedEntityBoneDataProps()
			{
				boneFlagsOffset = -1,
				bonePoseOffset = -1,
				rigBoneCount = -1,
			};
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////
	
    internal NativeList<BoneTransform> animatedBonesBuffer;
    internal NativeList<BoneTransform> worldSpaceBonesBuffer;
    internal NativeParallelHashMap<Entity, AnimatedEntityBoneDataProps> entityToDataOffsetMap;
    internal NativeList<int3> boneToEntityArr;
	internal NativeList<ulong> boneTransformFlagsHolderArr;

/////////////////////////////////////////////////////////////////////////////////

	public static RuntimeAnimationData MakeDefault()
	{
		var rv = new RuntimeAnimationData()
		{
			animatedBonesBuffer = new (Allocator.Persistent),
			worldSpaceBonesBuffer = new (Allocator.Persistent),
			entityToDataOffsetMap = new (128, Allocator.Persistent),
			boneToEntityArr = new (Allocator.Persistent),
			boneTransformFlagsHolderArr = new (Allocator.Persistent),
		};
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////

	public void Dispose()
	{
		animatedBonesBuffer.Dispose();
		worldSpaceBonesBuffer.Dispose();
		entityToDataOffsetMap.Dispose();
		boneToEntityArr.Dispose();
		boneTransformFlagsHolderArr.Dispose();
	}

/////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AnimatedEntityBoneDataProps CalculateBufferOffset(in NativeParallelHashMap<Entity, AnimatedEntityBoneDataProps> entityToDataOffsetMap, Entity animatedRigEntity)
	{
		if (!entityToDataOffsetMap.TryGetValue(animatedRigEntity, out var offset))
			return AnimatedEntityBoneDataProps.MakeInvalid();

		return offset;
	}

/////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<BoneTransform> GetAnimationDataForRigRO(in NativeList<BoneTransform> animatedBonesBuffer, int offset, int length)
	{
		var rv = animatedBonesBuffer.GetReadOnlySpan(offset, length);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<BoneTransform> GetAnimationDataForRigRW(in NativeList<BoneTransform> animatedBonesBuffer, int offset, int length)
	{
		var rv = animatedBonesBuffer.GetSpan(offset, length);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<BoneTransform> GetAnimationDataForRigRO
	(
		in NativeList<BoneTransform> animatedBonesBuffer,
		in NativeParallelHashMap<Entity, AnimatedEntityBoneDataProps> entityToDataOffsetMap,
		Entity animatedRigEntity
	)
	{
		var dp = CalculateBufferOffset(entityToDataOffsetMap, animatedRigEntity);
		if (dp.bonePoseOffset < 0)
			return default;
			
		return GetAnimationDataForRigRO(animatedBonesBuffer, dp.bonePoseOffset, dp.rigBoneCount);
	}

///////////////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<BoneTransform> GetAnimationDataForRigRW
	(
		in NativeList<BoneTransform> animatedBonesBuffer,
		in NativeParallelHashMap<Entity, AnimatedEntityBoneDataProps> entityToDataOffsetMap,
		Entity animatedRigEntity
	)
	{
		var dp = CalculateBufferOffset(entityToDataOffsetMap, animatedRigEntity);
		if (dp.bonePoseOffset < 0)
			return default;
			
		return GetAnimationDataForRigRW(animatedBonesBuffer, dp.bonePoseOffset, dp.rigBoneCount);
	}

///////////////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AnimationTransformFlags GetAnimationTransformFlagsRO(in NativeList<int3> boneToEntityArr, in NativeList<ulong> boneTransformFlagsArr, int globalBoneIndex, int boneCount)
	{
		var boneInfo = boneToEntityArr[globalBoneIndex];
		var rv = AnimationTransformFlags.CreateFromBufferRO(boneTransformFlagsArr, boneInfo.z, boneCount);
		return rv;
	}

///////////////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AnimationTransformFlags GetAnimationTransformFlagsRW(in NativeList<int3> boneToEntityArr, in NativeList<ulong> boneTransformFlagsArr, int globalBoneIndex, int boneCount)
	{
		var boneInfo = boneToEntityArr[globalBoneIndex];
		var rv = AnimationTransformFlags.CreateFromBufferRW(boneTransformFlagsArr, boneInfo.z, boneCount);
		return rv;
	}
}
}
