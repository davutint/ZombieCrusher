using System;
using UnityEngine;

public class TestEnemy : MonoBehaviour
{
    [SerializeField] private Transform ragdollParent;
    [SerializeField]private Collider[]  ragdollColliders;
    [SerializeField] Rigidbody[] ragdollRigidbodies;

    private void Awake()
    {
        ragdollColliders = GetComponentsInChildren<Collider>();
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        RagdollActive(false);
    }

    public void RagdollActive(bool active)
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = !active;
        }
    }
}
