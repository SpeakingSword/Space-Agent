using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    // 退出游戏
    public void ExitGame()
    {
        Application.Quit();
    }

    // 开始游戏
    public void DoStartGame()
    {
        SceneManager.LoadScene("Demo");
    }
}
