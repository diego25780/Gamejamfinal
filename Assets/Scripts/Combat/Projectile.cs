using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla o comportamento de projéteis (tiro do jogador ou dos inimigos).
/// Move-se continuamente em linha reta, causa dano e se autodestrói.
/// Suporta:
/// - Rotação automática na direção de disparo
/// - Dano customizado e escala
/// - Tiro Perfurante (atravessa múltiplos inimigos sem sumir no primeiro)
/// - Tiro Criogênico (congela/desacelera os alvos atingidos)
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Configurações do Projétil")]
    [Tooltip("Velocidade com que o tiro se desloca.")]
    [SerializeField] private float speed = 16f;

    [Tooltip("Direção do tiro. (Vector2.right para nave atirando para a direita).")]
    [SerializeField] private Vector2 direction = Vector2.right;

    [Tooltip("Dano causado ao atingir um alvo com componente Health.")]
    [SerializeField] private int damage = 1;

    [Tooltip("Tempo em segundos até o projétil se autodestruir caso não acerte nada.")]
    [SerializeField] private float lifeTime = 4f;

    [Tooltip("Tag dos objetos que este projétil deve atingir (ex: 'Enemy' ou 'Player').")]
    [SerializeField] private string targetTag = "Enemy";

    [Header("Impacto Visual")]
    [SerializeField] private GameObject hitEffectPrefab;

    [Header("Modificadores Especiais")]
    [SerializeField] private bool isPiercing = false;
    [SerializeField] private int maxPierces = 3;
    [SerializeField] private bool isCryo = false;
    [SerializeField] private float freezeDuration = 2.5f;

    private int remainingPierces = 1;
    private HashSet<Collider2D> alreadyHitColliders = new HashSet<Collider2D>();
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (speed <= 0f) speed = 16f;
        if (direction == Vector2.zero) direction = Vector2.right;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        if (TryGetComponent<Collider2D>(out var col))
        {
            col.isTrigger = true;
        }

        remainingPierces = isPiercing ? maxPierces : 1;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (speed <= 0f) speed = 16f;
        transform.position += (Vector3)(direction.normalized * (speed * Time.deltaTime));
    }

    /// <summary>
    /// Configura direção, tag alvo e opcionalmente dano aumentado.
    /// </summary>
    public void Setup(Vector2 newDirection, string newTargetTag, int customDamage = -1)
    {
        if (newDirection != Vector2.zero)
        {
            direction = newDirection.normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        targetTag = newTargetTag;

        if (customDamage > 0)
        {
            damage = customDamage;
        }
    }

    /// <summary>
    /// Aplica efeitos de Tiro Perfurante e/ou Tiro Criogênico no projétil.
    /// </summary>
    public void SetupModifiers(bool piercing, int pierces = 3, bool cryo = false)
    {
        isPiercing = piercing;
        maxPierces = pierces;
        remainingPierces = isPiercing ? maxPierces : 1;
        isCryo = cryo;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            if (isCryo)
            {
                spriteRenderer.color = new Color(0.3f, 0.9f, 1f, 1f); // Ciano brilhante
            }
            else if (isPiercing)
            {
                spriteRenderer.color = new Color(1f, 0.2f, 0.9f, 1f); // Magenta/Roxo neon
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!string.IsNullOrEmpty(targetTag) && !collision.CompareTag(targetTag))
        {
            return;
        }

        // Evita dano duplo no mesmo inimigo ao atravessá-lo
        if (alreadyHitColliders.Contains(collision))
        {
            return;
        }
        alreadyHitColliders.Add(collision);

        // 1. Causa dano
        if (collision.TryGetComponent<Health>(out var targetHealth))
        {
            targetHealth.TakeDamage(damage);
        }

        // 2. Aplica congelamento se for Criogênico
        if (isCryo)
        {
            FreezeEffect.Apply(collision.gameObject, freezeDuration, 0.3f);
        }

        // 3. Efeito de impacto
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // 4. Lógica de perfuração
        remainingPierces--;
        if (remainingPierces <= 0)
        {
            Destroy(gameObject);
        }
    }

    public int GetDamage() => damage;
}
