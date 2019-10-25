using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : MonoBehaviour
{
    public GameObject player;
    public Transform spawnPosition;
    public GameObject gameOverScreen;
    private GameManager gameManager;

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();    
    }

    void Update()
    {
        if(player.GetComponent<PlayerController>().PlayerHealth <= 0)
        {
            
            gameOverScreen.SetActive(true);
        }    
    }

    public void RestartGame()
    {
        Physics2D.gravity /= gameManager.GravityModifier;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Menu()
    {
        Physics2D.gravity /= gameManager.GravityModifier;
        SceneManager.LoadScene("Start");
    }
}
