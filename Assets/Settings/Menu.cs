using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void Jogar()
    {
        SceneManager.LoadScene("Fase1");
    }

    public void Sair()
    {
        Application.Quit();
        Debug.Log("Saindo...");
    }
}
