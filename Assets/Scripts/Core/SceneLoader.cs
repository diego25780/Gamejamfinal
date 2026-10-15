using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script versátil para gerenciar e executar troca de cenas no Unity.
/// Pode ser usado:
/// 1. Em botões de UI (Menu Principal, Jogar Novamente, Sair, etc.) via OnClick().
/// 2. Chamado por código C# em qualquer script (ex: SceneLoader.Load("NomeDaCena")).
/// 3. Em gatilhos de colisão 2D (portais, saída de fase, checkpoints).
/// 4. Com suporte opcional a atraso (delay) para tocar sons de clique e transição de fade.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Configurações Principais")]
    [Tooltip("Nome da cena padrão para carregar caso use funções sem parâmetro (ex: 'Gameplay', 'Fase1', 'Menu').")]
    [SerializeField] private string defaultSceneName = "Gameplay";

    [Tooltip("Tempo padrão de espera (em segundos) antes de trocar de cena (ideal para esperar o som do botão tocar).")]
    [SerializeField] private float defaultDelay = 0.2f;

    [Header("Transição de Tela Suave (Opcional)")]
    [Tooltip("CanvasGroup de uma imagem preta na tela para efeito de Fade Out (clareia/escurece). Deixe vazio se não for usar.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Gatilho 2D / Portal (Opcional)")]
    [Tooltip("Se marcado, troca de cena automaticamente quando a nave entrar neste Trigger 2D.")]
    [SerializeField] private bool loadOnTrigger2D = false;
    [Tooltip("Tag necessária para ativar a troca de cena no colisor (padrão: Player).")]
    [SerializeField] private string triggerTag = "Player";
    [Tooltip("Nome da cena que o gatilho 2D vai carregar.")]
    [SerializeField] private string triggerSceneName = "";

    private bool isTransitioning = false;

    private void Awake()
    {
        // Garante que o Instance esteja disponível
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Se houver CanvasGroup de fade, garante que a cena comece transparente
        if (fadeCanvasGroup != null)
        {
            StartCoroutine(FadeInRoutine());
        }
    }

    // =========================================================================
    // MÉTODOS PÚBLICOS (Conecte estes métodos no OnClick dos Botões da UI)
    // =========================================================================

    /// <summary>
    /// Carrega uma cena pelo nome.
    /// Pode ser chamado diretamente no OnClick do Button digitando o nome da cena no campo.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;

        if (defaultDelay > 0f || fadeCanvasGroup != null)
        {
            StartCoroutine(LoadSceneWithTransition(sceneName, defaultDelay));
        }
        else
        {
            ExecuteLoad(sceneName);
        }
    }

    /// <summary>
    /// Carrega a cena configurada no campo 'Default Scene Name'.
    /// Útil para botões simples 'Jogar' ou 'Começar'.
    /// </summary>
    public void LoadDefaultScene()
    {
        if (!string.IsNullOrEmpty(defaultSceneName))
        {
            LoadScene(defaultSceneName);
        }
        else
        {
            Debug.LogWarning("[SceneLoader] O campo 'Default Scene Name' está vazio no Inspector!");
        }
    }

    /// <summary>
    /// Carrega uma cena pelo índice configurado no Build Settings (ex: 0 = Menu, 1 = Fase 1).
    /// </summary>
    public void LoadSceneByIndex(int sceneIndex)
    {
        if (isTransitioning) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }

    /// <summary>
    /// Recarrega a cena atual (perfeito para botões de 'Reiniciar' / 'Tentar Novamente' no Game Over).
    /// </summary>
    public void ReloadCurrentScene()
    {
        if (isTransitioning) return;
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        LoadScene(activeScene.name);
    }

    /// <summary>
    /// Carrega a próxima cena na ordem do Build Settings.
    /// </summary>
    public void LoadNextScene()
    {
        if (isTransitioning) return;
        Time.timeScale = 1f;
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.LogWarning("[SceneLoader] Não há próxima cena configurada no Build Settings!");
        }
    }

    /// <summary>
    /// Carrega uma cena com um tempo de espera personalizado (em segundos).
    /// </summary>
    public void LoadSceneWithDelay(string sceneName, float delay)
    {
        if (isTransitioning) return;
        StartCoroutine(LoadSceneWithTransition(sceneName, delay));
    }

    /// <summary>
    /// Fecha o jogo na build executável ou encerra o PlayMode se estiver testando no Unity Editor.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[SceneLoader] Encerrando o jogo...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // =========================================================================
    // MÉTODOS ESTÁTICOS (Permite chamar SceneLoader.Load("Fase") de qualquer script C#)
    // =========================================================================

    public static void Load(string sceneName)
    {
        if (Instance != null)
        {
            Instance.LoadScene(sceneName);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }

    public static void Reload()
    {
        if (Instance != null)
        {
            Instance.ReloadCurrentScene();
        }
        else
        {
            Time.timeScale = 1f;
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }
    }

    // =========================================================================
    // ROTINAS DE TRANSIÇÃO E CARREGAMENTO
    // =========================================================================

    private IEnumerator LoadSceneWithTransition(string sceneName, float delay)
    {
        isTransitioning = true;

        // Se houver fade configurado, faz o fade-out para preto
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        // Aguarda o delay usando tempo não-escalado (funciona mesmo com jogo pausado em Time.timeScale = 0)
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        ExecuteLoad(sceneName);
    }

    private IEnumerator FadeInRoutine()
    {
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = false;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
    }

    private void ExecuteLoad(string sceneName)
    {
        // Garante que o tempo do jogo seja despausado antes de entrar na nova cena
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"[SceneLoader] A cena '{sceneName}' não pôde ser carregada! " +
                           "Certifique-se de que o nome está digitado corretamente e que a cena foi adicionada no Unity em: " +
                           "File > Build Settings > Scenes In Build.");
        }
    }

    // =========================================================================
    // GATILHO DE COLISÃO 2D (Para portais, passagens ou fim de fase)
    // =========================================================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!loadOnTrigger2D) return;

        if (collision.CompareTag(triggerTag))
        {
            string target = !string.IsNullOrEmpty(triggerSceneName) ? triggerSceneName : defaultSceneName;
            if (!string.IsNullOrEmpty(target))
            {
                LoadScene(target);
            }
        }
    }
}
