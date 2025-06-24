using System;
using System.Collections;
using System.Collections.Generic;
using DestroyIt;
using RenownedGames.AITree;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private BehaviourRunner behaviourRunner;
    private Transform target;
   
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject meshObject;

    [SerializeField] private float health = 100f;
    [SerializeField] private GameObject ragdollPrefab;
    [SerializeField] private float destroyDelay = 3f;
    private bool isDead = false;

    private void Awake()
    {
        // Ragdoll ile ilgili bileşenleri almaya gerek yok.
        // ragdollBodies = GetComponentsInChildren<Rigidbody>();
        // ragdollColliders = GetComponentsInChildren<Collider>();

        // Ragdoll'ı devre dışı bırakmaya gerek kalmadı.
        // RagdollActive(false);
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        target = GameObject.FindGameObjectWithTag("Player").transform;
        behaviourRunner = GetComponent<BehaviourRunner>();
        animator = GetComponent<Animator>();

        SetAlive(true);
        SetPlayer();
    }

    private void SetAlive(bool value)
    {
        Blackboard blackboard = behaviourRunner.GetBlackboard();
        if (blackboard.TryGetKey("alive", out BoolKey alive))
        {
            alive.SetValue(value);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    public void DestroyIt()
    {
        if (isDead) return;
        Die();
    }

    private void Die()
    {
        isDead = true;
        SpawnRagdoll();
        EventManager.OnZombieDead?.Invoke(transform.position);
        Destroy(gameObject);
    }

    private void SpawnRagdoll()
    {
        if (ragdollPrefab != null)
        {
            GameObject ragdoll = Instantiate(ragdollPrefab, transform.position, transform.rotation);
            ApplyRagdollVelocity(ragdoll);
            StartCoroutine(DestroyRagdollAfterDelay(ragdoll));
        }
    }

    private void ApplyRagdollVelocity(GameObject ragdoll)
    {
        Rigidbody[] rigidbodies = ragdoll.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }
    }

    private IEnumerator DestroyRagdollAfterDelay(GameObject ragdoll)
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(ragdoll);
    }

    private void SetPlayer()
    {
        Blackboard blackboard = behaviourRunner.GetBlackboard();
        if (blackboard.TryGetKey("Player", out TransformKey player))
        {
            player.SetValue(target);
        }
    }
    
    public void HitByCar(float impactForce)
    {
        // NavMesh agent durduruluyor ve animatör devre dışı bırakılıyor.
        agent.isStopped = true;
        if (animator != null)
            animator.enabled = false;
        
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
