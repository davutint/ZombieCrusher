using Unity.Collections;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.Hybrid.Baking;
using Unity.Rendering;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[TemporaryBakingType]
internal struct AnimatedRendererBakingComponent: IComponentData
{
	public Entity animatorEntity;
	public bool needUpdateRenderBounds;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(SkinnedMeshConversionSystem))]
public partial class AnimatedRendererBakingSystem : SystemBase
{
	partial struct CreateAnimatedRendererComponentsJob: IJobEntity
	{
		[ReadOnly]
		public BufferLookup<AdditionalEntitiesBakingData> additionalEntitiesBufferLookup;
		[ReadOnly]
		public ComponentLookup<AnimatedSkinnedMeshComponent> animatedSkinnedMeshComponentLookup;
		
		public EntityCommandBuffer ecb;
		public EntityManager em;
		
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		unsafe void Execute([EntityIndexInChunk] int entityIndexInChunk, Entity e, AnimatedRendererBakingComponent arbc)
		{
			var arc = new AnimatedRendererComponent()
			{
				animatorEntity = arbc.animatorEntity,
				skinnedMeshEntity = e,
			};
			
			if (animatedSkinnedMeshComponentLookup.HasComponent(e))
			{
				var smreb = ecb.AddBuffer<SkinnedMeshRenderEntity>(e);
				
				// Add AnimatedRendererComponent to its additional mesh render entities
				if (additionalEntitiesBufferLookup.TryGetBuffer(e, out var additionalEntitiesBuf))
				{
					foreach (var ae in additionalEntitiesBuf)
					{
						ecb.AddComponent(ae.Value, arc);
						smreb.Add(new SkinnedMeshRenderEntity() { value = ae.Value });
						if (arbc.needUpdateRenderBounds)
							ecb.AddComponent<ShouldUpdateBoundingBoxTag>(ae.Value);
					}
				}
				
				// Propagate MaterialOverrides from SMR entity to individual render entities
				var c = em.GetChunk(e);
				var at = c.Archetype;
				for (var i = 0; i < at.TypesCount; ++i)
				{
					var t = at.Types[i];
					var tt = TypeManager.GetTypeInfo(t.TypeIndex);
					var ca = tt.Type.GetCustomAttributes(typeof(MaterialPropertyAttribute), false);
					if (ca.Length == 0)
						continue;
					
					var rawData = UnsafeUtility.Malloc(tt.TypeSize, tt.AlignmentInBytes, Allocator.Temp);
					
					var cth = em.GetDynamicComponentTypeHandle(t.ToComponentType());
				#if ENTITIES_V110_OR_NEWER
					var componentDataArrPtr = (byte*)c.GetDynamicComponentDataPtr(ref cth);
				#else
					var componentDataArrPtr = (byte*)c.GetDynamicComponentDataArrayReinterpret<uint>(ref cth, 4).GetUnsafeReadOnlyPtr();
				#endif
					var componentDataForEntityPtr = componentDataArrPtr + tt.TypeSize * entityIndexInChunk;
					
					foreach (var ae in additionalEntitiesBuf)
					{
						UnsafeUtility.MemCpy(rawData, componentDataForEntityPtr, tt.TypeSize);
						ecb.UnsafeAddComponent(ae.Value, t.TypeIndex, tt.TypeSize, rawData);
					}
					ecb.RemoveComponent(e, t.ToComponentType());
				}
			}
			else
			{
				ecb.AddComponent(e, arc);
			}
		}
	}

//=================================================================================================================//

	protected override void OnUpdate()
	{
		var ecb = new EntityCommandBuffer(CheckedStateRef.WorldUpdateAllocator);
		var q = SystemAPI.QueryBuilder()
			.WithAll<AnimatedRendererBakingComponent>()
			.WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
			.Build();
		
		var createAnimatedRendererComponentsJob = new CreateAnimatedRendererComponentsJob()
		{
			ecb	= ecb,
			em = EntityManager,
			additionalEntitiesBufferLookup = SystemAPI.GetBufferLookup<AdditionalEntitiesBakingData>(true),
			animatedSkinnedMeshComponentLookup = SystemAPI.GetComponentLookup<AnimatedSkinnedMeshComponent>(true)
		};
		
		createAnimatedRendererComponentsJob.Run(q);
		
		ecb.Playback(EntityManager);
	}
} 
}
