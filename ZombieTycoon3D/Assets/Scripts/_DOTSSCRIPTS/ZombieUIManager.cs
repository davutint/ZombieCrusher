using TMPro;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public class ZombieUIManager : MonoBehaviour
{
    [Header("UI Referansları")]
    public TextMeshProUGUI totalZombiesText;   // Toplam zombie sayısı
    public TextMeshProUGUI killedZombiesText;  // Öldürülen zombie sayısı
    public TextMeshProUGUI maxZombiesText;     // Maksimum zombie sayısı
    public TextMeshProUGUI fpsText;            // FPS değeri

    private EntityManager entityManager;
    private EntityQuery uiUpdateQuery;
    private EntityQuery fpsQuery;

    void Start()
    {
        // EntityManager'ı al
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Zombie UI verilerini çeken query
        uiUpdateQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UIUpdateRequest>(),
            ComponentType.ReadOnly<ZombieSpawnSettings>());

        // FPS verisini çeken query
        fpsQuery = entityManager.CreateEntityQuery(typeof(FPSComponent));

        // Başlangıç değerleri
        UpdateUI(0, 0, 0, 0);
    }

    void Update()
    {
        int total = 0;
        int killed = 0;
        int max = 0;
        int fps = 0;

        // Zombie sayıları
        if (!uiUpdateQuery.IsEmpty)
        {
            var entities = uiUpdateQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length > 0)
            {
                var entity = entities[0];
                var uiRequest = entityManager.GetComponentData<UIUpdateRequest>(entity);
                var settings = entityManager.GetComponentData<ZombieSpawnSettings>(entity);

                total = uiRequest.totalZombies;
                killed = uiRequest.killedZombies;
                max = settings.maxZombieCount;
            }
            entities.Dispose();
        }

        // FPS verisi
        if (!fpsQuery.IsEmpty)
        {
            var entity = fpsQuery.GetSingletonEntity();
            var data = entityManager.GetComponentData<FPSComponent>(entity);
            fps = Mathf.RoundToInt(data.currentFPS);
        }

        // UI'yı güncelle
        UpdateUI(total, killed, max, fps);
    }

    void UpdateUI(int total, int killed, int max, int fps)
    {
        if (totalZombiesText != null)
            totalZombiesText.text = $"Zombies: {total}";

        if (killedZombiesText != null)
            killedZombiesText.text = $"Killed: {killed}";

        if (maxZombiesText != null)
            maxZombiesText.text = $"Max: {max}";

        if (fpsText != null)
            fpsText.text = $"FPS: {fps}";
    }

    // Sistemlerden çağrılabilir
    public static void NotifyZombieKilled(int newKillCount)
    {
        var instance = FindObjectOfType<ZombieUIManager>();
        if (instance != null && instance.killedZombiesText != null)
        {
            instance.killedZombiesText.text = $"Killed: {newKillCount}";
        }
    }
}
public struct FPSComponent : IComponentData
{
    public float currentFPS;
}

