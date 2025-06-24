using UnityEngine;
using System.Collections;

public class TextureAnimatorNormal : MonoBehaviour 
{
    public float     speed;
    public Material  material;
    public Texture[] textures;
    
    float frameTime;
    
	void Update() 
    {        
        int numTextures = textures.Length;
        
        if (numTextures == 0)
            return;
            
        frameTime += Time.deltaTime * speed;
        material.SetTexture("_BumpMap", textures[(int)Mathf.Abs(frameTime) % numTextures]);
	}
}

