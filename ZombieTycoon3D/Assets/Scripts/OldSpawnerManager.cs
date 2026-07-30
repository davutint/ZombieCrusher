using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class OldSpawnManager : MonoBehaviour
{
    [Header("Zombie Prefabs")]
    public List<GameObject> zombiePrefabs;

    [Header("Spawn Settings")]
    public int maxZombiesInScene;
    public Transform player;
    public float spawnDistanceMin = 20f;
    public float spawnDistanceMax = 50f;
    [SerializeField] private bool spawningEnabled;

    [Header("Mission Pressure")]
    [SerializeField, Range(0.05f, 0.9f)]
    private float buildUpProgress = 0.375f;
    [SerializeField, Range(0.1f, 0.95f)]
    private float surgeProgress = 0.75f;
    [SerializeField, Min(1)] private int openingActiveTarget = 60;
    [SerializeField, Min(1)] private int buildUpActiveTarget = 160;
    [SerializeField, Min(1)] private int surgeActiveTarget = 300;
    [SerializeField, Min(1)] private int finaleActiveTarget = 450;
    [SerializeField, Min(1)] private int openingHordeSize = 8;
    [SerializeField, Min(1)] private int buildUpHordeSize = 12;
    [SerializeField, Min(1)] private int surgeHordeSize = 17;
    [SerializeField, Min(1)] private int finaleHordeSize = 22;
    [SerializeField, Min(0.05f)] private float openingSpawnInterval = 1.25f;
    [SerializeField, Min(0.05f)] private float buildUpSpawnInterval = 0.9f;
    [SerializeField, Min(0.05f)] private float surgeSpawnInterval = 0.62f;
    [SerializeField, Min(0.05f)] private float finaleSpawnInterval = 0.45f;

    [Header("Pool Settings")]
    [SerializeField, Min(1)] private int initialPoolSize = 50;
    [SerializeField, Min(1)] private int poolWarmupPerFrame = 5;

    [Header("Death VFX Pool")]
    [SerializeField] private DeathEffectPool deathEffectPool;

    [Header("Animation LOD")]
    [SerializeField] private ZombieAnimatorLodManager animatorLodManager;

    [Header("AI Tick Budget")]
    [SerializeField] private ZombieAiTickManager aiTickManager;

    public Transform zombieParent;

    [SerializeField] private int currentZombieCount;

    private readonly Queue<Enemy> availableZombies = new Queue<Enemy>();
    private readonly HashSet<Enemy> activeZombies = new HashSet<Enemy>();
    private readonly List<GameObject> validZombiePrefabs = new List<GameObject>();

    private int totalCreatedZombieCount;
    private int nextPrefabIndex;
    private Vector3 poolCreationPosition;
    private float missionProgress;
    private int currentActiveTarget;
    private int currentHordeSize;
    private float currentSpawnInterval;
    private float nextSpawnTime;

    public int CurrentZombieCount => currentZombieCount;
    public int AvailableZombieCount => availableZombies.Count;
    public int TotalCreatedZombieCount => totalCreatedZombieCount;
    public bool SpawningEnabled => spawningEnabled;
    public float MissionProgress => missionProgress;
    public int CurrentActiveTarget => currentActiveTarget;
    public int CurrentHordeSize => currentHordeSize;
    public float CurrentSpawnInterval => currentSpawnInterval;

    private IEnumerator Start()
    {
        if (!TryInitializePool())
        {
            yield break;
        }

        int firstSpawnPoolSize = Mathf.Clamp(initialPoolSize, 1, maxZombiesInScene);
        yield return WarmPoolToSize(firstSpawnPoolSize);

        StartCoroutine(SpawnZombiesRoutine());
        yield return WarmPoolToSize(maxZombiesInScene);
    }

    private bool TryInitializePool()
    {
        if (maxZombiesInScene <= 0)
        {
            Debug.LogError("OldSpawnManager: Max zombie count must be greater than zero.", this);
            return false;
        }

        if (player == null || zombieParent == null)
        {
            Debug.LogError("OldSpawnManager: Player and zombie parent references are required.", this);
            return false;
        }

        validZombiePrefabs.Clear();
        if (deathEffectPool == null)
        {
            deathEffectPool = GetComponent<DeathEffectPool>();
        }

        if (animatorLodManager == null)
        {
            animatorLodManager = GetComponent<ZombieAnimatorLodManager>();
        }

        if (aiTickManager == null)
        {
            aiTickManager = GetComponent<ZombieAiTickManager>();
        }

        if (zombiePrefabs != null)
        {
            for (int i = 0; i < zombiePrefabs.Count; i++)
            {
                GameObject prefab = zombiePrefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                Enemy prefabEnemy = prefab.GetComponent<Enemy>();
                if (prefabEnemy == null)
                {
                    Debug.LogError($"OldSpawnManager: '{prefab.name}' does not contain an Enemy component.", prefab);
                    continue;
                }

                validZombiePrefabs.Add(prefab);
                deathEffectPool?.Register(prefabEnemy.DeathEffectPrefab);
            }
        }

        if (validZombiePrefabs.Count == 0)
        {
            Debug.LogError("OldSpawnManager: At least one valid zombie prefab is required.", this);
            return false;
        }

        if (!TryGetRandomSpawnPosition(out poolCreationPosition))
        {
            Debug.LogError("OldSpawnManager: A valid NavMesh position could not be found for the zombie pool.", this);
            return false;
        }

        availableZombies.Clear();
        activeZombies.Clear();
        totalCreatedZombieCount = 0;
        currentZombieCount = 0;
        nextPrefabIndex = 0;
        SetMissionProgress(0f);
        nextSpawnTime = Time.unscaledTime;
        return true;
    }

    private IEnumerator WarmPoolToSize(int targetSize)
    {
        int clampedTargetSize = Mathf.Clamp(targetSize, 0, maxZombiesInScene);
        int batchSize = Mathf.Max(1, poolWarmupPerFrame);

        while (totalCreatedZombieCount < clampedTargetSize)
        {
            int createCount = Mathf.Min(batchSize, clampedTargetSize - totalCreatedZombieCount);
            for (int i = 0; i < createCount; i++)
            {
                CreatePooledZombie();
            }

            yield return null;
        }
    }

    private void CreatePooledZombie()
    {
        GameObject prefab = validZombiePrefabs[nextPrefabIndex];
        nextPrefabIndex = (nextPrefabIndex + 1) % validZombiePrefabs.Count;

        GameObject zombieObject = Instantiate(prefab, poolCreationPosition, Quaternion.identity, zombieParent);
        Enemy zombie = zombieObject.GetComponent<Enemy>();
        zombie.ConfigurePool(this, player, deathEffectPool, animatorLodManager, aiTickManager);

        availableZombies.Enqueue(zombie);
        totalCreatedZombieCount++;
    }

    private IEnumerator SpawnZombiesRoutine()
    {
        while (true)
        {
            if (spawningEnabled && Time.unscaledTime >= nextSpawnTime)
            {
                nextSpawnTime =
                    Time.unscaledTime + Mathf.Max(0.05f, currentSpawnInterval);
                TrySpawnPressureTick();
            }

            yield return null;
        }
    }

    private void TrySpawnPressureTick()
    {
        int activeTarget = Mathf.Clamp(
            currentActiveTarget,
            1,
            maxZombiesInScene);
        if (currentZombieCount >= activeTarget
            || availableZombies.Count == 0)
        {
            return;
        }

        int spawnCount = Mathf.Min(
            currentHordeSize,
            activeTarget - currentZombieCount);
        SpawnZombieHorde(spawnCount);
    }

    private void SpawnZombieHorde(int count)
    {
        if (!TryGetRandomSpawnPosition(out Vector3 hordeCenter))
        {
            return;
        }

        int spawnCount = Mathf.Min(count, availableZombies.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            Vector3 spawnPosition = hordeCenter + offset;

            Enemy zombie = availableZombies.Dequeue();
            if (zombie == null)
            {
                continue;
            }

            activeZombies.Add(zombie);
            zombie.SpawnFromPool(spawnPosition, Quaternion.identity, zombieParent, player);
        }

        currentZombieCount = activeZombies.Count;
    }

    private bool TryGetRandomSpawnPosition(out Vector3 spawnPosition)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 randomDirection = Random.insideUnitSphere.normalized;
            randomDirection.y = 0f;

            float randomDistance = Random.Range(spawnDistanceMin, spawnDistanceMax);
            Vector3 randomPoint = player.position + randomDirection * randomDistance;

            if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                continue;
            }

            float distanceToPlayer = Vector3.Distance(hit.position, player.position);
            if (distanceToPlayer < spawnDistanceMin || distanceToPlayer > spawnDistanceMax)
            {
                continue;
            }

            spawnPosition = hit.position;
            return true;
        }

        spawnPosition = default;
        return false;
    }

    internal bool ReturnZombieToPool(Enemy zombie)
    {
        if (zombie == null || !activeZombies.Remove(zombie))
        {
            return false;
        }

        zombie.StoreInPool();
        availableZombies.Enqueue(zombie);
        currentZombieCount = activeZombies.Count;
        return true;
    }

    public void SetSpawningEnabled(bool enabled)
    {
        if (enabled && !spawningEnabled)
        {
            nextSpawnTime = Time.unscaledTime;
        }

        spawningEnabled = enabled;
    }

    public void BeginMission()
    {
        SetMissionProgress(0f);
        SetSpawningEnabled(true);
    }

    public void SetMissionProgress(float normalizedProgress)
    {
        missionProgress = Mathf.Clamp01(normalizedProgress);

        float buildPoint = Mathf.Clamp(buildUpProgress, 0.05f, 0.9f);
        float surgePoint = Mathf.Clamp(
            surgeProgress,
            buildPoint + 0.05f,
            0.95f);

        if (missionProgress <= buildPoint)
        {
            ApplyPressure(
                Mathf.InverseLerp(0f, buildPoint, missionProgress),
                openingActiveTarget,
                buildUpActiveTarget,
                openingHordeSize,
                buildUpHordeSize,
                openingSpawnInterval,
                buildUpSpawnInterval);
            return;
        }

        if (missionProgress <= surgePoint)
        {
            ApplyPressure(
                Mathf.InverseLerp(
                    buildPoint,
                    surgePoint,
                    missionProgress),
                buildUpActiveTarget,
                surgeActiveTarget,
                buildUpHordeSize,
                surgeHordeSize,
                buildUpSpawnInterval,
                surgeSpawnInterval);
            return;
        }

        ApplyPressure(
            Mathf.InverseLerp(surgePoint, 1f, missionProgress),
            surgeActiveTarget,
            finaleActiveTarget,
            surgeHordeSize,
            finaleHordeSize,
            surgeSpawnInterval,
            finaleSpawnInterval);
    }

    private void ApplyPressure(
        float progress,
        int activeTargetFrom,
        int activeTargetTo,
        int hordeSizeFrom,
        int hordeSizeTo,
        float intervalFrom,
        float intervalTo)
    {
        float smoothedProgress =
            Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
        int poolLimit = Mathf.Max(1, maxZombiesInScene);
        currentActiveTarget = Mathf.Clamp(
            Mathf.RoundToInt(
                Mathf.Lerp(
                    activeTargetFrom,
                    activeTargetTo,
                    smoothedProgress)),
            1,
            poolLimit);
        currentHordeSize = Mathf.Max(
            1,
            Mathf.RoundToInt(
                Mathf.Lerp(
                    hordeSizeFrom,
                    hordeSizeTo,
                    smoothedProgress)));
        currentSpawnInterval = Mathf.Max(
            0.05f,
            Mathf.Lerp(
                intervalFrom,
                intervalTo,
                smoothedProgress));
    }

    public void DespawnAllToPool()
    {
        if (activeZombies.Count == 0)
        {
            return;
        }

        List<Enemy> snapshot = new List<Enemy>(activeZombies);
        for (int i = 0; i < snapshot.Count; i++)
        {
            ReturnZombieToPool(snapshot[i]);
        }
    }
}

