using System.Collections;
using UnityEngine;

/// <summary>
/// Gerenciador de Distorção Temporal (Bullet Time / Câmera Lenta):
/// Desacelera todo o jogo enquanto a nave do jogador continua se movendo normalmente.
/// </summary>
public class TimeDilationManager : MonoBehaviour
{
    public static TimeDilationManager Instance { get; private set; }

    [Header("Configuração de Câmera Lenta")]
    [Tooltip("Escala de tempo durante a distorção (0.4 = 40% da velocidade normal).")]
    [SerializeField] private float slowScale = 0.4f;

    private Coroutine dilationRoutine;
    private float defaultFixedDeltaTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    public void ActivateDilation(float duration = 5f)
    {
        if (dilationRoutine != null)
        {
            StopCoroutine(dilationRoutine);
        }
        dilationRoutine = StartCoroutine(DilationRoutine(duration));
    }

    private IEnumerator DilationRoutine(float duration)
    {
        Time.timeScale = slowScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;

        // Usa tempo não afetado pela escala (unscaled)
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        dilationRoutine = null;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
}
