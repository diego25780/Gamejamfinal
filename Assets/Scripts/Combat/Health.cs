using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente de vida para Jogador, Inimigos e Chefes.
/// Suporta dano, cura, invulnerabilidade temporária com piscar de sprite (i-frames retrô),
/// feedback de dano visual (flash vermelho/branco), efeitos sonoros e eventos de morte.
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

    [Header("Feedback de Dano (Flash Visual)")]
    [Tooltip("Habilita o flash de cor ao receber dano.")]
    [SerializeField] private bool enableDamageFlash = true;

    [Tooltip("Cor do flash de impacto (vermelho por padrão).")]
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Tooltip("Duração do flash de dano em segundos.")]
    [SerializeField] private float damageFlashDuration = 0.12f;

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

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Coroutine flashCoroutine;
    private Coroutine invulnerabilityCoroutine;

    private void Awake()
    {
        currentHealth = maxHealth;

        // Captura todos os SpriteRenderers neste objeto e em filhos (ex: asas, cabine, canhões)
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                originalColors[i] = spriteRenderers[i].color;
            }
        }

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

    private void OnDisable()
    {
        RestoreOriginalColors();
    }

    /// <summary>
    /// Aplica dano à entidade respeitando invulnerabilidade temporária, escudo e ativando flash de dano.
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

        // Feedback sonoro
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        // Feedback visual imediato de impacto (flash no player e nos inimigos)
        if (enableDamageFlash)
        {
            TriggerDamageFlash();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (canBeInvulnerable)
        {
            if (invulnerabilityCoroutine != null) StopCoroutine(invulnerabilityCoroutine);
            invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine());
        }
    }

    /// <summary>
    /// Dispara o flash visual de dano imediatamente.
    /// </summary>
    public void TriggerDamageFlash()
    {
        if (!enableDamageFlash || spriteRenderers == null || spriteRenderers.Length == 0) return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            RestoreOriginalColors();
        }

        flashCoroutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = damageFlashColor;
            }
        }

        yield return new WaitForSeconds(damageFlashDuration);

        RestoreOriginalColors();
        flashCoroutine = null;
    }

    private void RestoreOriginalColors()
    {
        if (spriteRenderers == null || originalColors == null) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && i < originalColors.Length)
            {
                spriteRenderers[i].color = originalColors[i];
            }
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
            if (spriteRenderers != null)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                    {
                        spriteRenderers[i].enabled = !spriteRenderers[i].enabled;
                    }
                }
            }
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].enabled = true;
                }
            }
        }

        IsInvulnerable = false;
        invulnerabilityCoroutine = null;
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
        RestoreOriginalColors();

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
