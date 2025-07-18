#if UNITY_EDITOR

using System.Collections.Generic;
using Rukhanka.Toolbox;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using FixedStringName = Unity.Collections.FixedString512Bytes;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{ 
public class AvatarMaskBaker
{
	internal BlobAssetReference<AvatarMaskBakingDataBlob> CreateAvatarMaskBlob(IBaker baker, AvatarMask am, RigDefinitionAuthoring rd)
	{
		if (am == null)
			return default;
		
		var blobHash = BakingUtils.ComputeAvatarMaskHash(am, rd);
		var blobExists = baker.TryGetBlobAssetReference<AvatarMaskBakingDataBlob>(blobHash, out var avatarMaskBlob);
		if (blobExists)
			return avatarMaskBlob;
		
		var bb = new BlobBuilder(Allocator.Temp);
		ref var amb = ref bb.ConstructRoot<AvatarMaskBakingDataBlob>();
		amb.hash = blobHash;	
	#if RUKHANKA_DEBUG_INFO
		if (am.name.Length > 0)
			bb.AllocateString(ref amb.name, am.name);
		var startTimeMarker = Time.realtimeSinceStartup;
	#endif
		
		//	Generic avatar mask
		var avatarMaskIncludedBones = new List<string>();
		for (int i = 0; i < am.transformCount; ++i)
		{
			var bonePath = am.GetTransformPath(i);
			var boneActive = am.GetTransformActive(i);
			if (bonePath.Length == 0 || !boneActive) continue;
			var boneNames = bonePath.Split('/');
			var leafBoneName = boneNames[^1];
			avatarMaskIncludedBones.Add(leafBoneName);
		}
		
		var includedBoneHashes = bb.Allocate(ref amb.includedBoneHashes, avatarMaskIncludedBones.Count);
	#if RUKHANKA_DEBUG_INFO
		var includedBonePaths = bb.Allocate(ref amb.includedBoneNames, avatarMaskIncludedBones.Count);
	#endif
		
		for (var i = 0; i < includedBoneHashes.Length; ++i)
		{
			var leafBoneName = avatarMaskIncludedBones[i];
		#if RUKHANKA_DEBUG_INFO
			bb.AllocateString(ref includedBonePaths[i], leafBoneName);
		#endif
			includedBoneHashes[i] = new FixedStringName(leafBoneName).CalculateHash32();
		}

		//	Humanoid avatar mask
		var humanBodyPartsCount = (int)AvatarMaskBodyPart.LastBodyPart;
		amb.humanBodyPartsAvatarMask = 0;
		for (int i = 0; i < humanBodyPartsCount; ++i)
		{
			var ambp = (AvatarMaskBodyPart)i;
			if (am.GetHumanoidBodyPartActive(ambp))
				amb.humanBodyPartsAvatarMask |= 1u << i;
		}

	#if RUKHANKA_DEBUG_INFO
		var dt = Time.realtimeSinceStartupAsDouble - startTimeMarker;
		amb.bakingTime = (float)dt;
	#endif
		
		var rv = bb.CreateBlobAssetReference<AvatarMaskBakingDataBlob>(Allocator.Persistent);
		baker.AddBlobAssetWithCustomHash(ref rv, blobHash);
		
		return rv;
	}
}
}

#endif