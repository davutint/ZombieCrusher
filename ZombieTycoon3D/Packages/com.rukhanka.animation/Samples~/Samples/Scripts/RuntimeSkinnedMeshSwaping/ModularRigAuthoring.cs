using Unity.Entities;
using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
public class ModularRigAuthoring: MonoBehaviour
{
    public GameObject skinnedMeshRootBone;
    public GameObject[] rigParts;
    
////////////////////////////////////////////////////////////////////////////////////////

    class ModularRigBaker: Baker<ModularRigAuthoring>
    {
        public override void Bake(ModularRigAuthoring a)
        {
            var e = GetEntity(a, TransformUsageFlags.Renderable);
            foreach (var rp in a.rigParts)
            {
                GetEntity(rp, TransformUsageFlags.Dynamic);
            }
            var mrps = AddBuffer<ModularRigPartComponent>(e);
            for (var i = 0; i < (int)ModularBodyPart.Total; ++i)
            {
                var mrpc = new ModularRigPartComponent()
                {
                    bodyPart = (ModularBodyPart)i,
                    skinnedMeshRootBoneEntity = GetEntity(a.skinnedMeshRootBone, TransformUsageFlags.None),
                    currentPartIndex = -1
                };
                mrps.Add(mrpc);
            }
        }
    }
}
}
