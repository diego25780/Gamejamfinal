using UnityEngine;

/// <summary>
/// Controla o comportamento de projéteis (tiro do jogador ou dos inimigos).
/// Move-se continuamente em linha reta, causa dano e se autodestrói.
/// Configura automaticamente colisor e física para evitar que o tiro trave ou caia.
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

        // Se tiver Rigidbody2D, configura como Kinematic e sem gravidade para não travar fisicamente
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        // Garante que o colisor seja Trigger para não bater como parede sólida na própria nave
        if (TryGetComponent<Collider2D>(out var col))
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        // Autodestruição após o tempo limite
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Proteção contra velocidade zerada no Inspector
        if (speed <= 0f) speed = 16f;

        // Movimentação direta de posição no mundo (100% imune a bloqueios de física)
        transform.position += (Vector3)(direction.normalized * (speed * Time.deltaTime));
    }

    public void Setup(Vector2 newDirection, string newTargetTag)
    {
        if (newDirection != Vector2.zero)
        {
            direction = newDirection.normalized;
        }
        targetTag = newTargetTag;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignora colisões com objetos que não sejam o alvo definido
        if (!string.IsNullOrEmpty(targetTag) && !collision.CompareTag(targetTag))
        {
            return;
        }

        // Aplica dano caso o alvo possua Health
        if (collision.TryGetComponent<Health>(out var targetHealth))
        {
            targetHealth.TakeDamage(damage);
        }

        // Efeito visual de impacto opcional
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
