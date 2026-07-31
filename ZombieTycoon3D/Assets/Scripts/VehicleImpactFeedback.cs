using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineImpulseSource))]
public sealed class VehicleImpactFeedback : MonoBehaviour
{
    [Header("Camera Impulse")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField, Min(0f)] private float minimumImpulseStrength = 0.2f;
    [SerializeField, Min(0f)] private float maximumImpulseStrength = 0.65f;
    [SerializeField, Min(0f)] private float feedbackCooldownSeconds = 0.055f;

    private float nextFeedbackTime;

    private void Reset()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        ConfigureImpulseSource();
    }

    private void Awake()
    {
        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        ConfigureImpulseSource();
    }

    private void OnValidate()
    {
        maximumImpulseStrength =
            Mathf.Max(minimumImpulseStrength, maximumImpulseStrength);
        feedbackCooldownSeconds = Mathf.Max(0f, feedbackCooldownSeconds);

        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        ConfigureImpulseSource();
    }

    public void PlayZombieImpact(
        Vector3 worldPosition,
        Vector3 travelDirection,
        float normalizedStrength)
    {
        if (Time.unscaledTime < nextFeedbackTime)
        {
            return;
        }

        float strength = Mathf.Clamp01(normalizedStrength);
        nextFeedbackTime =
            Time.unscaledTime + feedbackCooldownSeconds;

        if (impulseSource != null)
        {
            Vector3 direction = travelDirection.sqrMagnitude > 0.001f
                ? travelDirection.normalized
                : transform.forward;
            float impulseStrength = Mathf.Lerp(
                minimumImpulseStrength,
                maximumImpulseStrength,
                strength);
            impulseSource.GenerateImpulseAtPositionWithVelocity(
                worldPosition,
                direction * impulseStrength);
        }
    }

    private void ConfigureImpulseSource()
    {
        if (impulseSource == null
            || impulseSource.m_ImpulseDefinition == null)
        {
            return;
        }

        CinemachineImpulseDefinition definition =
            impulseSource.m_ImpulseDefinition;
        definition.m_ImpulseChannel = 1;
        definition.m_ImpulseShape =
            CinemachineImpulseDefinition.ImpulseShapes.Bump;
        definition.m_ImpulseDuration = 0.1f;
        definition.m_ImpulseType =
            CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        definition.m_DissipationDistance = 100f;
        definition.m_DissipationRate = 0.25f;
        definition.m_PropagationSpeed = 343f;
    }
}
