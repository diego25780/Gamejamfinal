using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerencia todos os elementos visuais de UI: HUD de Pontuação e Vidas (com imagens/ícones),
/// Tela de Pause, Tela de Game Over e Alerta de Chefe (Boss Warning).
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Superior - Pontuação")]
    [Tooltip("Texto para exibir os pontos atuais.")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Texto para exibir o recorde salvo.")]
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Header("HUD Superior - Vidas (Imagens)")]
    [Tooltip("Arraste aqui as Imagens (UI Image) das vidas (ex: ícones de naves ou corações).")]
    [SerializeField] private Image[] lifeImages;

    [Tooltip("Se marcado, diminui a transparência do ícone ao perder vida em vez de fazê-lo sumir.")]
    [SerializeField] private bool fadeLostLives = false;
    [SerializeField] private Color activeLifeColor = Color.white;
    [SerializeField] private Color lostLifeColor = new Color(1f, 1f, 1f, 0.2f);

    [Header("Alerta de Chefe (Opcional)")]
    [Tooltip("Objeto/Texto de aviso 'WARNING! BOSS APPROACHING!' na tela.")]
    [SerializeField] private GameObject bossWarningPanel;
    [SerializeField] private float bossWarningDuration = 2.5f;

    [Header("Tela de Pause")]
    [SerializeField] private GameObject pausePanel;

    [Header("Tela de Game Over (Derrota)")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI finalHighScoreText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Garante que os painéis comecem ocultos
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (bossWarningPanel != null) bossWarningPanel.SetActive(false);

        // Busca o jogador na cena para inicializar os ícones de vida imediatamente
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && player.TryGetComponent<Health>(out var playerHealth))
        {
            UpdateLives(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    /// <summary>
    /// Atualiza o texto de pontuação no HUD.
    /// </summary>
    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {score:D6}";
        }
    }

    /// <summary>
    /// Atualiza o texto do High Score no HUD.
    /// </summary>
    public void UpdateHighScore(int highScore)
    {
        if (highScoreText != null)
        {
            highScoreText.text = $"HI-SCORE: {highScore:D6}";
        }
    }

    /// <summary>
    /// Atualiza a exibição visual das vidas usando as Imagens configuradas.
    /// </summary>
    public void UpdateLives(int currentLives, int maxLives)
    {
        if (lifeImages == null || lifeImages.Length == 0) return;

        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] == null) continue;

            bool isLifeActive = i < currentLives;

            if (fadeLostLives)
            {
                lifeImages[i].gameObject.SetActive(true);
                lifeImages[i].color = isLifeActive ? activeLifeColor : lostLifeColor;
            }
            else
            {
                lifeImages[i].gameObject.SetActive(isLifeActive);
            }
        }
    }

    /// <summary>
    /// Exibe temporariamente um aviso estilo arcade quando um Boss se aproxima.
    /// </summary>
    public void ShowBossWarning()
    {
        if (bossWarningPanel != null)
        {
            StopCoroutine(nameof(BossWarningRoutine));
            StartCoroutine(BossWarningRoutine());
        }
    }

    private IEnumerator BossWarningRoutine()
    {
        bossWarningPanel.SetActive(true);
        yield return new WaitForSeconds(bossWarningDuration);
        bossWarningPanel.SetActive(false);
    }

    /// <summary>
    /// Abre ou fecha o menu de pausa.
    /// </summary>
    public void ShowPauseMenu(bool isPaused)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }
        else
        {
            Debug.LogWarning("UIManager: O campo 'Pause Panel' está VAZIO no Inspector do UIManager!");
        }
    }

    /// <summary>
    /// Abre a tela de derrota exibindo pontuação final e recorde.
    /// </summary>
    public void ShowGameOverMenu(int finalScore, int highScore)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"FINAL SCORE: {finalScore:D6}";
        }

        if (finalHighScoreText != null)
        {
            finalHighScoreText.text = $"HIGH SCORE: {highScore:D6}";
        }
    }

    #region Funções para Botões da UI (OnClick)

    public void OnResumeButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }

    public void OnRestartButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    public void OnQuitButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }

    #endregion
}
