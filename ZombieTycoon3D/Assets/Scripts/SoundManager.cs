using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SoundManager : MonoBehaviour
{
    [Header("Death Sound")]
    [SerializeField] private GameObject soundEffect;
    [SerializeField, Min(0.01f)] private float minimumPitch = 0.7f;
    [SerializeField, Min(0.01f)] private float maximumPitch = 1f;

    [Header("Pool Settings")]
    [SerializeField, Min(1)] private int initialPoolSize = 16;
    [SerializeField, Min(1)] private int maxPoolSize = 32;
    [SerializeField, Min(1)] private int warmupPerFrame = 4;

    private sealed class PooledAudioSource
    {
        public readonly GameObject Instance;
        public readonly AudioSource Source;
        public float ReleaseTime;
        public int ActiveIndex = -1;

        public PooledAudioSource(GameObject instance, AudioSource source)
        {
            Instance = instance;
            Source = source;
        }
    }

    private readonly Queue<PooledAudioSource> availableSources = new Queue<PooledAudioSource>(32);
    private readonly List<PooledAudioSource> activeSources = new List<PooledAudioSource>(32);

    private Transform inactiveCreationRoot;
    private int totalSourceCount;
    private bool isConfigured;
    private bool isWarming;
    private float baseVolume = 1f;
    private ScoreManager scoreManager;

    public int ActiveSourceCount => activeSources.Count;
    public int AvailableSourceCount => availableSources.Count;
    public int TotalSourceCount => totalSourceCount;

    private void Awake()
    {
        EnsureCreationRoot();
        isConfigured = ValidateSoundEffectPrefab();
        scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    private void OnValidate()
    {
        minimumPitch = Mathf.Max(0.01f, minimumPitch);
        maximumPitch = Mathf.Max(minimumPitch, maximumPitch);
        initialPoolSize = Mathf.Max(1, initialPoolSize);
        maxPoolSize = Mathf.Max(initialPoolSize, maxPoolSize);
        warmupPerFrame = Mathf.Max(1, warmupPerFrame);
    }

    private void OnEnable()
    {
        EventManager.OnZombieDead += PlayDeathSound;

        if (!Application.isPlaying)
        {
            return;
        }

        EnsureCreationRoot();
        isConfigured = ValidateSoundEffectPrefab();
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }
        StartWarmupIfNeeded();
    }

    private void OnDisable()
    {
        EventManager.OnZombieDead -= PlayDeathSound;
        StopAllCoroutines();
        isWarming = false;

        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            Release(activeSources[i], true);
        }
    }

    private void Update()
    {
        float currentTime = Time.time;
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            PooledAudioSource pooledSource = activeSources[i];
            if (currentTime >= pooledSource.ReleaseTime)
            {
                Release(pooledSource, true);
            }
        }
    }

    private bool ValidateSoundEffectPrefab()
    {
        if (soundEffect == null)
        {
            Debug.LogError("SoundManager: A sound effect prefab is required.", this);
            return false;
        }

        AudioSource prefabSource = soundEffect.GetComponent<AudioSource>();
        if (prefabSource == null || prefabSource.clip == null)
        {
            Debug.LogError("SoundManager: The sound effect prefab requires an AudioSource with a clip.", soundEffect);
            return false;
        }

        baseVolume = prefabSource.volume;
        return true;
    }

    private void EnsureCreationRoot()
    {
        if (inactiveCreationRoot != null)
        {
            return;
        }

        GameObject creationRoot = new GameObject("Sound Pool Creation Root");
        creationRoot.transform.SetParent(transform, false);
        creationRoot.SetActive(false);
        inactiveCreationRoot = creationRoot.transform;
    }

    private void StartWarmupIfNeeded()
    {
        if (!isActiveAndEnabled || !isConfigured || isWarming || totalSourceCount >= maxPoolSize)
        {
            return;
        }

        isWarming = true;
        StartCoroutine(WarmPool());
    }

    private IEnumerator WarmPool()
    {
        int firstTarget = Mathf.Min(initialPoolSize, maxPoolSize);
        yield return WarmPoolToSize(firstTarget);
        yield return WarmPoolToSize(maxPoolSize);
        isWarming = false;
    }

    private IEnumerator WarmPoolToSize(int targetSize)
    {
        int batchSize = Mathf.Max(1, warmupPerFrame);
        while (totalSourceCount < targetSize)
        {
            int createCount = Mathf.Min(batchSize, targetSize - totalSourceCount);
            for (int i = 0; i < createCount; i++)
            {
                availableSources.Enqueue(CreatePooledSource());
            }

            yield return null;
        }
    }

    private PooledAudioSource CreatePooledSource()
    {
        GameObject instance = Instantiate(soundEffect, inactiveCreationRoot);
        instance.SetActive(false);
        instance.transform.SetParent(transform, false);

        AudioSource source = instance.GetComponent<AudioSource>();
        PooledAudioSource pooledSource = new PooledAudioSource(instance, source);
        totalSourceCount++;
        return pooledSource;
    }

    private void PlayDeathSound(Vector3 position)
    {
        if (!isConfigured)
        {
            return;
        }

        PooledAudioSource pooledSource = Acquire();
        if (pooledSource == null)
        {
            return;
        }

        Transform sourceTransform = pooledSource.Instance.transform;
        sourceTransform.SetPositionAndRotation(position, Quaternion.identity);
        pooledSource.Instance.SetActive(true);

        AudioSource source = pooledSource.Source;
        source.Stop();
        float mayhemIntensity = scoreManager != null
            ? scoreManager.CurrentMayhem.Meter01
            : 0f;
        source.pitch = Random.Range(minimumPitch, maximumPitch)
            * Mathf.Lerp(1f, 1.08f, mayhemIntensity);
        source.volume = Mathf.Clamp01(
            baseVolume * Mathf.Lerp(1f, 1.18f, mayhemIntensity));
        source.time = 0f;
        source.Play();

        float clipDuration = source.clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch));
        pooledSource.ReleaseTime = Time.time + clipDuration + 0.05f;
        pooledSource.ActiveIndex = activeSources.Count;
        activeSources.Add(pooledSource);
    }

    private PooledAudioSource Acquire()
    {
        if (availableSources.Count > 0)
        {
            return availableSources.Dequeue();
        }

        if (totalSourceCount < maxPoolSize)
        {
            return CreatePooledSource();
        }

        PooledAudioSource oldestSource = FindOldestActiveSource();
        if (oldestSource != null)
        {
            Release(oldestSource, false);
        }

        return oldestSource;
    }

    private PooledAudioSource FindOldestActiveSource()
    {
        PooledAudioSource oldestSource = null;
        float earliestReleaseTime = float.PositiveInfinity;

        for (int i = 0; i < activeSources.Count; i++)
        {
            PooledAudioSource candidate = activeSources[i];
            if (candidate.ReleaseTime >= earliestReleaseTime)
            {
                continue;
            }

            oldestSource = candidate;
            earliestReleaseTime = candidate.ReleaseTime;
        }

        return oldestSource;
    }

    private void Release(PooledAudioSource pooledSource, bool enqueue)
    {
        RemoveFromActiveSources(pooledSource);
        pooledSource.Source.Stop();
        pooledSource.Instance.SetActive(false);

        if (enqueue)
        {
            availableSources.Enqueue(pooledSource);
        }
    }

    private void RemoveFromActiveSources(PooledAudioSource pooledSource)
    {
        int index = pooledSource.ActiveIndex;
        int lastIndex = activeSources.Count - 1;
        if (index < 0 || index > lastIndex)
        {
            return;
        }

        PooledAudioSource lastSource = activeSources[lastIndex];
        activeSources[index] = lastSource;
        lastSource.ActiveIndex = index;
        activeSources.RemoveAt(lastIndex);
        pooledSource.ActiveIndex = -1;
    }
}
