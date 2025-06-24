using System;
using UnityEngine;

public class UpgradeTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GarageUIRoot.Instance.OpenShop();

        } 
    }

    private void OnTriggerExit(Collider other)
    {
        
        if (other.CompareTag("Player"))
        {
            GarageUIRoot.Instance.CloseAll();

        } 
    }
}
