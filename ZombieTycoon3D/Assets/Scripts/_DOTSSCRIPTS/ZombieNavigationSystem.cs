using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Zombie'nin hedefini tutan component
public struct ZombieTarget : IComponentData
{
    public Entity targetEntity;
    public float3 lastKnownPosition;
    public float3 destination;
}

// Basitleştirilmiş navigation system - sadece AgentBody kullan
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ZombieNavigationSystem : ISystem
{
    private float nextUpdateTime;
    private const float UPDATE_INTERVAL = 0.2f; // Saniyede 5 kez güncelle (her ~12 frame)

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ZombieSpawnSettings>();
        nextUpdateTime = 0f;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float currentTime = (float)SystemAPI.Time.ElapsedTime;

        // Throttling - Her frame güncelleme yapma
        if (currentTime < nextUpdateTime)
            return;

        nextUpdateTime = currentTime + UPDATE_INTERVAL;

        // Player vehicle'ı bul (Sadece bir tane olduğunu varsayıyoruz)
        Entity playerEntity = Entity.Null;
        float3 playerPosition = float3.zero;

        foreach (var (transform, entity) in
                 SystemAPI.Query<RefRO<LocalTransform>>()
                 .WithAll<VehicleTag>() // Player tag'i varsa daha iyi olur ama şimdilik VehicleTag
                 .WithEntityAccess())
        {
            // İlk bulduğumuz aracı hedef alalım
            playerEntity = entity;
            playerPosition = transform.ValueRO.Position;
            break;
        }

        if (playerEntity == Entity.Null)
            return;

        // Tüm zombilere hedefi ata
        foreach (var (target, agent, transform) in
                 SystemAPI.Query<RefRW<ZombieTarget>, RefRW<AgentBody>, RefRO<LocalTransform>>()
                 .WithAll<ZombieTag>())
        {
            // Eğer hedef çok değişmediyse güncelleme (isteğe bağlı optimizasyon)
            if (math.distancesq(target.ValueRO.lastKnownPosition, playerPosition) < 1.0f)
                continue;

            target.ValueRW.targetEntity = playerEntity;
            target.ValueRW.destination = playerPosition;
            target.ValueRW.lastKnownPosition = playerPosition;

            // Agent'a hedefi ver
            agent.ValueRW.SetDestination(playerPosition);

            // Eğer durmuşsa hareket ettir
            if (agent.ValueRO.IsStopped)
                agent.ValueRW.IsStopped = false;
        }
    }
}
