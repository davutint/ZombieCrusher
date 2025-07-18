#if RUKHANKA_DEBUG_INFO

using Rukhanka.DebugDrawer;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public class GPUBoneRenderer
{
	Material boneDrawerMat;
	Mesh boneMesh;
	
	readonly int ShaderID_animatedBoneWorkload = Shader.PropertyToID("animatedBoneWorkload");
	readonly int ShaderID_animationJobs = Shader.PropertyToID("animationJobs");
	readonly int ShaderID_rigDefinitions = Shader.PropertyToID("rigDefinitions");
	readonly int ShaderID_rigBones = Shader.PropertyToID("rigBones");
	readonly int ShaderID_rigSpaceBoneTransforms = Shader.PropertyToID("rigSpaceBoneTransforms");
	readonly int ShaderID_rigVisualizationData = Shader.PropertyToID("rigVisualizationData");
	
	GraphicsBuffer framePerBoneAnimationWorkloadGB;
	GraphicsBuffer frameRigAnimationJobsGB;
	GraphicsBuffer rigDefinitionGB;
	GraphicsBuffer rigBonesGB;
	GraphicsBuffer rigSpaceAnimatedBonesGB;
	int frameAnimatedBonesCount;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public GPUBoneRenderer()
	{
		boneDrawerMat = CoreUtils.CreateEngineMaterial(Shader.Find($"BoneRendererGPU"));
		boneDrawerMat.enableInstancing = true;
		boneMesh = DrawerManagedSingleton.CreateBoneMesh();
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void SetGPUBuffersForFrame
	(
		GraphicsBuffer framePerBoneAnimationWorkloadGB,
		GraphicsBuffer frameRigAnimationJobsGB,
		GraphicsBuffer rigDefinitionGB,
		GraphicsBuffer rigBonesGB,
		GraphicsBuffer rigSpaceAnimatedBonesGB,
		int frameAnimatedBonesCount
	)
	{
		this.framePerBoneAnimationWorkloadGB = framePerBoneAnimationWorkloadGB;
		this.frameRigAnimationJobsGB = frameRigAnimationJobsGB;
		this.rigDefinitionGB = rigDefinitionGB;
		this.rigBonesGB = rigBonesGB;
		this.rigSpaceAnimatedBonesGB = rigSpaceAnimatedBonesGB;
		this.frameAnimatedBonesCount = frameAnimatedBonesCount;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void RenderBones(GraphicsBuffer rigVisualizationDataGB, float4 boneColor)
	{
        var rp = new RenderParams();
        rp.camera = null;
        rp.layer = 0;
        rp.lightProbeProxyVolume = null;
        rp.lightProbeUsage = LightProbeUsage.Off;
        rp.motionVectorMode = MotionVectorGenerationMode.ForceNoMotion;
        rp.receiveShadows = false;
        rp.reflectionProbeUsage = ReflectionProbeUsage.Off;
        rp.rendererPriority = 0;
        rp.renderingLayerMask = 0xffffffff;
        rp.shadowCastingMode = ShadowCastingMode.Off;
        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * 100000);
		
		rp.matProps = new MaterialPropertyBlock();
		rp.matProps.SetBuffer(ShaderID_animatedBoneWorkload, framePerBoneAnimationWorkloadGB);
		rp.matProps.SetBuffer(ShaderID_animationJobs, frameRigAnimationJobsGB);
		rp.matProps.SetBuffer(ShaderID_rigDefinitions, rigDefinitionGB);
		rp.matProps.SetBuffer(ShaderID_rigBones, rigBonesGB);
		rp.matProps.SetBuffer(ShaderID_rigSpaceBoneTransforms, rigSpaceAnimatedBonesGB);
		rp.matProps.SetBuffer(ShaderID_rigVisualizationData, rigVisualizationDataGB);
		rp.material = boneDrawerMat;
		
		rp.matProps.SetVector("boneColor", boneColor);
		Graphics.RenderMeshPrimitives(rp, boneMesh, 1, frameAnimatedBonesCount);
		boneColor.w = 0.3f;
		rp.matProps.SetVector("boneColor", boneColor);
		Graphics.RenderMeshPrimitives(rp, boneMesh, 0, frameAnimatedBonesCount);
	}
}
}

#endif
