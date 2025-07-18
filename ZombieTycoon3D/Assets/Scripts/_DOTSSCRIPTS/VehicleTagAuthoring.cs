using Unity.Entities;
using UnityEngine;

// Vehicle entity'sine tag eklemek için authoring
public class VehicleTagAuthoring : MonoBehaviour
{
    class Baker : Baker<VehicleTagAuthoring>
    {
        public override void Bake(VehicleTagAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
                
            // Vehicle tag ekle
            AddComponent<VehicleTag>(entity);
        }
    }
}