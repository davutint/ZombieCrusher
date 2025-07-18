using Unity.Entities;
using Unity.Entities.Hybrid.Baking;
using Unity.Rendering;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[RequireMatchingQueriesForUpdate]
public partial class GPUAttachmentMeshBakingSystem: SystemBase
{
	protected override void OnUpdate()
	{
		var ecb = new EntityCommandBuffer(CheckedStateRef.WorldUpdateAllocator);

		//	GPU attachments support for submesh render entities
		foreach(var (aeb, gac0, gac1, gac2, e) in SystemAPI.Query
			        <DynamicBuffer<AdditionalEntitiesBakingData>,
				     GPUAttachmentBoneIndexMPComponent,
				     GPUAttachmentToBoneTransformMPComponent,
				     GPURigEntityLocalToWorldMPComponent>()
			        .WithNone<MaterialMeshInfo>()
			        .WithEntityAccess()
			        .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
		{
			//	This is submesh render entity. Propagate GPU attachment components from parent to the children
			for (var i = 0; i < aeb.Length; ++i)
			{
				var childRenderEntity = aeb[i].Value;
				ecb.AddComponent(childRenderEntity, gac0);
				ecb.AddComponent(childRenderEntity, gac1);
				ecb.AddComponent(childRenderEntity, gac2);
			}
			ecb.RemoveComponent<GPUAttachmentBoneIndexMPComponent>(e);
			ecb.RemoveComponent<GPUAttachmentToBoneTransformMPComponent>(e);
			ecb.RemoveComponent<GPURigEntityLocalToWorldMPComponent>(e);
		}
		
		ecb.Playback(EntityManager);
	}
}
}
