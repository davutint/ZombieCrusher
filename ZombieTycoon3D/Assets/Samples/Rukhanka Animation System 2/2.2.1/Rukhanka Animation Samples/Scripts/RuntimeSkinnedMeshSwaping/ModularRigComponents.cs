
using Unity.Collections;
using Unity.Entities;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
public enum ModularBodyPart
{
    Head,
    Body,
    LeftArm,
    RightArm,
    Total
}
    
////////////////////////////////////////////////////////////////////////////////////////

public struct SwitchableBodyPartComponent: IComponentData
{
    public ModularBodyPart bodyPart;
    public FixedString64Bytes name;
}

////////////////////////////////////////////////////////////////////////////////////////

public struct ModularRigPartComponent: IBufferElementData
{
    public ModularBodyPart bodyPart;
    public Entity skinnedMeshRootBoneEntity;
    public int currentPartIndex;
    public FixedList64Bytes<Entity> partsList;
}

}
