using System.Collections.Generic;
using RenownedGames.AITree;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class ZombieAiTickManager : MonoBehaviour
{
    [Header("Direct Steering")]
    [SerializeField, Min(0.1f)] private float speedMultiplier = 1f;
    [SerializeField, Min(0f)] private float stoppingDistance = 2f;
    [SerializeField, Min(1f)] private float turnSpeed = 540f;

    private readonly List<Entry> entries = new List<Entry>(512);
    private readonly HashSet<Enemy> registeredEnemies = new HashSet<Enemy>();

    private Transform navigationTarget;

    public int RegisteredCount => entries.Count;
    public int TickedLastFrame { get; private set; }

    private void Awake()
    {
        OldSpawnManager spawnManager = GetComponent<OldSpawnManager>();
        if (spawnManager == null || spawnManager.player == null)
        {
            Debug.LogError(
                "ZombieAiTickManager: OldSpawnManager with a player target is required.",
                this);
            enabled = false;
            return;
        }

        navigationTarget = spawnManager.player;
    }

    private void OnEnable()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            entry.Managed = false;
            BeginManaging(ref entry);
            entries[i] = entry;
        }
    }

    private void Update()
    {
        TickedLastFrame = 0;
        RemoveDestroyedEntries();

        if (navigationTarget == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (!entry.Managed)
            {
                BeginManaging(ref entry);
                entries[i] = entry;
            }

            if (!entry.Enemy.CanTickAi)
            {
                continue;
            }

            Steer(entry, navigationTarget.position, deltaTime);
            TickedLastFrame++;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            RestoreEntry(entries[i]);
        }

        TickedLastFrame = 0;
    }

    internal void Register(Enemy enemy, BehaviourRunner runner)
    {
        if (!isActiveAndEnabled
            || enemy == null
            || runner == null
            || registeredEnemies.Contains(enemy))
        {
            return;
        }

        BehaviourTree tree = runner.GetBehaviourTree();
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (tree == null || agent == null)
        {
            return;
        }

        registeredEnemies.Add(enemy);
        Entry entry = new Entry(
            enemy,
            runner,
            tree,
            agent,
            agent.speed,
            agent.radius,
            agent.enabled,
            agent.updatePosition,
            agent.updateRotation,
            agent.isStopped,
            tree.GetUpdateMode(),
            tree.GetTickRate(),
            runner.enabled);
        BeginManaging(ref entry);
        entries.Add(entry);
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

            RestoreEntry(entries[i]);

            int lastIndex = entries.Count - 1;
            entries[i] = entries[lastIndex];
            entries.RemoveAt(lastIndex);
            break;
        }
    }

    private void BeginManaging(ref Entry entry)
    {
        entry.Tree.SetUpdateMode(UpdateMode.Custom);
        entry.Runner.enabled = false;

        if (entry.Agent.enabled && entry.Agent.isOnNavMesh)
        {
            entry.Agent.ResetPath();
            entry.Agent.isStopped = true;
        }

        entry.Agent.enabled = false;
        entry.Managed = true;
    }

    private void Steer(Entry entry, Vector3 targetPosition, float deltaTime)
    {
        Transform enemyTransform = entry.Enemy.transform;
        Vector3 toTarget = targetPosition - enemyTransform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        float stopRadius = Mathf.Max(stoppingDistance, entry.AgentRadius);
        if (distance <= stopRadius || distance <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 direction = toTarget / distance;
        float moveDistance = Mathf.Min(
            entry.MoveSpeed * Mathf.Max(0.1f, speedMultiplier) * deltaTime,
            distance - stopRadius);
        enemyTransform.position += direction * moveDistance;

        if (!entry.OriginalUpdateRotation)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        enemyTransform.rotation = Quaternion.RotateTowards(
            enemyTransform.rotation,
            targetRotation,
            Mathf.Max(1f, turnSpeed) * deltaTime);
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

    private static void RestoreEntry(Entry entry)
    {
        if (entry.Agent != null)
        {
            if (entry.AgentWasEnabled && !entry.Agent.enabled)
            {
                if (NavMesh.SamplePosition(
                    entry.Enemy.transform.position,
                    out NavMeshHit hit,
                    5f,
                    NavMesh.AllAreas))
                {
                    entry.Enemy.transform.position = hit.position;
                    entry.Agent.enabled = true;
                }
            }

            if (entry.Agent.enabled)
            {
                entry.Agent.updatePosition = entry.OriginalUpdatePosition;
                entry.Agent.updateRotation = entry.OriginalUpdateRotation;
                if (entry.Agent.isOnNavMesh)
                {
                    entry.Agent.isStopped = entry.OriginalIsStopped;
                }
            }
        }

        if (entry.Tree != null)
        {
            entry.Tree.SetUpdateMode(entry.OriginalUpdateMode);
            entry.Tree.SetTickRate(entry.OriginalTickRate);
        }

        if (entry.Runner != null)
        {
            entry.Runner.enabled = entry.RunnerWasEnabled;
        }
    }

    private struct Entry
    {
        public Entry(
            Enemy enemy,
            BehaviourRunner runner,
            BehaviourTree tree,
            NavMeshAgent agent,
            float moveSpeed,
            float agentRadius,
            bool agentWasEnabled,
            bool originalUpdatePosition,
            bool originalUpdateRotation,
            bool originalIsStopped,
            UpdateMode originalUpdateMode,
            int originalTickRate,
            bool runnerWasEnabled)
        {
            Enemy = enemy;
            Runner = runner;
            Tree = tree;
            Agent = agent;
            MoveSpeed = moveSpeed;
            AgentRadius = agentRadius;
            AgentWasEnabled = agentWasEnabled;
            OriginalUpdatePosition = originalUpdatePosition;
            OriginalUpdateRotation = originalUpdateRotation;
            OriginalIsStopped = originalIsStopped;
            OriginalUpdateMode = originalUpdateMode;
            OriginalTickRate = originalTickRate;
            RunnerWasEnabled = runnerWasEnabled;
            Managed = false;
        }

        public Enemy Enemy;
        public BehaviourRunner Runner;
        public BehaviourTree Tree;
        public NavMeshAgent Agent;
        public float MoveSpeed;
        public float AgentRadius;
        public bool AgentWasEnabled;
        public bool OriginalUpdatePosition;
        public bool OriginalUpdateRotation;
        public bool OriginalIsStopped;
        public UpdateMode OriginalUpdateMode;
        public int OriginalTickRate;
        public bool RunnerWasEnabled;
        public bool Managed;
    }
}
