
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
//  If present and enabled, then GPU animation engine is used
public struct GPUAnimationEngineTag: IComponentData, IEnableableComponent { }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//  Used to move attachments using GPU computed bone poses in shader
[MaterialProperty("_RukhankaGPUBoneIndex")]
public struct GPUAttachmentBoneIndexMPComponent: IComponentData
{
    public int boneIndex; 
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[MaterialProperty("_RukhankaAttachmentToBoneTransform")]
public struct GPUAttachmentToBoneTransformMPComponent: IComponentData
{
    public float4x4 value; 
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[MaterialProperty("_RukhankaAnimatedEntityLocalToWorld")]
public struct GPURigEntityLocalToWorldMPComponent: IComponentData
{
    public float4x4 value; 
}
}
