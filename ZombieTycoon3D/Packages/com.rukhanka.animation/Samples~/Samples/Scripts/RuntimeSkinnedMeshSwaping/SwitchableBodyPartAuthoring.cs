using TMPro;
using Unity.Entities;
using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class SwitchableBodyPartAuthoring: MonoBehaviour
{
    public ModularBodyPart bodyPart;
    
////////////////////////////////////////////////////////////////////////////////////////

    class SwitchableBodyPartBaker: Baker<SwitchableBodyPartAuthoring>
    {
        public override void Bake(SwitchableBodyPartAuthoring a)
        {
            var e = GetEntity(a, TransformUsageFlags.Renderable);
            var sbp = new SwitchableBodyPartComponent()
            {
                bodyPart = a.bodyPart,
                name = a.name,
            };
            AddComponent(e, sbp);
        }
    }
}
}
