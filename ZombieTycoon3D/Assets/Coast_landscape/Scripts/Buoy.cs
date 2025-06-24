using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buoy : MonoBehaviour {

    float sinX1;
    float sinX2;
    float sinZ1;
    float sinZ2;

    float speedX1;
    float speedX2;
    float speedZ1;
    float speedZ2;


    // Start is called before the first frame update
    void Start()
    {
        sinX1 = Random.Range(0.0f, 360.0f);       
        sinX2 = Random.Range(0.0f, 360.0f);       
        sinZ1 = Random.Range(0.0f, 360.0f);       
        sinZ2 = Random.Range(0.0f, 360.0f);       

        speedX1 = Random.Range(16.0f, 24.0f);       
        speedX2 = Random.Range(35.0f, 48.0f);       
        speedZ1 = Random.Range(13.0f, 30.0f);       
        speedZ2 = Random.Range(28.0f, 45.0f);       

    }

    // Update is called once per frame
    void Update()
    {
        sinX1 = (sinX1 + Time.deltaTime * speedX1) % 360.0f;
        sinX2 = (sinX2 + Time.deltaTime * speedX2) % 360.0f;
        sinZ1 = (sinZ1 + Time.deltaTime * speedZ1) % 360.0f;
        sinZ2 = (sinZ2 + Time.deltaTime * speedZ2) % 360.0f;

        float angleX =  ((Mathf.Sin(((2*Mathf.PI) / 360.0f) * sinX1) + Mathf.Sin(((2*Mathf.PI) / 360.0f) * sinX2)) / 2.0f) * 15.0f;
        float angleZ =  ((Mathf.Sin(((2*Mathf.PI) / 360.0f) * sinZ1) + Mathf.Sin(((2*Mathf.PI) / 360.0f) * sinZ2)) / 2.0f) * 15.0f;

        transform.localEulerAngles = new Vector3(angleX, 0.0f, angleZ);
    }
}
