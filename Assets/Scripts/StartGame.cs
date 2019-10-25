using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    public void ExitGame()
    {
        Application.Quit();
    }

    public void DoStartGame()
    {
        SceneManager.LoadScene("Demo");
    }
}
