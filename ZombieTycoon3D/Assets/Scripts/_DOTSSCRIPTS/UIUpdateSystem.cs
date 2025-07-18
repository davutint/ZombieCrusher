using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Collections;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class UIUpdateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Bu system sadece editor'de veya development build'de çalışsın
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            
        Entities
            .WithAll<UIUpdateRequest, ZombieSpawnSettings>()
            .WithoutBurst() // UI güncellemesi main thread'de olmalı
            .ForEach((Entity entity, ref UIUpdateRequest uiRequest, in ZombieSpawnSettings settings) =>
            {
                // UI Manager varsa güncelle
                var uiManager = GameObject.FindObjectOfType<ZombieUIManager>();
                if (uiManager != null)
                {
                    // Not: ForEach içinde Unity API kullanımı Burst'ü devre dışı bırakır
                    // Bu yüzden WithoutBurst() kullandık
                }
            }).Run();
            
#endif
    }
}
