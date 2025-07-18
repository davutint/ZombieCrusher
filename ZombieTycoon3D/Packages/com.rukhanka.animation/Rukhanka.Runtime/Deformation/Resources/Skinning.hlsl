#ifndef SKINNING_HLSL_
#define SKINNING_HLSL_

/////////////////////////////////////////////////////////////////////////////////

#pragma warning (disable: 4000)
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/BoneTransform.hlsl"
#include "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/GPUStructures/DualQuaternion.hlsl"

/////////////////////////////////////////////////////////////////////////////////

ByteAddressBuffer framePerVertexWorkload;
//  SourceSkinnedMeshVertex
ByteAddressBuffer inputMeshVertexData;
//  BoneInfluence
ByteAddressBuffer inputBoneInfluences;
//  DeformedVertex
ByteAddressBuffer inputBlendShapes;

StructuredBuffer<float3x4> frameSkinMatrices;
StructuredBuffer<float> frameBlendShapeWeights;

#ifdef RUKHANKA_HALF_DEFORMED_DATA
RWStructuredBuffer<PackedDeformedVertex> outDeformedVertices;
#else
RWStructuredBuffer<DeformedVertex> outDeformedVertices;
#endif

uint totalSkinnedVerticesCount;
uint voidMeshVertexCount;

/////////////////////////////////////////////////////////////////////////////////

DeformedVertex ApplyBlendShapes(DeformedVertex v, uint meshVertexIndex, MeshFrameDeformationDescription mfd)
{
    if (mfd.baseBlendShapeWeightIndex < 0)
        return v;

    for (int i = 0; i < mfd.meshBlendShapesCount; ++i)
    {
        float blendShapeWeight = frameBlendShapeWeights[mfd.baseBlendShapeWeightIndex + i];
        if (blendShapeWeight == 0)
            continue;

        DeformedVertex blendShapeDelta = DeformedVertex::ReadFromRawBuffer
        (
            inputBlendShapes,
            mfd.baseInputMeshBlendShapeIndex + meshVertexIndex + i * mfd.meshVerticesCount
        );
        blendShapeDelta.Scale(blendShapeWeight * 0.01f);

        v.position += blendShapeDelta.position;
        v.normal += blendShapeDelta.normal;
        v.tangent += blendShapeDelta.tangent;
    }

    return v;
}

/////////////////////////////////////////////////////////////////////////////////

DeformedVertex ApplySkinMatrices
(
    DeformedVertex v,
    uint vertexBoneWeightsOffset,
    uint vertexBoneWeightsCount,
    MeshFrameDeformationDescription mfd
)
{
    if (mfd.baseSkinMatrixIndex < 0 || vertexBoneWeightsCount == 0)
        return v;

    //  Skinning loop
#ifdef RUKHANKA_DUAL_QUATERNION_SKINNING
    DualQuaternion adq = (DualQuaternion)0;
#endif
    DeformedVertex rv = (DeformedVertex)0;

    float4 refRot = 0;
    float3 skinnedScale = 0;

    for (uint i = 0; i < vertexBoneWeightsCount; ++i)
    {
        uint boneInfluenceIndex = i + vertexBoneWeightsOffset;
        BoneInfluence bi = BoneInfluence::ReadFromRawBuffer(inputBoneInfluences, boneInfluenceIndex);

        int skinMatrixIndex = bi.boneIndex + mfd.baseSkinMatrixIndex;
        CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_DEFORMATION_FRAME_SKIN_MATRICES_READ, skinMatrixIndex, frameSkinMatrices);
        float3x4 skinMatrix = frameSkinMatrices[skinMatrixIndex];

    #ifdef RUKHANKA_DUAL_QUATERNION_SKINNING
        BoneTransform skinPose = BoneTransform::FromMatrix(skinMatrix);
        
        skinnedScale += skinPose.scale * bi.weight;

        if (i == 0)
            refRot = skinPose.rot.value;
        else if (dot(skinPose.rot.value, refRot) < 0)
           bi.weight = -bi.weight;

        DualQuaternion dq = DualQuaternion::Construct(skinPose.pos, skinPose.rot);
        DualQuaternion sdq = DualQuaternion::Scale(dq, bi.weight);
        adq = DualQuaternion::Add(adq, sdq);
    #else
        rv.position += mul(skinMatrix, float4(v.position, 1)) * bi.weight;
        rv.normal   += mul(skinMatrix, float4(v.normal, 0))   * bi.weight;
        rv.tangent  += mul(skinMatrix, float4(v.tangent, 0))  * bi.weight;
    #endif


    }
#ifdef RUKHANKA_DUAL_QUATERNION_SKINNING
    adq = DualQuaternion::Normalize(adq);

    BoneTransform bt = adq.GetBoneTransform();
    bt.scale = skinnedScale;

    rv.position = BoneTransform::TransformPoint(bt, v.position);
    rv.normal   = BoneTransform::TransformDirection(bt, v.normal);
    rv.tangent  = BoneTransform::TransformDirection(bt, v.tangent);
#endif
    
    return rv;
}

/////////////////////////////////////////////////////////////////////////////////

void Skinning(uint tid)
{
    //  Skip zero vertex because it is uninitialized marker
    if (tid >= totalSkinnedVerticesCount || tid == 0)
    {
        if (tid < totalSkinnedVerticesCount + voidMeshVertexCount)
        {
        #ifdef RUKHANKA_HALF_DEFORMED_DATA
            outDeformedVertices[tid] = (PackedDeformedVertex)0;
        #else
            outDeformedVertices[tid] = (DeformedVertex)0;
        #endif
            CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_DEFORMATION_COPY_MESH_DATA, tid, outDeformedVertices);
        }
        return;
    }

    CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_DEFORMATION_PER_VERTEX_WORKLOAD_READ, tid * 4, 4, framePerVertexWorkload);
    uint frameDeformedMeshIndex = framePerVertexWorkload.Load(tid * 4);
    CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_DEFORMATION_FRAME_DEFORMED_VERTEX_READ, frameDeformedMeshIndex, frameDeformedMeshes);
    MeshFrameDeformationDescription mfd = frameDeformedMeshes[frameDeformedMeshIndex];
    uint meshVertexIndex = tid - mfd.baseOutVertexIndex;
    uint absoluteInputMeshVertexIndex = meshVertexIndex + mfd.baseInputMeshVertexIndex;
    SourceSkinnedMeshVertex smv = SourceSkinnedMeshVertex::ReadFromRawBuffer(inputMeshVertexData, absoluteInputMeshVertexIndex);

    DeformedVertex rv = (DeformedVertex)0;
#ifndef RUKHANKA_INPLACE_SKINNING
    rv.position = smv.position;
    rv.tangent = smv.tangent;
    rv.normal = smv.normal;
#endif

    rv = ApplyBlendShapes(rv, meshVertexIndex, mfd);
    rv = ApplySkinMatrices(rv, smv.boneWeightsOffset, smv.boneWeightsCount, mfd);

#ifdef RUKHANKA_HALF_DEFORMED_DATA
    outDeformedVertices[tid] = PackedDeformedVertex::Pack(rv);
#else
    outDeformedVertices[tid] = rv;
#endif
    CHECK_STRUCTURED_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_DEFORMATION_DEFORMED_VERTEX_WRITE, tid, outDeformedVertices);
}

/////////////////////////////////////////////////////////////////////////////////

[numthreads(128, 1, 1)]
void Skinning_128(uint tid: SV_DispatchThreadID) { Skinning(tid); }
[numthreads(256, 1, 1)]
void Skinning_256(uint tid: SV_DispatchThreadID) { Skinning(tid); }
[numthreads(512, 1, 1)]
void Skinning_512(uint tid: SV_DispatchThreadID) { Skinning(tid); }
[numthreads(1024, 1, 1)]
void Skinning_1024(uint tid: SV_DispatchThreadID) { Skinning(tid); }
[numthreads(1024, 1, 1)]
void Skinning_2048(uint tid: SV_DispatchThreadID) { Skinning(tid); }

#endif // SKINNING_HLSL_
