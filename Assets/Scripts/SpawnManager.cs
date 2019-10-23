using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject player;
    public Transform spawnPosition;

    void Update()
    {
        if(player.GetComponent<PlayerController>().PlayerHealth <= 0)
        {
            player.transform.position = spawnPosition.position;
            player.GetComponent<PlayerController>().PlayerHealth = 100;
            player.SetActive(true);
        }    
    }
}
