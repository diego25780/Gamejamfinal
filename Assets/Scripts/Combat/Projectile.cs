using UnityEngine;

/// <summary>
/// Controla o comportamento de projéteis (tiro do jogador ou dos inimigos).
/// Move-se continuamente em linha reta, causa dano e se autodestrói.
/// Suporta rotação automática na direção de disparo, dano customizado e escala.
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

    private void Awake()
    {
        // Garante valores válidos mesmo se o Inspector estiver zerado
        if (speed <= 0f) speed = 16f;
        if (direction == Vector2.zero) direction = Vector2.right;

        // Se tiver Rigidbody2D, configura como Kinematic e sem gravidade
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        // Garante que o colisor seja Trigger
        if (TryGetComponent<Collider2D>(out var col))
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (speed <= 0f) speed = 16f;

        // Movimentação direta de posição no mundo
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

            // Rotaciona o sprite para apontar na direção do voo
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        targetTag = newTargetTag;

        if (customDamage > 0)
        {
            damage = customDamage;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!string.IsNullOrEmpty(targetTag) && !collision.CompareTag(targetTag))
        {
            return;
        }

        if (collision.TryGetComponent<Health>(out var targetHealth))
        {
            targetHealth.TakeDamage(damage);
        }

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    public int GetDamage() => damage;
}
