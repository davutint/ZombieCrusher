using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField]private GameObject soundEffect;

    private void OnEnable()
    {
        EventManager.OnZombieDead+=SoundEffect;
    }

    private void OnDisable()
    {
        EventManager.OnZombieDead-=SoundEffect;

    }

    private void SoundEffect(Vector3 position)
    {
        Debug.Log("Sound Effect ÇALIŞTI");
        
        float rand=UnityEngine.Random.Range(.7f,1f);
        Debug.Log(rand+ " Random pitch");
        GameObject effect = Instantiate(soundEffect, position, Quaternion.identity);
        effect.GetComponent<AudioSource>().pitch=rand;
        effect.GetComponent<AudioSource>().Play();
        Destroy(effect,1.4f);
    }
}
