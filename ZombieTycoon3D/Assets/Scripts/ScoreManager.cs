using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private void OnEnable()
    {
        EventManager.OnZombieDead += GetScore;
    }

    private void OnDisable()
    {
        EventManager.OnZombieDead -= GetScore;
    }

    private void GetScore(Vector3 position)
    {
        //herzombi öldürüşte 10 para ver
        //skoru kaydet paraya çevir vs..
    }
}
