using Unity.Entities;
using Unity.Mathematics;

// Koşma animasyonu için gerekli componentler
public struct RunningAnimation : IComponentData
{
    public float animationSpeed;
    public float limbSwingAmount;
    public float currentTime;
    public bool isRunning;
}

// Karakterin uzuvlarının (kol/bacak) referanslarını tutar
public struct LimbReferences : IComponentData
{
    public Entity leftArm;
    public Entity rightArm;
    public Entity leftLeg;
    public Entity rightLeg;
}

// Her uzuv için orijinal rotasyonu saklar
public struct OriginalRotation : IComponentData
{
    public quaternion value;
}