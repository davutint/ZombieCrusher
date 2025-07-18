#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public partial class GPUAnimationSystem
{
	readonly int ShaderID_outAnimatedBones = Shader.PropertyToID("outAnimatedBones");
	readonly int ShaderID_animatedBoneWorkload = Shader.PropertyToID("animatedBoneWorkload");
	readonly int ShaderID_animationJobs = Shader.PropertyToID("animationJobs");
	readonly int ShaderID_animationsToProcess = Shader.PropertyToID("animationsToProcess");
	readonly int ShaderID_rigDefinitions = Shader.PropertyToID("rigDefinitions");
	readonly int ShaderID_rigBones = Shader.PropertyToID("rigBones");
	readonly int ShaderID_animationClips = Shader.PropertyToID("animationClips");
	readonly int ShaderID_humanRotationDataBuffer = Shader.PropertyToID("humanRotationDataBuffer");
	readonly int ShaderID_animatedBonesCount = Shader.PropertyToID("animatedBonesCount");
	readonly int ShaderID_boneLocalTransforms = Shader.PropertyToID("boneLocalTransforms");
	readonly int ShaderID_outBoneTransforms = Shader.PropertyToID("outBoneTransforms");
	readonly int ShaderID_avatarMasksBuffer = Shader.PropertyToID("avatarMasksBuffer");
	readonly int ShaderID_skinMatrixWorkloadBuf = Shader.PropertyToID("skinMatrixWorkloadBuf");
	readonly int ShaderID_rigSpaceBoneTransformsBuf = Shader.PropertyToID("rigSpaceBoneTransformsBuf");
	readonly int ShaderID_outSkinMatrices = Shader.PropertyToID("outSkinMatrices");
	readonly int ShaderID_skinnedMeshBoneData = Shader.PropertyToID("skinnedMeshBoneData");
	readonly int ShaderID_totalSkinnedMeshes = Shader.PropertyToID("totalSkinnedMeshes");
}
}

#endif
