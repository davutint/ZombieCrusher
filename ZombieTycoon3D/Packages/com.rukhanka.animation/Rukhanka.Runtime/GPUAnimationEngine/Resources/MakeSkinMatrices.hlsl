#ifndef MAKE_SKIN_MATRICES_HLSL_
#define MAKE_SKIN_MATRICES_HLSL_

/////////////////////////////////////////////////////////////////////////////////

RWStructuredBuffer<float3x4> outSkinMatrices;
StructuredBuffer<BoneTransform> rigSpaceBoneTransformsBuf;
StructuredBuffer<SkinnedMeshWorkload> skinMatrixWorkloadBuf;
ByteAddressBuffer skinnedMeshBoneData;
uint totalSkinnedMeshes;

/////////////////////////////////////////////////////////////////////////////////

[numthreads(128, 1, 1)]
void ComputeSkinMatrices(uint tid: SV_DispatchThreadID)
{
    if (tid >= totalSkinnedMeshes)
        return;

    CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_COMPUTE_SKIN_MATRICES_SKIN_MATRIX_WORKLOAD_READ, tid, skinMatrixWorkloadBuf);
    SkinnedMeshWorkload smw = skinMatrixWorkloadBuf[tid];
    float4x4 w2l = smw.skinnedRootBoneToEntityTransform;

    for (int i = 0; i < smw.skinMatricesCount; ++i)
    {
        SkinnedMeshBone smb = SkinnedMeshBone::ReadFromRawBuffer(skinnedMeshBoneData, smw.boneRemapTableIndex + i);

        int boneTransformIndex = smb.boneRemapIndex + smw.animatedBoneIndexOffset;
        CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_COMPUTE_SKIN_MATRICES_RIG_SPACE_BONE_TRANSFORMS_READ, boneTransformIndex, rigSpaceBoneTransformsBuf);
        BoneTransform bt = rigSpaceBoneTransformsBuf[boneTransformIndex];

        float4x4 skinMatrix = bt.ToFloat4x4();
        skinMatrix = mul(w2l, skinMatrix);
        float4x4 outSkinMatrix = mul(skinMatrix, smb.bindPose);

        int skinMatrixOutIndex = smw.skinMatrixBaseOutIndex + i;
        CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_COMPUTE_SKIN_MATRICES_OUT_SKIN_MATRICES_WRITE, skinMatrixOutIndex, outSkinMatrices);
        outSkinMatrices[skinMatrixOutIndex] = (float3x4)outSkinMatrix;
    }
}

#endif //MAKE_SKIN_MATRICES_HLSL_
