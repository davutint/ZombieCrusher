using System;
using System.Collections.Generic;
using System.Reflection;
using RenownedGames.AITree;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ZombieAiTickManager : MonoBehaviour
{
    [Header("AI Tick Budget")]
    [SerializeField, Min(1)] private int tickIntervalFrames = 4;

    private readonly List<Entry> entries = new List<Entry>(512);
    private readonly HashSet<Enemy> registeredEnemies = new HashSet<Enemy>();

    private BehaviourTreeUpdate updateBehaviourTree;
    private int nextEntryIndex;

    public int RegisteredCount => entries.Count;
    public int TickedLastFrame { get; private set; }

    private delegate State BehaviourTreeUpdate(BehaviourTree tree);

    private void Awake()
    {
        MethodInfo updateMethod = typeof(BehaviourTree).GetMethod(
            "OnUpdate",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (updateMethod == null)
        {
            Debug.LogError("ZombieAiTickManager: BehaviourTree.OnUpdate could not be resolved.", this);
            enabled = false;
            return;
        }

        try
        {
            updateBehaviourTree = (BehaviourTreeUpdate)Delegate.CreateDelegate(
                typeof(BehaviourTreeUpdate),
                updateMethod);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError(
                $"ZombieAiTickManager: Behaviour tree update delegate could not be created. {exception.Message}",
                this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        int activationFrame = Time.frameCount + 1;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            entry.Managed = false;
            entry.ActivationFrame = activationFrame;
            entries[i] = entry;
        }
    }

    private void Update()
    {
        TickedLastFrame = 0;

        if (updateBehaviourTree == null)
        {
            return;
        }

        RemoveDestroyedEntries();
        int entryCount = entries.Count;
        if (entryCount == 0)
        {
            return;
        }

        int interval = Mathf.Max(1, tickIntervalFrames);
        int tickBudget = Mathf.CeilToInt(entryCount / (float)interval);
        int visitedCount = 0;

        while (visitedCount < entryCount && TickedLastFrame < tickBudget)
        {
            if (nextEntryIndex >= entries.Count)
            {
                nextEntryIndex = 0;
            }

            int entryIndex = nextEntryIndex;
            nextEntryIndex++;
            visitedCount++;

            Entry entry = entries[entryIndex];
            if (!entry.Managed)
            {
                if (Time.frameCount < entry.ActivationFrame)
                {
                    continue;
                }

                entry.Tree.SetUpdateMode(UpdateMode.Custom);
                entry.Tree.SetTickRate(interval);
                entry.Runner.enabled = false;
                entry.Managed = true;
                entries[entryIndex] = entry;
            }
            else if (entry.Tree.GetTickRate() != interval)
            {
                entry.Tree.SetTickRate(interval);
            }

            if (!entry.Enemy.CanTickAi)
            {
                continue;
            }

            updateBehaviourTree(entry.Tree);
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
        if (!isActiveAndEnabled ||
            updateBehaviourTree == null ||
            enemy == null ||
            runner == null ||
            registeredEnemies.Contains(enemy))
        {
            return;
        }

        BehaviourTree tree = runner.GetBehaviourTree();
        if (tree == null)
        {
            return;
        }

        registeredEnemies.Add(enemy);
        entries.Add(new Entry(
            enemy,
            runner,
            tree,
            tree.GetUpdateMode(),
            tree.GetTickRate(),
            runner.enabled,
            Time.frameCount + 1));
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
            nextEntryIndex = 0;
            break;
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
            nextEntryIndex = 0;
        }
    }

    private static void RestoreEntry(Entry entry)
    {
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
            UpdateMode originalUpdateMode,
            int originalTickRate,
            bool runnerWasEnabled,
            int activationFrame)
        {
            Enemy = enemy;
            Runner = runner;
            Tree = tree;
            OriginalUpdateMode = originalUpdateMode;
            OriginalTickRate = originalTickRate;
            RunnerWasEnabled = runnerWasEnabled;
            ActivationFrame = activationFrame;
            Managed = false;
        }

        public Enemy Enemy;
        public BehaviourRunner Runner;
        public BehaviourTree Tree;
        public UpdateMode OriginalUpdateMode;
        public int OriginalTickRate;
        public bool RunnerWasEnabled;
        public int ActivationFrame;
        public bool Managed;
    }
}
