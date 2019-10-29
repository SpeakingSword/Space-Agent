using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : MonoBehaviour
{
    public GameObject player;                       // 玩家实例
    public Transform spawnPosition;                 // 重生位置
    public GameObject gameOverScreen;               // 玩家死亡后的显示界面
    private GameManager gameManager;                // 游戏管理者实例

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();    
    }

    void Update()
    {
        // 如果玩家的生命值小于等于0则显示"Game Over"
        if(player.GetComponent<PlayerController>().PlayerHealth <= 0)
        {
            gameOverScreen.SetActive(true);
        }    
    }

    // 重新开始游戏
    public void RestartGame()
    {
        Physics2D.gravity /= gameManager.GravityModifier;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 返回标题界面
    public void Menu()
    {
        Physics2D.gravity /= gameManager.GravityModifier;
        SceneManager.LoadScene("Start");
    }
}
