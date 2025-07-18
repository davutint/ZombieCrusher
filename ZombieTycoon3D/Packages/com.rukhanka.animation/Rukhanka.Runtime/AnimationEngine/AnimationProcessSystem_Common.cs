
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

//=================================================================================================================//

namespace Rukhanka
{
partial struct AnimationProcessSystem
{
    static bool ComputeAnimatedProperty(ref float animatedValue, in NativeArray<AnimationToProcessComponent> animations, ReadOnlySpan<float> layerWeights, uint trackHash, uint propHash)
    {
		var cumulativeCurveValue = 0f;
		var hasCurveValue = false;
		
		for (int l = 0; l < animations.Length; ++l)
		{
			var atp = animations[l];
			if (atp.animation == BlobAssetReference<AnimationClipBlob>.Null)
				continue;
			
			var animTime = ComputeBoneAnimationJob.NormalizeAnimationTime(atp.time, ref atp.animation.Value);
			var layerWeight = layerWeights[atp.layerIndex];
			ref var trackSet = ref atp.animation.Value.clipTracks;
			var weight = atp.weight * layerWeight;
			
			if (weight <= 0)
				continue;
			
			var trackGroupIndex = trackSet.trackGroupPHT.Query(trackHash);
			if (trackGroupIndex < 0)
				continue;
			
			var trackRange = new int2(trackSet.trackGroups[trackGroupIndex], trackSet.trackGroups[trackGroupIndex + 1]);
			for (var k = trackRange.x; k < trackRange.y; ++k)
			{
				var track = trackSet.tracks[k];
				if (track.props == propHash)
				{
					var curveValue = SampleTrack(ref trackSet, k, atp, animTime.x);
					if (atp.animation.Value.loopPoseBlend)
						curveValue -= CalculateTrackLoopValue(ref trackSet, k, atp, animTime.y);
					cumulativeCurveValue += curveValue * weight;	
					hasCurveValue = true;
					break;
				}
			}
		}
		
		if (hasCurveValue)
			animatedValue = cumulativeCurveValue;
		
		return hasCurveValue;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static float SampleTrack(ref TrackSet trackSet, int trackIndex, in AnimationToProcessComponent atp, float animTime)
	{ 
		var curveValue = BlobCurve.SampleAnimationCurve(ref trackSet, trackIndex, animTime);
		//	Make additive animation if requested
		if (atp.blendMode == AnimationBlendingMode.Additive)
		{
			var additiveValue = BlobCurve.SampleAnimationCurve(ref trackSet, trackIndex, 0);
			curveValue -= additiveValue;
		}
		return curveValue;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static float CalculateTrackLoopValue(ref TrackSet trackSet, int trackIndex, in AnimationToProcessComponent atp, float normalizedTime)
	{
		var startV = SampleTrack(ref trackSet, trackIndex, atp, 0);
		var endV = SampleTrack(ref trackSet, trackIndex, atp, atp.animation.Value.length);

		var rv = (endV - startV) * normalizedTime;
		return rv;
	}
}
}
