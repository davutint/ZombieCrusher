using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    // Zombi öldüğünde pozisyon bilgisiyle çağrılır.
    public static Action<Vector3> OnZombieDead;

    // Oyuncunun veya aracın ölümünü tetiklemek için kullanılır.
    public static Action OnPlayerDeath;
}
