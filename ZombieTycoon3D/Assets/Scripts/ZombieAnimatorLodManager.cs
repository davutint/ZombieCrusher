using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ZombieAnimatorLodManager : MonoBehaviour
{
    [Header("Animation Budget")]
    [SerializeField, Min(1)] private int maxAnimatedZombies = 128;
    [SerializeField, Min(0.05f)] private float evaluationInterval = 0.25f;
    [SerializeField] private Camera targetCamera;

    private readonly List<Entry> entries = new List<Entry>(512);
    private readonly HashSet<Enemy> registeredEnemies = new HashSet<Enemy>();
    private readonly List<Candidate> candidates = new List<Candidate>(512);
    private readonly Plane[] frustumPlanes = new Plane[6];

    private float nextEvaluationTime;
    private bool evaluationRequested;

    public int RegisteredCount => entries.Count;
    public int AnimatedCount { get; private set; }

    private void Awake()
    {
        ResolveCamera();
    }

    private void OnEnable()
    {
        RequestEvaluation();
    }

    private void Update()
    {
        if (!evaluationRequested && Time.unscaledTime < nextEvaluationTime)
        {
            return;
        }

        EvaluateBudget();
        evaluationRequested = false;
        nextEvaluationTime = Time.unscaledTime + Mathf.Max(0.05f, evaluationInterval);
    }

    private void OnDisable()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Enemy enemy = entries[i].Enemy;
            if (enemy != null)
            {
                enemy.SetAnimationLodEnabled(true);
            }
        }

        AnimatedCount = 0;
    }

    internal void Register(Enemy enemy)
    {
        if (enemy == null || registeredEnemies.Contains(enemy))
        {
            return;
        }

        if (!enemy.TryPrepareAnimationLod(out Renderer animationRenderer))
        {
            return;
        }

        registeredEnemies.Add(enemy);
        entries.Add(new Entry(enemy, animationRenderer));
        RequestEvaluation();
    }

    internal void Unregister(Enemy enemy)
    {
        if (enemy == null || !registeredEnemies.Remove(enemy))
        {
            return;
        }

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].Enemy != enemy)
            {
                continue;
            }

            int lastIndex = entries.Count - 1;
            entries[i] = entries[lastIndex];
            entries.RemoveAt(lastIndex);
            break;
        }

        enemy.SetAnimationLodEnabled(false);
        RequestEvaluation();
    }

    private void EvaluateBudget()
    {
        ResolveCamera();
        RemoveDestroyedEntries();
        candidates.Clear();

        if (targetCamera == null)
        {
            EnableAllEligible();
            return;
        }

        GeometryUtility.CalculateFrustumPlanes(targetCamera, frustumPlanes);
        Vector3 cameraPosition = targetCamera.transform.position;
        int animationBudget = Mathf.Max(1, maxAnimatedZombies);
        int retainedAnimatorCount = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            Enemy enemy = entry.Enemy;
            Renderer animationRenderer = entry.AnimationRenderer;

            if (enemy == null ||
                animationRenderer == null ||
                !enemy.CanAnimateByLod ||
                !animationRenderer.enabled ||
                !animationRenderer.gameObject.activeInHierarchy ||
                !GeometryUtility.TestPlanesAABB(frustumPlanes, animationRenderer.bounds))
            {
                if (enemy != null)
                {
                    enemy.SetAnimationLodEnabled(false);
                }

                continue;
            }

            if (enemy.IsAnimationLodEnabled && retainedAnimatorCount < animationBudget)
            {
                retainedAnimatorCount++;
                continue;
            }

            if (enemy.IsAnimationLodEnabled)
            {
                enemy.SetAnimationLodEnabled(false);
            }

            float distanceSqr = (enemy.transform.position - cameraPosition).sqrMagnitude;
            candidates.Add(new Candidate(enemy, distanceSqr));
        }

        int openSlots = Mathf.Min(animationBudget - retainedAnimatorCount, candidates.Count);
        if (openSlots > 0)
        {
            candidates.Sort(CandidateComparer.Instance);

            for (int i = 0; i < openSlots; i++)
            {
                candidates[i].Enemy.SetAnimationLodEnabled(true);
            }
        }

        AnimatedCount = retainedAnimatorCount + openSlots;
    }

    private void EnableAllEligible()
    {
        AnimatedCount = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            Enemy enemy = entries[i].Enemy;
            if (enemy == null || !enemy.CanAnimateByLod)
            {
                continue;
            }

            enemy.SetAnimationLodEnabled(true);
            AnimatedCount++;
        }
    }

    private void RemoveDestroyedEntries()
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Enemy enemy = entries[i].Enemy;
            if (enemy != null)
            {
                continue;
            }

            registeredEnemies.Remove(enemy);
            int lastIndex = entries.Count - 1;
            entries[i] = entries[lastIndex];
            entries.RemoveAt(lastIndex);
        }
    }

    private void ResolveCamera()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            targetCamera = Camera.main;
        }
    }

    private void RequestEvaluation()
    {
        evaluationRequested = true;
        nextEvaluationTime = 0f;
    }

    private readonly struct Entry
    {
        public Entry(Enemy enemy, Renderer animationRenderer)
        {
            Enemy = enemy;
            AnimationRenderer = animationRenderer;
        }

        public Enemy Enemy { get; }
        public Renderer AnimationRenderer { get; }
    }

    private readonly struct Candidate
    {
        public Candidate(Enemy enemy, float distanceSqr)
        {
            Enemy = enemy;
            DistanceSqr = distanceSqr;
        }

        public Enemy Enemy { get; }
        public float DistanceSqr { get; }
    }

    private sealed class CandidateComparer : IComparer<Candidate>
    {
        public static readonly CandidateComparer Instance = new CandidateComparer();

        public int Compare(Candidate x, Candidate y)
        {
            return x.DistanceSqr.CompareTo(y.DistanceSqr);
        }
    }
}
