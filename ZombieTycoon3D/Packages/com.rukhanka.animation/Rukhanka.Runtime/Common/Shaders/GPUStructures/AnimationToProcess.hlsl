#ifndef ANIMATION_TO_PROCESS_HLSL_
#define ANIMATION_TO_PROCESS_HLSL_

/////////////////////////////////////////////////////////////////////////////////

#define BLEND_MODE_OVERRIDE 0
#define BLEND_MODE_ADDITIVE 1

/////////////////////////////////////////////////////////////////////////////////

struct AnimationToProcess
{
    int animationClipAddress;
    float weight;
    float time;
    int blendMode;
	float layerWeight;
	int layerIndex;
    int avatarMaskDataOffset;
};

#endif


