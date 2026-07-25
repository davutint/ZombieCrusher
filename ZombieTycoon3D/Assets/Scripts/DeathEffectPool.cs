using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DeathEffectPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField, Min(1)] private int initialPoolSize = 32;
    [SerializeField, Min(1)] private int maxPoolSizePerPrefab = 128;
    [SerializeField, Min(1)] private int warmupPerFrame = 4;

    private sealed class EffectBucket
    {
        public readonly GameObject Prefab;
        public readonly Queue<PooledEffect> Available;
        public int TotalCount;
        public bool IsWarming;

        public EffectBucket(GameObject prefab, int capacity)
        {
            Prefab = prefab;
            Available = new Queue<PooledEffect>(capacity);
        }
    }

    private sealed class PooledEffect
    {
        public readonly GameObject Instance;
        public readonly ParticleSystem[] ParticleSystems;
        public readonly EffectBucket Bucket;
        public float ReleaseTime;
        public int ActiveIndex = -1;

        public PooledEffect(GameObject instance, ParticleSystem[] particleSystems, EffectBucket bucket)
        {
            Instance = instance;
            ParticleSystems = particleSystems;
            Bucket = bucket;
        }
    }

    private readonly Dictionary<GameObject, EffectBucket> buckets = new Dictionary<GameObject, EffectBucket>();
    private readonly List<PooledEffect> activeEffects = new List<PooledEffect>(128);

    private Transform inactiveCreationRoot;

    public int RegisteredPrefabCount => buckets.Count;
    public int ActiveEffectCount => activeEffects.Count;

    public int TotalEffectCount
    {
        get
        {
            int total = 0;
            foreach (EffectBucket bucket in buckets.Values)
            {
                total += bucket.TotalCount;
            }

            return total;
        }
    }

    public int AvailableEffectCount
    {
        get
        {
            int total = 0;
            foreach (EffectBucket bucket in buckets.Values)
            {
                total += bucket.Available.Count;
            }

            return total;
        }
    }

    private void Awake()
    {
        GameObject creationRoot = new GameObject("Death Effect Pool Creation Root");
        creationRoot.transform.SetParent(transform, false);
        creationRoot.SetActive(false);
        inactiveCreationRoot = creationRoot.transform;
    }

    private void OnValidate()
    {
        initialPoolSize = Mathf.Max(1, initialPoolSize);
        maxPoolSizePerPrefab = Mathf.Max(initialPoolSize, maxPoolSizePerPrefab);
        warmupPerFrame = Mathf.Max(1, warmupPerFrame);
    }

    private void Update()
    {
        float currentTime = Time.time;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            PooledEffect effect = activeEffects[i];
            if (currentTime >= effect.ReleaseTime)
            {
                Release(effect, true);
            }
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        foreach (EffectBucket bucket in buckets.Values)
        {
            bucket.IsWarming = false;
        }

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            Release(activeEffects[i], true);
        }
    }

    private void OnEnable()
    {
        foreach (EffectBucket bucket in buckets.Values)
        {
            StartWarmupIfNeeded(bucket);
        }
    }

    public void Register(GameObject effectPrefab)
    {
        if (effectPrefab == null)
        {
            return;
        }

        if (!buckets.TryGetValue(effectPrefab, out EffectBucket bucket))
        {
            bucket = new EffectBucket(effectPrefab, maxPoolSizePerPrefab);
            buckets.Add(effectPrefab, bucket);
        }

        StartWarmupIfNeeded(bucket);
    }

    public bool Play(GameObject effectPrefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (effectPrefab == null)
        {
            return false;
        }

        Register(effectPrefab);
        EffectBucket bucket = buckets[effectPrefab];
        PooledEffect effect = Acquire(bucket);
        if (effect == null)
        {
            return false;
        }

        Transform effectTransform = effect.Instance.transform;
        effectTransform.SetPositionAndRotation(position, rotation);
        effect.Instance.SetActive(true);

        RestartParticles(effect);
        effect.ReleaseTime = Time.time + Mathf.Max(0.01f, lifetime);
        effect.ActiveIndex = activeEffects.Count;
        activeEffects.Add(effect);
        return true;
    }

    private void StartWarmupIfNeeded(EffectBucket bucket)
    {
        if (!isActiveAndEnabled || bucket.IsWarming || bucket.TotalCount >= maxPoolSizePerPrefab)
        {
            return;
        }

        bucket.IsWarming = true;
        StartCoroutine(WarmBucket(bucket));
    }

    private IEnumerator WarmBucket(EffectBucket bucket)
    {
        int firstTarget = Mathf.Min(initialPoolSize, maxPoolSizePerPrefab);
        yield return WarmBucketToSize(bucket, firstTarget);
        yield return WarmBucketToSize(bucket, maxPoolSizePerPrefab);
        bucket.IsWarming = false;
    }

    private IEnumerator WarmBucketToSize(EffectBucket bucket, int targetSize)
    {
        int batchSize = Mathf.Max(1, warmupPerFrame);
        while (bucket.TotalCount < targetSize)
        {
            int createCount = Mathf.Min(batchSize, targetSize - bucket.TotalCount);
            for (int i = 0; i < createCount; i++)
            {
                bucket.Available.Enqueue(CreateEffect(bucket));
            }

            yield return null;
        }
    }

    private PooledEffect Acquire(EffectBucket bucket)
    {
        if (bucket.Available.Count > 0)
        {
            return bucket.Available.Dequeue();
        }

        if (bucket.TotalCount < maxPoolSizePerPrefab)
        {
            return CreateEffect(bucket);
        }

        PooledEffect oldestEffect = FindOldestActiveEffect(bucket);
        if (oldestEffect != null)
        {
            Release(oldestEffect, false);
        }

        return oldestEffect;
    }

    private PooledEffect CreateEffect(EffectBucket bucket)
    {
        GameObject instance = Instantiate(bucket.Prefab, inactiveCreationRoot);
        instance.SetActive(false);
        instance.transform.SetParent(transform, false);

        ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        PooledEffect effect = new PooledEffect(instance, particleSystems, bucket);
        bucket.TotalCount++;
        return effect;
    }

    private PooledEffect FindOldestActiveEffect(EffectBucket bucket)
    {
        PooledEffect oldestEffect = null;
        float earliestReleaseTime = float.PositiveInfinity;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            PooledEffect candidate = activeEffects[i];
            if (candidate.Bucket != bucket || candidate.ReleaseTime >= earliestReleaseTime)
            {
                continue;
            }

            oldestEffect = candidate;
            earliestReleaseTime = candidate.ReleaseTime;
        }

        return oldestEffect;
    }

    private static void RestartParticles(PooledEffect effect)
    {
        for (int i = 0; i < effect.ParticleSystems.Length; i++)
        {
            ParticleSystem particleSystem = effect.ParticleSystems[i];
            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(false);
            particleSystem.Play(false);
        }
    }

    private void Release(PooledEffect effect, bool enqueue)
    {
        RemoveFromActiveEffects(effect);

        for (int i = 0; i < effect.ParticleSystems.Length; i++)
        {
            ParticleSystem particleSystem = effect.ParticleSystems[i];
            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(false);
        }

        effect.Instance.SetActive(false);
        if (enqueue)
        {
            effect.Bucket.Available.Enqueue(effect);
        }
    }

    private void RemoveFromActiveEffects(PooledEffect effect)
    {
        int index = effect.ActiveIndex;
        int lastIndex = activeEffects.Count - 1;
        if (index < 0 || index > lastIndex)
        {
            return;
        }

        PooledEffect lastEffect = activeEffects[lastIndex];
        activeEffects[index] = lastEffect;
        lastEffect.ActiveIndex = index;
        activeEffects.RemoveAt(lastIndex);
        effect.ActiveIndex = -1;
    }
}
