
#ifndef GPU_STRUCTURES_HLSL_
#define GPU_STRUCTURES_HLSL_

/////////////////////////////////////////////////////////////////////////////////

//  struct RigDefinition
ByteAddressBuffer rigDefinitions;
//  struct RigBone
ByteAddressBuffer rigBones;
//  struct AnimationClip
ByteAddressBuffer animationClips;
//  struct HumanRotationData
ByteAddressBuffer humanRotationDataBuffer;
int animatedBonesCount;

/////////////////////////////////////////////////////////////////////////////////

#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/Debug.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/BoneTransform.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/AnimationClip.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/Track.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/SkinnedMeshBone.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/RigDefinition.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/RigBone.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/HumanRotationData.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/AnimationToProcess.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/AvatarMask.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/PerfectHashTable.hlsl"

/////////////////////////////////////////////////////////////////////////////////

struct AnimationJob
{
    int rigDefinitionIndex;
    int animatedBoneIndexOffset;
    int2 animationsToProcessRange;
};

/////////////////////////////////////////////////////////////////////////////////

struct SkinnedMeshWorkload
{
    int skinMatrixBaseOutIndex;
    int boneRemapTableIndex;
    int skinMatricesCount;
    int rootBoneIndex;
    int animatedBoneIndexOffset;
    float4x4 skinnedRootBoneToEntityTransform;
};

/////////////////////////////////////////////////////////////////////////////////

struct AnimatedBoneWorkload
{
    int boneIndexInRig;
    int animationJobIndex;
};

/////////////////////////////////////////////////////////////////////////////////

StructuredBuffer<AnimatedBoneWorkload> animatedBoneWorkload;
StructuredBuffer<AnimationJob> animationJobs;
StructuredBuffer<AnimationToProcess> animationsToProcess;

#endif
