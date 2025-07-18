using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{ 

internal struct AvatarMaskBakingDataBlob
{
#if RUKHANKA_DEBUG_INFO
	public BlobString name;
	public BlobArray<BlobString> includedBoneNames;
	public float bakingTime;
#endif
	public Hash128 hash;
	public BlobArray<uint> includedBoneHashes;
	public uint humanBodyPartsAvatarMask;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	
[TemporaryBakingType]
internal struct AvatarMaskBakingData: IBufferElementData
{
	public Entity rigEntity;
	public BlobAssetReference<AvatarMaskBakingDataBlob> dataBlob;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[RequireMatchingQueriesForUpdate]
partial class AvatarMaskBakingSystem: SystemBase
{
	BakingSystem bakingSystem;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnCreate()
	{
		bakingSystem = World.GetExistingSystemManaged<BakingSystem>();
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnUpdate()
	{
		DynamicBuffer<NewBlobAssetDatabaseRecord<AvatarMaskBlob>> newBlobAssetRecords = default;
		var ecb = new EntityCommandBuffer(Allocator.Temp);
		
		foreach (var (avatarMaskDataArr, e) in SystemAPI.Query<DynamicBuffer<AvatarMaskBakingData>>()
			         .WithEntityAccess().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
		{
			foreach (var am in avatarMaskDataArr)
			{
				if (!EntityManager.HasComponent<RigDefinitionComponent>(am.rigEntity))
					continue;
				
				var rigDef = EntityManager.GetComponentData<RigDefinitionComponent>(am.rigEntity);
				var amb = MakeMaskForAvatar(rigDef.rigBlob, am.dataBlob);	
			
				if (!newBlobAssetRecords.IsCreated)
				{
					newBlobAssetRecords = ecb.AddBuffer<NewBlobAssetDatabaseRecord<AvatarMaskBlob>>(e);
				}
				var newAvatarMaskBlob = new NewBlobAssetDatabaseRecord<AvatarMaskBlob>()
				{
					hash = amb.Value.hash,
					value = amb
				};
				newBlobAssetRecords.Add(newAvatarMaskBlob);
			}	
		}
		ecb.Playback(EntityManager);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	BlobAssetReference<AvatarMaskBlob> MakeMaskForAvatar(BlobAssetReference<RigDefinitionBlob> rdb, BlobAssetReference<AvatarMaskBakingDataBlob> am)
	{
		var l = rdb.Value.bones.Length;
		var avatarMaskContainerLength = (int)math.ceil(l / 32.0f); 
		var bb = new BlobBuilder(Allocator.Temp);
		ref var ambBuilder = ref bb.ConstructRoot<AvatarMaskBlob>();
        ambBuilder.hash = am.Value.hash;
        ambBuilder.humanBodyPartsAvatarMask = am.Value.humanBodyPartsAvatarMask;
        
	#if RUKHANKA_DEBUG_INFO
		if (am.Value.name.Length > 0)
			bb.AllocateString(ref ambBuilder.name, am.Value.name.ToString());
		var includedBoneNames = bb.Allocate(ref ambBuilder.includedBoneNames, am.Value.includedBoneNames.Length);
		for (var i = 0; i < includedBoneNames.Length; ++i)
		{
			bb.AllocateString(ref includedBoneNames[i], am.Value.includedBoneNames[i].ToString());
		}
		ambBuilder.bakingTime = am.Value.bakingTime;
	#endif
        
		var maskArr = bb.Allocate(ref ambBuilder.includedBoneMask, avatarMaskContainerLength);
		
		for (var i = 0; i < l; ++i)
		{
			ref var rigBone = ref rdb.Value.bones[i];
			var maskEntriesCount = am.Value.includedBoneHashes.Length;
			var j = 0;
			for (; j < maskEntriesCount; ++j)
			{
				var maskBoneHash = am.Value.includedBoneHashes[j];
				if (maskBoneHash == rigBone.hash)
					break;
			}
			
			if (j < maskEntriesCount)
			{
				var (uintIndex, mask) = AvatarMaskBlob.GetUintIndexAndMask(i);
				var avatarMaskValue = maskArr[uintIndex];
				avatarMaskValue |= mask;	
				maskArr[uintIndex] = avatarMaskValue;
			}
		}
		
		var amb = bb.CreateBlobAssetReference<AvatarMaskBlob>(Allocator.Persistent);
		bakingSystem.BlobAssetStore.TryAdd(am.Value.hash, ref amb);
		return amb;
	}
}

}
