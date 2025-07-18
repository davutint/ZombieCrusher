
using UnityEngine.Rendering;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
    [GenerateHLSL]
    public enum RukhankaDebugMarkers
    {
		Deformation_CopyMeshData,
		Deformation_CopyBlendShapes,
		Deformation_CreatePerVertexDeformationWorkload,
		Deformation_SkinnedMeshVertex_Write,
		Deformation_SkinnedMeshVertex_Read,
		Deformation_DeformedVertex_Read,
		Deformation_BoneInfluence_Read,
		Deformation_DeformedVertex_Write,
		Deformation_FrameSkinMatrices_Read,
		Deformation_PerVertexWorkload_Read,
		Deformation_FrameDeformedVertex_Read,
        GPUAnimator_MakeRigSpaceBoneTransforms_AnimatedBoneWorkload_Read,
        GPUAnimator_MakeRigSpaceBoneTransforms_AnimationJobs_Read,
        GPUAnimator_MakeRigSpaceBoneTransforms_BoneLocalTransforms_Read0,
        GPUAnimator_MakeRigSpaceBoneTransforms_BoneLocalTransforms_Read1,
        GPUAnimator_MakeRigSpaceBoneTransforms_OutBoneTransforms_Write,
        GPUAnimator_ComputeSkinMatrices_SkinMatrixWorkload_Read,
        GPUAnimator_ComputeSkinMatrices_RigSpaceBoneTransforms_Read,
        GPUAnimator_ComputeSkinMatrices_OutSkinMatrices_Write,
        GPUAnimator_ProcessAnimations_AnimatedBoneWorkload_Read,
        GPUAnimator_ProcessAnimations_AnimationJobs_Read,
        GPUAnimator_ProcessAnimations_OutAnimatedBones_Write,
        GPUAnimator_GenericAvatar_AvatarMaskBuffer_Read,
        GPUAnimator_HumanoidAvatar_AvatarMaskBuffer_Read,
        GPUAnimator_HumanRotationData_Read,
        GPUAnimator_QueryPerfectHashTable_AnimationClips_Read0,
        GPUAnimator_QueryPerfectHashTable_AnimationClips_Read1,
        GPUAnimator_KeyFrame_Read,
        GPUAnimator_Track_Read,
        GPUAnimator_TrackSet_Read,
        GPUAnimator_AnimationClip_Read,
        GPUAnimator_SkinnedMeshBone_Read,
        GPUAnimator_RigDefinition_Read,
        GPUAnimator_RigBone_Read,
		Total
    }
}
