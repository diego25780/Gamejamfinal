using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gerenciador central do jogo: controla ciclo de vida, pontuação,
/// recorde (High Score persistente com PlayerPrefs), Pause e Game Over.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Pontuação & Recorde")]
    [SerializeField] private int currentScore = 0;
    private int highScore = 0;
    private const string HighScoreKey = "RetroShmup_HighScore";

    public int CurrentScore => currentScore;
    public int HighScore => highScore;
    public bool IsPaused { get; private set; }
    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Garante que o tempo esteja rodando ao iniciar a cena
        Time.timeScale = 1f;

        // Carrega o recorde gravado
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void Start()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(currentScore);
            UIManager.Instance.UpdateHighScore(highScore);
        }
    }

    private void Update()
    {
        // Tecla de Pause (ESC ou P) - somente se o jogo não tiver terminado
        if (!IsGameOver && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
        {
            TogglePause();
        }

        // Atalho rápido de teclado no Game Over para reiniciar
        if (IsGameOver && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    /// <summary>
    /// Adiciona pontos ao placar e atualiza UI e Recorde.
    /// </summary>
    public void AddScore(int amount)
    {
        if (IsGameOver) return;

        currentScore += amount;

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(currentScore);
            UIManager.Instance.UpdateHighScore(highScore);
        }
    }

    /// <summary>
    /// Alterna entre pausar e despausar o jogo.
    /// </summary>
    public void TogglePause()
    {
        if (IsGameOver) return;

        IsPaused = !IsPaused;

        // 0 congela todo o tempo de física e corrotinas do jogo
        Time.timeScale = IsPaused ? 0f : 1f;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPauseMenu(IsPaused);
        }
    }

    /// <summary>
    /// Chamado quando a nave do jogador é destruída (Derrota).
    /// </summary>
    public void GameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;

        // Atualiza recorde final
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverMenu(currentScore, highScore);
        }
    }

    /// <summary>
    /// Reinicia a fase atual.
    /// </summary>
    public void RestartGame()
    {
        // Importante: descongela o tempo antes de recarregar a cena
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    /// <summary>
    /// Fecha o jogo ou para o Playmode no editor.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
