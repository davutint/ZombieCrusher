#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public partial class MeshDeformationSystem
{
	readonly int ShaderID_inputVertexSizeInBytes = Shader.PropertyToID("inputVertexSizeInBytes");
	readonly int ShaderID_outDataVertexOffset = Shader.PropertyToID("outDataVertexOffset");
	readonly int ShaderID_totalMeshVertices = Shader.PropertyToID("totalMeshVertices");
	readonly int ShaderID_meshVertexData = Shader.PropertyToID("meshVertexData");
	readonly int ShaderID_outInitialDeformedMeshData = Shader.PropertyToID("outInitialDeformedMeshData");
	readonly int ShaderID_meshBonesPerVertexData = Shader.PropertyToID("meshBonesPerVertexData");
	readonly int ShaderID_inputBonesWeightsDataOffset = Shader.PropertyToID("inputBonesWeightsDataOffset");
	readonly int ShaderID_outBonesWeightsDataOffset = Shader.PropertyToID("outBonesWeightsDataOffset");
	readonly int ShaderID_frameDeformedMeshes = Shader.PropertyToID("frameDeformedMeshes");
	readonly int ShaderID_outFramePerVertexWorkload = Shader.PropertyToID("outFramePerVertexWorkload");
	readonly int ShaderID_framePerVertexWorkload = Shader.PropertyToID("framePerVertexWorkload");
	readonly int ShaderID_inputMeshVertexData = Shader.PropertyToID("inputMeshVertexData");
	readonly int ShaderID_inputBoneInfluences = Shader.PropertyToID("inputBoneInfluences");
	readonly int ShaderID_inputBlendShapes = Shader.PropertyToID("inputBlendShapes");
	readonly int ShaderID_frameSkinMatrices = Shader.PropertyToID("frameSkinMatrices");
	readonly int ShaderID_frameBlendShapeWeights = Shader.PropertyToID("frameBlendShapeWeights");
	readonly int ShaderID_outDeformedVertices = Shader.PropertyToID("outDeformedVertices");
	readonly int ShaderID_totalDeformedMeshesCount = Shader.PropertyToID("totalDeformedMeshesCount");
	readonly int ShaderID_totalSkinnedVerticesCount = Shader.PropertyToID("totalSkinnedVerticesCount");
	readonly int ShaderID_voidMeshVertexCount = Shader.PropertyToID("voidMeshVertexCount");
	readonly int ShaderID_DeformedMeshData = Shader.PropertyToID("_DeformedMeshData");
	readonly int ShaderID_PreviousFrameDeformedMeshData = Shader.PropertyToID("_PreviousFrameDeformedMeshData");
	readonly int ShaderID_meshBlendShapesBuffer = Shader.PropertyToID("meshBlendShapesBuffer");
	readonly int ShaderID_outInitialMeshBlendShapesData = Shader.PropertyToID("outInitialMeshBlendShapesData");
	readonly int ShaderID_inputBlendShapeVerticesCount = Shader.PropertyToID("inputBlendShapeVerticesCount");
	readonly int ShaderID_inputBlendShapeVertexOffset = Shader.PropertyToID("inputBlendShapeVertexOffset");
	readonly int ShaderID_outBlendShapeVertexOffset = Shader.PropertyToID("outBlendShapeVertexOffset");
}
}

#endif
