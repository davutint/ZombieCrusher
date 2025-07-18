#ifndef AVATAR_MASK_HLSL_
#define AVATAR_MASK_HLSL_

/////////////////////////////////////////////////////////////////////////////////

//  uint
ByteAddressBuffer avatarMasksBuffer;

/////////////////////////////////////////////////////////////////////////////////

bool IsBoneInGenericAvatarMask(int avatarMaskDataOffset, int boneIndex)
{
	int uintIndex = boneIndex >> 5; // boneIndex / 32
	int byteOffsetInUint = boneIndex & 0x1f; // boneIndex % 32
	uint boneMask = 1u << byteOffsetInUint;

    // First uint in mask is human body parts mask, skip it
    uint absMaskDataIndex = avatarMaskDataOffset + 1 + uintIndex;
    uint avatarMask = avatarMasksBuffer.Load(absMaskDataIndex * 4);

    CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_GENERIC_AVATAR_AVATAR_MASK_BUFFER_READ, absMaskDataIndex * 4, 4, avatarMasksBuffer);

    return (avatarMask & boneMask) != 0;
}

/////////////////////////////////////////////////////////////////////////////////

bool IsBoneInHumanAvatarMask(int avatarMaskDataOffset, int humanAvatarMaskBodyPart)
{
    uint absMaskDataIndex = avatarMaskDataOffset;
    uint humanAvatarMask = avatarMasksBuffer.Load(absMaskDataIndex * 4);

    CHECK_RAW_BUFFER_OUT_OF_BOUNDS(RUKHANKADEBUGMARKERS_GPUANIMATOR_HUMANOID_AVATAR_AVATAR_MASK_BUFFER_READ, absMaskDataIndex * 4, 4, avatarMasksBuffer);

    return (humanAvatarMask & 1 << humanAvatarMaskBodyPart) != 0;
}

/////////////////////////////////////////////////////////////////////////////////

bool IsBoneInAvatarMask(int avatarMaskDataOffset, int humanAvatarMaskBodyPart, int boneIndex)
{
    if (avatarMaskDataOffset < 0)
        return true;

    bool rv = humanAvatarMaskBodyPart < 0 ?
        IsBoneInGenericAvatarMask(avatarMaskDataOffset, boneIndex) :
        IsBoneInHumanAvatarMask(avatarMaskDataOffset, humanAvatarMaskBodyPart);
    return rv;
}

#endif


