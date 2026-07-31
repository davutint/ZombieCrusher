using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class VehicleAttachmentHitZone : MonoBehaviour
{
    private Player player;

    public void Configure(Player owner)
    {
        player = owner;
    }

    private void OnTriggerEnter(Collider other)
    {
        player?.TryImpactZombie(other, transform.position);
    }
}
