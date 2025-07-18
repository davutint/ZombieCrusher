using Unity.Entities;
using UnityEngine;

// Debug amaçlı - animasyon çalışmıyorsa bu sistem size bilgi verecek
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class AnimationDebugSystem : SystemBase
{
    private float lastLogTime = 0f;
    
    protected override void OnUpdate()
    {
        float currentTime = (float)SystemAPI.Time.ElapsedTime;
        
        // Her 2 saniyede bir log at
        if (currentTime - lastLogTime < 2f) return;
        lastLogTime = currentTime;
        
        int zombieCount = 0;
        int animatingCount = 0;
        
        Entities
            .ForEach((in RunningAnimation runAnim, in LimbReferences limbRefs) =>
            {
                zombieCount++;
                if (runAnim.isRunning)
                {
                    animatingCount++;
                    
                    // İlk zombie'nin detaylarını yazdır
                    if (zombieCount == 1)
                    {
                        Debug.Log($"[AnimDebug] Zombie animasyon durumu:" +
                                  $"\n- Zaman: {runAnim.currentTime:F2}" +
                                  $"\n- Hız: {runAnim.animationSpeed}" +
                                  $"\n- Salınım: {runAnim.limbSwingAmount}" +
                                  $"\n- Sol Kol: {(limbRefs.leftArm != Entity.Null ? "VAR" : "YOK")}" +
                                  $"\n- Sağ Kol: {(limbRefs.rightArm != Entity.Null ? "VAR" : "YOK")}" +
                                  $"\n- Sol Bacak: {(limbRefs.leftLeg != Entity.Null ? "VAR" : "YOK")}" +
                                  $"\n- Sağ Bacak: {(limbRefs.rightLeg != Entity.Null ? "VAR" : "YOK")}");
                    }
                }
            })
            .WithoutBurst()
            .Run();
            
        if (zombieCount > 0)
        {
            Debug.Log($"[AnimDebug] Toplam zombie: {zombieCount}, Animasyon çalışan: {animatingCount}");
        }
    }
}