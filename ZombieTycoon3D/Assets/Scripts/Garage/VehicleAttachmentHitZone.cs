using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class VehicleAttachmentHitZone : MonoBehaviour
{
    private Player player;
    private float contactImpactPowerMultiplier = 1f;
    private string feedbackLabel;
    private GarageAttachmentFeedbackTone feedbackTone;

    public void Configure(
        Player owner,
        GarageAttachmentEffect effect)
    {
        player = owner;
        contactImpactPowerMultiplier = effect.ContactImpactPowerMultiplier;
        feedbackLabel = effect.FeedbackLabel;
        feedbackTone = effect.FeedbackTone;
    }

    private void OnTriggerEnter(Collider other)
    {
        player?.TryImpactZombie(
            other,
            transform.position,
            contactImpactPowerMultiplier,
            feedbackLabel,
            feedbackTone);
    }
}
