using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene"); 
    }

    public void OpenOptions()
    {
        Debug.Log("Abrindo opções...");
    }

    public void ExitGame()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
    }
}
