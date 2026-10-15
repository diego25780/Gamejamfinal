using UnityEngine;
using UnityEngine.SceneManagement;

public class Opções : MonoBehaviour
{
    public void Opcoes()
    {
        SceneManager.LoadScene("Opções");
    }

    public void Sair()
    {
        Application.Quit();
        Debug.Log("Saindo...");
    }
}