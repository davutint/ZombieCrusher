using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Orijinal rotasyonları ilk frame'de saklamak için sistem
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(RunningAnimationSystem))]
public partial struct InitializeLimbRotationsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        // Koşma animasyonu olan her karakter için
        foreach (var (limbRefs, entity) in SystemAPI.Query<RefRO<LimbReferences>>()
            .WithAll<RunningAnimation>()
            .WithEntityAccess())
        {
            // Her uzuv için orijinal rotasyonu kontrol et ve sakla
            CheckAndSaveOriginalRotation(ref state, limbRefs.ValueRO.leftArm, ecb);
            CheckAndSaveOriginalRotation(ref state, limbRefs.ValueRO.rightArm, ecb);
            CheckAndSaveOriginalRotation(ref state, limbRefs.ValueRO.leftLeg, ecb);
            CheckAndSaveOriginalRotation(ref state, limbRefs.ValueRO.rightLeg, ecb);
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    private void CheckAndSaveOriginalRotation(ref SystemState state, Entity limbEntity, EntityCommandBuffer ecb)
    {
        if (limbEntity == Entity.Null) return;
        
        // Eğer henüz orijinal rotasyon saklanmamışsa
        if (!SystemAPI.HasComponent<OriginalRotation>(limbEntity) && 
            SystemAPI.HasComponent<LocalTransform>(limbEntity))
        {
            var transform = SystemAPI.GetComponent<LocalTransform>(limbEntity);
            ecb.AddComponent(limbEntity, new OriginalRotation { value = transform.Rotation });
        }
    }
}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct RunningAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        // Koşma animasyonunu uygula
        foreach (var (runAnim, limbRefs) in SystemAPI.Query<RefRW<RunningAnimation>, RefRO<LimbReferences>>())
        {
            if (!runAnim.ValueRO.isRunning) continue;
            
            // Animasyon zamanını güncelle
            runAnim.ValueRW.currentTime += deltaTime * runAnim.ValueRO.animationSpeed;
            float time = runAnim.ValueRO.currentTime;
            float swingAmount = runAnim.ValueRO.limbSwingAmount;
            
            // Sol bacak ve sağ kol aynı fazda
            float leftLegAngle = math.sin(time) * swingAmount;
            float rightArmAngle = math.sin(time) * swingAmount * 0.7f; // Kollar biraz daha az sallanır
            
            // Sağ bacak ve sol kol zıt fazda
            float rightLegAngle = -math.sin(time) * swingAmount;
            float leftArmAngle = -math.sin(time) * swingAmount * 0.7f;
            
            // Uzuvlara rotasyon uygula
            ApplyLimbRotation(ref state, limbRefs.ValueRO.leftLeg, leftLegAngle, true);
            ApplyLimbRotation(ref state, limbRefs.ValueRO.rightLeg, rightLegAngle, true);
            ApplyLimbRotation(ref state, limbRefs.ValueRO.leftArm, leftArmAngle, false);
            ApplyLimbRotation(ref state, limbRefs.ValueRO.rightArm, rightArmAngle, false);
        }
    }
    
    [BurstCompile]
    private void ApplyLimbRotation(ref SystemState state, Entity limbEntity, float angle, bool isLeg)
    {
        if (limbEntity == Entity.Null) return;
        
        // LocalTransform ve OriginalRotation component'lerini kontrol et
        if (!SystemAPI.HasComponent<LocalTransform>(limbEntity)) return;
        if (!SystemAPI.HasComponent<OriginalRotation>(limbEntity)) return;
        
        RefRW<LocalTransform> transform = SystemAPI.GetComponentRW<LocalTransform>(limbEntity);
        var originalRot = SystemAPI.GetComponent<OriginalRotation>(limbEntity).value;
        
        // X ekseni etrafında döndür (ileri-geri salınım)
        quaternion swingRotation = quaternion.RotateX(angle);
        
        // Bacaklar için hafif bir Y rotasyonu da ekle (daha doğal görünüm için)
        if (isLeg)
        {
            quaternion sideRotation = quaternion.RotateY(angle * 0.1f);
            swingRotation = math.mul(swingRotation, sideRotation);
        }
        
        // Orijinal rotasyon ile birleştir
        transform.ValueRW.Rotation = math.mul(originalRot, swingRotation);
    }
}
