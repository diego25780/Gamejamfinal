using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente de vida para Jogador, Inimigos e Chefes.
/// Suporta dano, cura, invulnerabilidade temporária com piscar de sprite (i-frames retrô),
/// efeitos sonoros e eventos de morte.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    [Header("Invulnerabilidade (i-frames)")]
    [Tooltip("Tempo em segundos que a nave fica imune e piscando após tomar dano.")]
    [SerializeField] private float invulnerabilityDuration = 1.5f;
    [SerializeField] private bool canBeInvulnerable = false;

    [Header("Efeitos & Feedbacks")]
    [Tooltip("Prefab de explosão/partículas instanciado ao morrer.")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Eventos")]
    public UnityEvent<int, int> onHealthChanged; // (vidaAtual, vidaMaxima)
    public UnityEvent onDeath;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;
    public bool IsInvulnerable { get; private set; }

    public void SetInvulnerable(bool invulnerable)
    {
        IsInvulnerable = invulnerable;
    }

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Se este objeto for o Player, ativa invulnerabilidade automática
        if (CompareTag("Player"))
        {
            canBeInvulnerable = true;
        }
    }

    private void Start()
    {
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Aplica dano à entidade respeitando invulnerabilidade temporária e escudo.
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (IsDead || IsInvulnerable || damageAmount <= 0) return;

        // Se a entidade possuir um escudo ativo (PlayerShield), ele absorve o golpe
        if (TryGetComponent<PlayerShield>(out var shield) && shield.TryAbsorbDamage())
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (canBeInvulnerable)
        {
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    /// <summary>
    /// Corrotina clássica de i-frames: a nave pisca e fica invulnerável por um tempo.
    /// </summary>
    private IEnumerator InvulnerabilityRoutine()
    {
        IsInvulnerable = true;

        float elapsed = 0f;
        float blinkInterval = 0.1f;

        while (elapsed < invulnerabilityDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        IsInvulnerable = false;
    }

    /// <summary>
    /// Restaura vida da entidade.
    /// </summary>
    public void Heal(int healAmount)
    {
        if (IsDead || healAmount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Executa a rotina de destruição/morte.
    /// </summary>
    private void Die()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        onDeath?.Invoke();
        Destroy(gameObject);
    }
}
