using System.Collections;
using DestroyIt;
using RenownedGames.AITree;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private BehaviourRunner behaviourRunner;
    private Transform target;
   
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject meshObject;

    [FormerlySerializedAs("health")]
    [SerializeField] private float maxHealth = 100f;
    [FormerlySerializedAs("ragdollPrefab")]
    [SerializeField] private GameObject deathEffectPrefab;
    [FormerlySerializedAs("destroyDelay")]
    [SerializeField] private float deathEffectLifetime = 3f;
    [Header("Gameplay Profile")]
    [SerializeField] private ZombieGameplayProfile gameplayProfile;

    private OldSpawnManager poolOwner;
    private DeathEffectPool deathEffectPool;
    private ZombieAnimatorLodManager animatorLodManager;
    private ZombieAiTickManager aiTickManager;
    private Collider[] cachedColliders;
    private bool[] initialColliderStates;
    private Renderer animationRenderer;
    private float currentHealth;
    private bool initialAgentState;
    private bool initialAnimatorState;
    private bool animationGameplaySuppressed;
    private bool aiGameplaySuppressed;
    private bool hasStarted;
    private bool isDead;

    private void Awake()
    {
        CacheComponents();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        hasStarted = true;

        if (poolOwner == null)
        {
            ResolvePlayerTarget();
            ResetLifeState();
        }
        else
        {
            aiTickManager?.Register(this, behaviourRunner);
        }
    }

    private void CacheComponents()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (behaviourRunner == null)
        {
            behaviourRunner = GetComponent<BehaviourRunner>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        initialAgentState = agent != null && agent.enabled;

        if (meshObject != null)
        {
            animationRenderer = meshObject.GetComponentInChildren<Renderer>(true);
        }

        if (animationRenderer == null)
        {
            animationRenderer = GetComponentInChildren<Renderer>(true);
        }

        initialAnimatorState = animator != null && animator.enabled;
        cachedColliders = GetComponentsInChildren<Collider>(true);
        initialColliderStates = new bool[cachedColliders.Length];

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            initialColliderStates[i] = cachedColliders[i].enabled;
        }
    }

    private void ResolvePlayerTarget()
    {
        if (target != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            target = playerObject.transform;
        }
    }

    internal GameObject DeathEffectPrefab => deathEffectPrefab;
    internal ZombieArchetype Archetype => gameplayProfile.Archetype;
    internal float MovementSpeedMultiplier =>
        gameplayProfile.MovementSpeedMultiplier;
    internal float RequiredImpactSpeedMultiplier =>
        gameplayProfile.RequiredImpactSpeedMultiplier;
    internal float ContactThreatMultiplier =>
        gameplayProfile.ContactThreatMultiplier;
    internal float KillScoreMultiplier =>
        gameplayProfile.KillScoreMultiplier;
    internal string KillFeedbackLabel =>
        gameplayProfile.KillFeedbackLabel;

    internal void ConfigurePool(
        OldSpawnManager owner,
        Transform playerTarget,
        DeathEffectPool effectPool,
        ZombieAnimatorLodManager lodManager,
        ZombieAiTickManager tickManager)
    {
        poolOwner = owner;
        deathEffectPool = effectPool;
        animatorLodManager = lodManager;
        aiTickManager = tickManager;
        target = playerTarget;
        currentHealth = maxHealth;
        isDead = false;
        gameObject.SetActive(false);
    }

    internal void SpawnFromPool(Vector3 position, Quaternion rotation, Transform parent, Transform playerTarget)
    {
        target = playerTarget;
        transform.SetParent(parent, false);
        transform.SetPositionAndRotation(position, rotation);

        animationGameplaySuppressed = false;
        aiGameplaySuppressed = false;
        RestoreComponents();
        ResetLifeState();
        gameObject.SetActive(true);
        ResetAgent(position);

        if (hasStarted)
        {
            RestoreBehaviourTree();
            aiTickManager?.Register(this, behaviourRunner);
        }

        animatorLodManager?.Register(this);
    }

    internal void StoreInPool()
    {
        aiTickManager?.Unregister(this);
        animatorLodManager?.Unregister(this);
        SetAlive(false);
        AbortBehaviourTree();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        gameObject.SetActive(false);
    }

    private void RestoreComponents()
    {
        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] != null)
            {
                cachedColliders[i].enabled = initialColliderStates[i];
            }
        }

        if (animator != null)
        {
            bool isAnimationLodManaged =
                animatorLodManager != null &&
                animatorLodManager.isActiveAndEnabled;
            animator.enabled =
                !isAnimationLodManaged &&
                initialAnimatorState &&
                !animationGameplaySuppressed;
        }
    }

    internal bool CanAnimateByLod =>
        initialAnimatorState &&
        !animationGameplaySuppressed &&
        !isDead &&
        gameObject.activeInHierarchy;

    internal bool CanTickAi =>
        !aiGameplaySuppressed &&
        !isDead &&
        gameObject.activeInHierarchy;

    internal bool IsAnimationLodEnabled => animator != null && animator.enabled;

    internal bool TryPrepareAnimationLod(out Renderer renderer)
    {
        renderer = animationRenderer;
        if (animator == null || renderer == null || !initialAnimatorState)
        {
            return false;
        }

        animator.keepAnimatorStateOnDisable = true;
        animator.cullingMode = AnimatorCullingMode.CullCompletely;
        return true;
    }

    internal void SetAnimationLodEnabled(bool value)
    {
        if (animator == null)
        {
            return;
        }

        bool shouldEnable = value && CanAnimateByLod;
        if (animator.enabled != shouldEnable)
        {
            animator.enabled = shouldEnable;
        }
    }

    private void ResetLifeState()
    {
        currentHealth = maxHealth;
        isDead = false;
        SetAlive(true);
        SetPlayer();
    }

    private void ResetAgent(Vector3 position)
    {
        if (agent == null)
        {
            return;
        }

        if (initialAgentState && !agent.enabled)
        {
            agent.enabled = true;
        }

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        agent.Warp(position);
        agent.ResetPath();
        agent.isStopped = false;
    }

    private void AbortBehaviourTree()
    {
        if (!hasStarted || behaviourRunner == null)
        {
            return;
        }

        BehaviourTree behaviourTree = behaviourRunner.GetBehaviourTree();
        if (behaviourTree != null && behaviourTree.GetRootNode() != null)
        {
            behaviourTree.GetRootNode().Abort();
        }
    }

    private void RestoreBehaviourTree()
    {
        if (behaviourRunner == null)
        {
            return;
        }

        BehaviourTree behaviourTree = behaviourRunner.GetBehaviourTree();
        if (behaviourTree == null || behaviourTree.GetRootNode() == null)
        {
            return;
        }

        behaviourTree.GetRootNode().Restore();
    }

    private void SetAlive(bool value)
    {
        if (behaviourRunner == null)
        {
            return;
        }

        Blackboard blackboard = behaviourRunner.GetBlackboard();
        if (blackboard != null && blackboard.TryGetKey("alive", out BoolKey alive))
        {
            alive.SetValue(value);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void DestroyIt()
    {
        TryDestroy();
    }

    public bool TryDestroy()
    {
        if (isDead)
        {
            return false;
        }

        Die();
        return true;
    }

    private void Die()
    {
        isDead = true;
        SpawnDeathEffect();
        EventManager.OnZombieDead?.Invoke(transform.position);
        EventManager.OnZombieKilled?.Invoke(new ZombieKillEvent(
            transform.position,
            Archetype,
            KillScoreMultiplier,
            KillFeedbackLabel));

        if (poolOwner == null || !poolOwner.ReturnZombieToPool(this))
        {
            Destroy(gameObject);
        }
    }

    private void SpawnDeathEffect()
    {
        if (deathEffectPrefab == null)
        {
            return;
        }

        if (deathEffectPool != null &&
            deathEffectPool.Play(deathEffectPrefab, transform.position, transform.rotation, deathEffectLifetime))
        {
            return;
        }

        GameObject deathEffect = Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        Destroy(deathEffect, deathEffectLifetime);
    }

    private void SetPlayer()
    {
        if (behaviourRunner == null || target == null)
        {
            return;
        }

        Blackboard blackboard = behaviourRunner.GetBlackboard();
        if (blackboard != null && blackboard.TryGetKey("Player", out TransformKey player))
        {
            player.SetValue(target);
        }
    }
    
    public void HitByCar(float impactForce)
    {
        agent.isStopped = true;
        aiGameplaySuppressed = true;
        animationGameplaySuppressed = true;
        SetAnimationLodEnabled(false);

        SetAlive(false);
        
        // Enemy'nin kendi Rigidbody'sine kuvvet uygulanıyor.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(Vector3.up * impactForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("Enemy'de Rigidbody bulunamadı.");
        }
    }

    public void GetHit(Vector3 forcedirection, float impactForce)
    {
        // Bu metod içerisine çarpma sonrası yapılacaklar eklenebilir.
    }

    IEnumerator GetHitCoroutine(Vector3 force, Vector3 hitPoint, Rigidbody rb)
    {
        yield return new WaitForSeconds(0.1f);
    }
}
