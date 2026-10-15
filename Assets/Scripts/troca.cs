using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    // Troca de cena pelo número (índice) no Build Settings
    public void TrocarCena(int numeroDaCena)
    {
        SceneManager.LoadScene(numeroDaCena);
    }

    // Sai do jogo
    public void SairDoJogo()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // para o Play no editor
#else
        Application.Quit(); // fecha o jogo no build
#endif
    }
}