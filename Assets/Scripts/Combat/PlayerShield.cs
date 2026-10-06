using UnityEngine;

/// <summary>
/// Controla o escudo de energia da nave do jogador.
/// Absorve impactos sem que a nave perca vida.
/// </summary>
public class PlayerShield : MonoBehaviour
{
    [Header("Configurações do Escudo")]
    [Tooltip("Objeto visual do escudo (ex: sprite de círculo/bolha como filho da nave).")]
    [SerializeField] private GameObject shieldVisual;

    [Tooltip("Quantidade de golpes que o escudo suporta antes de se quebrar.")]
    [SerializeField] private int hitsToAbsorb = 1;

    [Header("Efeitos Sonoros")]
    [SerializeField] private AudioClip shieldActivateSound;
    [SerializeField] private AudioClip shieldBreakSound;

    public bool IsShieldActive { get; private set; }
    private int currentHits;

    private void Start()
    {
        // Inicia com o escudo desligado
        DeactivateShieldVisual();
    }

    /// <summary>
    /// Ativa o escudo com uma quantidade de cargas/bloqueios.
    /// </summary>
    public void ActivateShield(int hits = 1)
    {
        IsShieldActive = true;
        currentHits = Mathf.Max(hitsToAbsorb, hits);

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(true);
        }

        if (shieldActivateSound != null)
        {
            AudioSource.PlayClipAtPoint(shieldActivateSound, transform.position);
        }
    }

    /// <summary>
    /// Chamado pelo Health.cs antes de subtrair vidas.
    /// Retorna true se o escudo absorveu o golpe.
    /// </summary>
    public bool TryAbsorbDamage()
    {
        if (!IsShieldActive) return false;

        currentHits--;

        if (currentHits <= 0)
        {
            BreakShield();
        }

        return true;
    }

    private void BreakShield()
    {
        IsShieldActive = false;
        DeactivateShieldVisual();

        if (shieldBreakSound != null)
        {
            AudioSource.PlayClipAtPoint(shieldBreakSound, transform.position);
        }
    }

    private void DeactivateShieldVisual()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }
    }
}
