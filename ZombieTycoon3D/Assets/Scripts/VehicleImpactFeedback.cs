using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineImpulseSource))]
public sealed class VehicleImpactFeedback : MonoBehaviour
{
    private const string ContactDamageAudioResourcePath =
        "ZombieContactDamage";

    [Header("Camera Impulse")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField, Min(0f)] private float minimumImpulseStrength = 0.2f;
    [SerializeField, Min(0f)] private float maximumImpulseStrength = 0.65f;
    [SerializeField, Min(0f)] private float feedbackCooldownSeconds = 0.055f;

    [Header("Contact Damage")]
    [SerializeField, Min(0f)] private float minimumContactImpulseStrength = 0.08f;
    [SerializeField, Min(0f)] private float maximumContactImpulseStrength = 0.18f;
    [SerializeField, Min(0f)] private float contactDamageFeedbackCooldown = 0.32f;

    private AudioSource contactDamageAudioSource;
    private float nextFeedbackTime;
    private float nextContactDamageFeedbackTime;
    private float mayhemIntensity;
    private bool contactImpulseRight;

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
        CreateContactDamageAudioSource();
    }

    private void OnValidate()
    {
        maximumImpulseStrength =
            Mathf.Max(minimumImpulseStrength, maximumImpulseStrength);
        maximumContactImpulseStrength = Mathf.Max(
            minimumContactImpulseStrength,
            maximumContactImpulseStrength);
        feedbackCooldownSeconds = Mathf.Max(0f, feedbackCooldownSeconds);
        contactDamageFeedbackCooldown = Mathf.Max(
            0f,
            contactDamageFeedbackCooldown);

        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        ConfigureImpulseSource();
        ConfigureContactDamageAudio();
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
                strength)
                * Mathf.Lerp(1f, 1.18f, mayhemIntensity);
            impulseSource.GenerateImpulseAtPositionWithVelocity(
                worldPosition,
                direction * impulseStrength);
        }
    }

    public void SetMayhemIntensity(float normalizedIntensity)
    {
        mayhemIntensity = Mathf.Clamp01(normalizedIntensity);
    }

    public void PlayZombieContactDamage(int effectiveAttackerCount)
    {
        if (Time.unscaledTime < nextContactDamageFeedbackTime)
        {
            return;
        }

        nextContactDamageFeedbackTime =
            Time.unscaledTime + contactDamageFeedbackCooldown;
        float strength = Mathf.InverseLerp(
            1f,
            10f,
            Mathf.Max(1, effectiveAttackerCount));

        if (impulseSource != null)
        {
            contactImpulseRight = !contactImpulseRight;
            Vector3 lateralDirection = contactImpulseRight
                ? transform.right
                : -transform.right;
            Vector3 direction =
                (lateralDirection + Vector3.up * 0.12f).normalized;
            float impulseStrength = Mathf.Lerp(
                minimumContactImpulseStrength,
                maximumContactImpulseStrength,
                strength);
            impulseSource.GenerateImpulseAtPositionWithVelocity(
                transform.position,
                direction * impulseStrength);
        }

        if (contactDamageAudioSource != null
            && contactDamageAudioSource.isActiveAndEnabled
            && contactDamageAudioSource.clip != null)
        {
            contactDamageAudioSource.pitch = Mathf.Lerp(
                0.96f,
                1.04f,
                strength);
            contactDamageAudioSource.PlayOneShot(
                contactDamageAudioSource.clip);
        }
    }

    public void PlayMayhemTierReached(MayhemTier tier)
    {
        if (impulseSource == null || tier == MayhemTier.None)
        {
            return;
        }

        float tierStrength = 0.25f + (int)tier * 0.055f;
        Vector3 direction = transform.forward + Vector3.up * 0.18f;
        impulseSource.GenerateImpulseAtPositionWithVelocity(
            transform.position,
            direction.normalized * tierStrength);
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

    private void ConfigureContactDamageAudio()
    {
        if (contactDamageAudioSource == null)
        {
            return;
        }

        contactDamageAudioSource.playOnAwake = false;
        contactDamageAudioSource.loop = false;
        contactDamageAudioSource.spatialBlend = 0f;
        contactDamageAudioSource.dopplerLevel = 0f;
    }

    private void CreateContactDamageAudioSource()
    {
        AudioClip contactDamageClip = Resources.Load<AudioClip>(
            ContactDamageAudioResourcePath);
        if (contactDamageClip == null)
        {
            return;
        }

        contactDamageAudioSource = gameObject.AddComponent<AudioSource>();
        contactDamageAudioSource.hideFlags = HideFlags.DontSave;
        contactDamageAudioSource.clip = contactDamageClip;
        contactDamageAudioSource.volume = 0.16f;
        contactDamageAudioSource.priority = 96;
        ConfigureContactDamageAudio();
    }
}
