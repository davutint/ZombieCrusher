#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
    //  Just a copy of EG SkinMatrix
    public struct SkinMatrix: IBufferElementData
    {
        public float3x4 Value;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //  Just a copy of EG BlendShapeWeight
    public struct BlendShapeWeight : IBufferElementData
    {
        public float Value;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
    //	DEPRECATED: remove me
    [MaterialProperty("_DotsDeformationParams")]
    public struct DeformedMeshIndexDeprecated: IComponentData
    {
        public uint4 Value;
    }
    [MaterialProperty("_DeformationParamsForMotionVectors")]
    public struct DeformedMeshIndex: IComponentData
    {
        public uint4 Value;
    }
#else
    //	DEPRECATED: remove me
    [MaterialProperty("_ComputeMeshIndex")]
    public struct DeformedMeshIndexDeprecated: IComponentData
    {
        public uint Value;
    }
    [MaterialProperty("_DeformedMeshIndex")]
    public struct DeformedMeshIndex: IComponentData
    {
        public uint Value;
    }
#endif
}

#endif