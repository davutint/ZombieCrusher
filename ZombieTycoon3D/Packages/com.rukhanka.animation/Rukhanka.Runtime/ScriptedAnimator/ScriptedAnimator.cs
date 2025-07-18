using System;
using Rukhanka.Toolbox;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public static partial class ScriptedAnimator
{
    /// <summary>
    /// Clear animation to process component buffer, to clear current animation state. Usually used once per-frame.
    /// </summary>
    /// <param name="atps">Animation to process component buffer of animated entity.</param>
    public static void ResetAnimationState(ref DynamicBuffer<AnimationToProcessComponent> atps)
    {
        atps.Clear();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Instruct Rukhanka to play given animation at specified point of the time. You must call this function every
    /// frame with progressively advancing normalized time value.
    /// </summary>
    /// <param name="atps">Animation to process component buffer used to fill animations to. Use buffer from animated entity.</param>
    /// <param name="clip">Clip to play.</param>
    /// <param name="normalizedTime">Normalized play time (0 - beginning of the animation, 1 - end of the animation).</param>
    /// <param name="weight">Weight of the animation.</param>
    /// <param name="avatarMask">Optional avatar mask to use with state animations.</param>
    public static void PlayAnimation
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        BlobAssetReference<AnimationClipBlob> clip,
        float normalizedTime,
        float weight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        var atp = new AnimationToProcessComponent()
        {
            animation = clip,
            time = normalizedTime,
            weight = weight,
            avatarMask = avatarMask,
            blendMode = AnimationBlendingMode.Override,
            layerIndex = 0,
            layerWeight = 1,
            motionId = (uint)atps.Length
        };
        atps.Add(atp);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Instruct Rukhanka to blend given two animations at specified point of the time. You must call this function every
    /// frame with progressively advancing normalized time value.
    /// </summary>
    /// <param name="atps">Animation to process component buffer used to fill animations to. Use buffer from animated entity.</param>
    /// <param name="clip0">First clip of blending operation.</param>
    /// <param name="clip1">Second clip of blending operation.</param>
    /// <param name="normalizedTime">Normalized time (0 - beginning of the state, 1 - end of the state) of state to play.</param>
    /// <param name="blendFactor">Linear interpolation factor. Interpolate from clip0 to clip1 with range [0..1].</param>
    /// <param name="weight">Weight of the blending operation.</param>
    /// <param name="avatarMask">Optional avatar mask to use with state animations.</param>
    public static void BlendTwoAnimations
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        BlobAssetReference<AnimationClipBlob> clip0,
        BlobAssetReference<AnimationClipBlob> clip1,
        float normalizedTime,
        float blendFactor,
        float weight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        var atp = new AnimationToProcessComponent()
        {
            animation = clip0,
            time = normalizedTime,
            weight = (1 - blendFactor) * weight,
            avatarMask = avatarMask,
            blendMode = AnimationBlendingMode.Override,
            layerIndex = 0,
            layerWeight = 1,
            motionId = (uint)atps.Length
        };
        atps.Add(atp);
        
        atp.animation = clip1;
        atp.weight = blendFactor * weight;
        atp.motionId = (uint)atps.Length;
        atps.Add(atp);
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Instruct Rukhanka to play given blend tree at specified point of the time. You must call this function every
    /// frame with progressively advancing normalized time value
    /// </summary>
    /// <param name="atps">Animation to process component buffer used to fill animations to. Use buffer from animated entity.</param>
    /// <param name="blendTreeClips">Array of all animation clips that make up the blend tree.</param>
    /// <param name="blendTreeThresholds">1D blend tree coordinate positions of input clips. Size must match blendTreeClips length.</param>
    /// <param name="blendTreeParameterValue">Current blend tree coordinate position.</param>
    /// <param name="normalizedTime">Normalized time (0 - beginning of the state, 1 - end of the state) of state to play.</param>
    /// <param name="blendTreeWeight">Weight of the entire blend tree.</param>
    /// <param name="avatarMask">Optional avatar mask to use with state animations.</param>
    public static unsafe void PlayBlendTree1D
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        in NativeArray<BlobAssetReference<AnimationClipBlob>> blendTreeClips,
        in NativeArray<float> blendTreeThresholds,
        float blendTreeParameterValue,
        float normalizedTime,
        float blendTreeWeight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        BurstAssert.IsTrue(blendTreeClips.Length == blendTreeThresholds.Length, "Blend tree clips count must match thresholds array length");
        var bttSpan = new ReadOnlySpan<float>(blendTreeThresholds.GetUnsafeReadOnlyPtr(), blendTreeThresholds.Length);
        
        var motions = ComputeBlendTree1D(bttSpan, blendTreeParameterValue);
        
        for (var i = 0; i < motions.Length; ++i)
        {
            var m = motions[i];
            var atp = new AnimationToProcessComponent()
            {
                animation = blendTreeClips[m.motionIndex],
                time = normalizedTime,
                weight = m.weight * blendTreeWeight,
                avatarMask = avatarMask,
                blendMode = AnimationBlendingMode.Override,
                layerIndex = 0,
                layerWeight = 1,
                motionId = (uint)atps.Length
            };
            
            atps.Add(atp);
        }
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Instruct Rukhanka to play given blend tree at specified point of the time. You must call this function every
    /// frame with progressively advancing normalized time value
    /// </summary>
    /// <param name="atps">Animation to process component buffer used to fill animations to. Use buffer from animated entity.</param>
    /// <param name="blendTreeClips">Array of all animation clips that make up the blend tree.</param>
    /// <param name="blendTreePositions">2D blend tree coordinate positions of input clips. Size must match blendTreeClips length.</param>
    /// <param name="blendTreeParameterValue">2D blend coordinate position of current blend tree state.</param>
    /// <param name="normalizedTime">Normalized time (0 - beginning of the state, 1 - end of the state) of state to play.</param>
    /// <param name="blendTreeType">Blend tree type.</param>
    /// <param name="blendTreeWeight">Weight of the entire blend tree.</param>
    /// <param name="avatarMask">Optional avatar mask to use with state animations.</param>
    public static unsafe void PlayBlendTree2D
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        in NativeArray<BlobAssetReference<AnimationClipBlob>> blendTreeClips,
        in NativeArray<float2> blendTreePositions,
        float2 blendTreeParameterValue,
        float normalizedTime,
        MotionBlob.Type blendTreeType,
        float blendTreeWeight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        BurstAssert.IsTrue(blendTreeClips.Length == blendTreePositions.Length, "Blend tree clips and positions array lengths must match.");
        
        var bttSpan = new ReadOnlySpan<float2>(blendTreePositions.GetUnsafeReadOnlyPtr(), blendTreePositions.Length);
        
        var motions = blendTreeType switch
        {
	       MotionBlob.Type.BlendTree2DSimpleDirectional   => ComputeBlendTree2DSimpleDirectional(bttSpan, blendTreeParameterValue),
	       MotionBlob.Type.BlendTree2DFreeformCartesian   => ComputeBlendTree2DFreeformCartesian(bttSpan, blendTreeParameterValue),
	       MotionBlob.Type.BlendTree2DFreeformDirectional => ComputeBlendTree2DFreeformDirectional(bttSpan, blendTreeParameterValue),
	       _ => default
        };
        
        for (var i = 0; i < motions.Length; ++i)
        {
            var m = motions[i];
            var atp = new AnimationToProcessComponent()
            {
                animation = blendTreeClips[m.motionIndex],
                time = normalizedTime,
                weight = m.weight * blendTreeWeight,
                avatarMask = avatarMask,
                blendMode = AnimationBlendingMode.Override,
                layerIndex = 0,
                layerWeight = 1,
                motionId = (uint)atps.Length
            };
            
            atps.Add(atp);
        }
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get index of the state in layer with given name hash.
    /// </summary>
    /// <param name="cb">Controller layer blob</param>
    /// <param name="layerIndex">Later index state belongs to</param>
    /// <param name="stateHash">State name hash. Use "StateName".CalculateHash32() to obtain one.</param>
    /// <returns></returns>
    public static int GetStateIndexInControllerLayer(BlobAssetReference<ControllerBlob> cb, int layerIndex, uint stateHash)
    {
        ref var layerBlob = ref cb.Value.layers[layerIndex];
        for (var i = 0; i < layerBlob.states.Length; ++i)
        {
            ref var stateBlob = ref layerBlob.states[i];
            if (stateBlob.hash == stateHash)
                return i;
        }
        return -1;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Instruct Rukhanka to play given animator state at specified point of the time. You must call this function every
    /// frame with progressively advancing normalized time value.
    /// </summary>
    /// <param name="atps">Animation to process component buffer used to fill animations from requested state. Use buffer from animated entity.</param>
    /// <param name="animatorControllerParameters">Animator runtime parameters buffer of given animator controller. Use buffer from animated entity.</param>
    /// <param name="controllerBlob">Animator controller blob asset. Blob asset can be obtained from AnimatorControllerLayerComponent of animated entity.</param>
    /// <param name="animationsBlob">Animations blob asset to play with given controller. Usually can be obtained from AnimatorControllerLayerComponent of animated entity.</param>
    /// <param name="blobDatabase">Blob database singleton to query requested animations from animations blob.</param>
    /// <param name="layerIndex">Layer index of controller to play.</param>
    /// <param name="stateIndex">State index of layer to play. Index can be obtained using ScriptedAnimator.GetStateIndexInControllerLayer function.</param>
    /// <param name="normalizedTime">Normalized time (0 - beginning of the state, 1 - end of the state) of state to play.</param>
    /// <param name="weight">Weight of current state.</param>
    /// <param name="avatarMask">Optional avatar mask to use with state animations.</param>
    public static void PlayAnimatorState
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        in NativeArray<AnimatorControllerParameterComponent> animatorControllerParameters,
        in BlobAssetReference<ControllerBlob> controllerBlob,
        in BlobAssetReference<ControllerAnimationsBlob> animationsBlob,
        in BlobDatabaseSingleton blobDatabase,
        int layerIndex,
        int stateIndex,
        float normalizedTime,
        float weight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        BurstAssert.IsTrue(controllerBlob.IsCreated, "Controller blob is not valid");
        BurstAssert.IsTrue(animationsBlob.IsCreated, "Controller animations blob is not valid");
        
        BurstAssert.IsTrue(controllerBlob.Value.layers.Length > layerIndex, "Layer index is out of range of controller layers array");
        if (controllerBlob.Value.layers.Length <= layerIndex || layerIndex < 0)
            return;
        
        ref var lb = ref controllerBlob.Value.layers[layerIndex];
        
        BurstAssert.IsTrue(lb.states.Length > stateIndex, "State index is out of range of controller layer states array");
        if (lb.states.Length <= stateIndex || stateIndex < 0)
            return;
        
        ref var sb = ref lb.states[stateIndex];
        
        PlayMotion
        (
            ref atps,
            ref sb.motion,
            animatorControllerParameters,
            animationsBlob,
            blobDatabase,
            normalizedTime,
            weight,
            avatarMask
        );
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Start cross fade (linear blend) of current controller layer state and any other layer state with specified transition properties.
    /// This function is full analog of Unity's Animator.CrossFade API.
    /// </summary>
    /// <remarks>This is "fire and forget" function. It needs to be called only once to start transition. Transition flow will continue authomatically.</remarks>
    /// <param name="animatorControllerLayer">Controller layer to perform transition.</param>
    /// <param name="stateIndex">State index to crossfade into.</param>
    /// <param name="normalizedTransitionDuration">Transition duration as a fraction of current state length. I.e. if current state length is 3 sec, and
    ///     normalizedTransitionDuration is 0.5f, then transition duration will be 1.5 sec.</param>
    /// <param name="normalizedTimeOffset">Offset of start point of target state (defined by state index) in transition.</param>
    /// <param name="normalizedTransitionTime">Offset of transition start point.</param>
    /// <see href="https://docs.unity3d.com/ScriptReference/Animator.CrossFade.html">Unity Animator.CrossFade</see>
    public static void CrossFade
    (
        ref AnimatorControllerLayerComponent animatorControllerLayer,
        int stateIndex,
        float normalizedTransitionDuration,
        float normalizedTimeOffset = 0,
        float normalizedTransitionTime = 0
    )
    {
        var rt = new RuntimeAnimatorData.TransitionRuntimeData()
        {
            id = 0xffffff,
            length = -normalizedTransitionDuration,
            normalizedDuration = normalizedTransitionTime
        };
        animatorControllerLayer.rtd.activeTransition = rt;
        animatorControllerLayer.rtd.dstState.id = stateIndex;
        animatorControllerLayer.rtd.dstState.normalizedDuration = normalizedTimeOffset;
    }
}
}
