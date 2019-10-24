using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject player;
    public Transform spawnPosition;
    private GameManager gameManager;

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();    
    }

    void Update()
    {
        if(player.GetComponent<PlayerController>().PlayerHealth <= 0)
        {
            player.transform.position = spawnPosition.position;
            player.GetComponent<PlayerController>().PlayerHealth = gameManager.PlayerMaxHealth;
            player.SetActive(true);
        }    
    }
}
