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
        public readonly int InitialSize;
        public readonly int MaximumSize;
        public int TotalCount;
        public bool IsWarming;

        public EffectBucket(
            GameObject prefab,
            int initialSize,
            int maximumSize)
        {
            Prefab = prefab;
            InitialSize = initialSize;
            MaximumSize = maximumSize;
            Available = new Queue<PooledEffect>(maximumSize);
        }
    }

    private sealed class PooledEffect
    {
        public readonly GameObject Instance;
        public readonly ParticleSystem[] ParticleSystems;
        public readonly EffectBucket Bucket;
        public readonly Vector3 BaseLocalScale;
        public float ReleaseTime;
        public int ActiveIndex = -1;

        public PooledEffect(
            GameObject instance,
            ParticleSystem[] particleSystems,
            EffectBucket bucket,
            Vector3 baseLocalScale)
        {
            Instance = instance;
            ParticleSystems = particleSystems;
            Bucket = bucket;
            BaseLocalScale = baseLocalScale;
        }
    }

    private readonly Dictionary<GameObject, EffectBucket> buckets = new Dictionary<GameObject, EffectBucket>();
    private readonly List<PooledEffect> activeEffects = new List<PooledEffect>(128);

    private Transform inactiveCreationRoot;
    private float mayhemIntensity;

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
        Register(effectPrefab, initialPoolSize, maxPoolSizePerPrefab);
    }

    public void Register(
        GameObject effectPrefab,
        int initialSize,
        int maximumSize)
    {
        if (effectPrefab == null)
        {
            return;
        }

        if (!buckets.TryGetValue(effectPrefab, out EffectBucket bucket))
        {
            int safeInitialSize = Mathf.Max(1, initialSize);
            int safeMaximumSize = Mathf.Max(safeInitialSize, maximumSize);
            bucket = new EffectBucket(
                effectPrefab,
                safeInitialSize,
                safeMaximumSize);
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
        effectTransform.localScale = effect.BaseLocalScale
            * Mathf.Lerp(1f, 1.22f, mayhemIntensity);
        effect.Instance.SetActive(true);

        RestartParticles(effect);
        effect.ReleaseTime = Time.time + Mathf.Max(0.01f, lifetime);
        effect.ActiveIndex = activeEffects.Count;
        activeEffects.Add(effect);
        return true;
    }

    public void SetMayhemIntensity(float normalizedIntensity)
    {
        mayhemIntensity = Mathf.Clamp01(normalizedIntensity);
    }

    private void StartWarmupIfNeeded(EffectBucket bucket)
    {
        if (!isActiveAndEnabled
            || bucket.IsWarming
            || bucket.TotalCount >= bucket.MaximumSize)
        {
            return;
        }

        bucket.IsWarming = true;
        StartCoroutine(WarmBucket(bucket));
    }

    private IEnumerator WarmBucket(EffectBucket bucket)
    {
        int firstTarget = Mathf.Min(
            bucket.InitialSize,
            bucket.MaximumSize);
        yield return WarmBucketToSize(bucket, firstTarget);
        yield return WarmBucketToSize(bucket, bucket.MaximumSize);
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

        if (bucket.TotalCount < bucket.MaximumSize)
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
        PooledEffect effect = new PooledEffect(
            instance,
            particleSystems,
            bucket,
            instance.transform.localScale);
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
        effect.Instance.transform.localScale = effect.BaseLocalScale;
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
