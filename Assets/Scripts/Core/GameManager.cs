using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gerenciador central do jogo: controla ciclo de vida, pontuação,
/// recorde (High Score persistente com PlayerPrefs), invocação de Chefes (a cada 5k),
/// Pause e Game Over.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Pontuação & Recorde")]
    [SerializeField] private int currentScore = 0;
    private int highScore = 0;
    private const string HighScoreKey = "RetroShmup_HighScore";

    [Header("Sistema de Boss (Chefe)")]
    [Tooltip("Intervalo de pontuação para o Boss aparecer (padrão: 5000).")]
    [SerializeField] private int scoreIntervalForBoss = 5000;
    private int nextBossScoreThreshold = 5000;
    private bool isBossActive = false;

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

        nextBossScoreThreshold = scoreIntervalForBoss;
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
    /// Adiciona pontos ao placar, atualiza UI/Recorde e verifica se atingiu marco do Boss.
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

        // Invoca o chefe quando atingir cada múltiplo de 5000 pontos
        if (!isBossActive && currentScore >= nextBossScoreThreshold)
        {
            TriggerBossFight();
        }
    }

    private void TriggerBossFight()
    {
        isBossActive = true;
        nextBossScoreThreshold += scoreIntervalForBoss;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBossWarning();
        }

        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.SpawnBoss();
        }
        else
        {
            Debug.LogWarning("GameManager: EnemySpawner.Instance não encontrado ao tentar invocar o Boss!");
        }
    }

    /// <summary>
    /// Chamado pelo BossController quando o chefe é derrotado.
    /// </summary>
    public void OnBossDefeated()
    {
        isBossActive = false;

        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.ResumeSpawning();
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
        else
        {
            Debug.LogWarning("GameManager: UIManager.Instance não foi encontrado na cena! Verifique se há um GameObject com o script UIManager.");
        }
    }

    /// <summary>
    /// Chamado quando a nave do jogador é destruída (Derrota).
    /// </summary>
    public void GameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;

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
