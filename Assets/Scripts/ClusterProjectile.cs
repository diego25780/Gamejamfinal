using System.Collections;
using UnityEngine;

/// <summary>
/// Projétil especial do Boss (Ataque Agressivo / Cluster Focado):
/// - Dispara em direção ao Player em alta velocidade.
/// - Desacelera rapidamente no ar enquanto pisca em alerta.
/// - Ao detonar, dispara os fragmentos em um CONE CONCENTRADO na direção do Player
///   (em vez de 360° espalhado), criando uma parede cerrada de tiros muito mais difícil de desviar!
/// </summary>
public class ClusterProjectile : MonoBehaviour
{
    [Header("Configurações do Projétil Principal")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float fuseTime = 1.3f;
    [SerializeField] private int fragmentCount = 8;
    [SerializeField] private GameObject fragmentPrefab;
    [SerializeField] private int fragmentDamage = 1;

    [Header("Dificuldade / Dispersão Fechada")]
    [Tooltip("Abertura do cone de explosão em graus (ex: 65° a 75° cria uma parede fechada de estilhaços).")]
    [SerializeField] private float spreadAngle = 70f;
    [Tooltip("Se verdadeiro, recalcula a mira no Player no momento exato da explosão.")]
    [SerializeField] private bool aimAtPlayerOnExplode = true;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color warningColor = new Color(1f, 0.15f, 0.15f, 1f);

    private Vector2 moveDirection = Vector2.left;
    private Color originalColor = Color.white;
    private bool isLaunched = false;

    public void Launch(Vector2 direction, GameObject customFragmentPrefab = null, int count = -1, float customFuse = -1, float customSpread = -1)
    {
        if (direction != Vector2.zero) moveDirection = direction.normalized;
        if (customFragmentPrefab != null) fragmentPrefab = customFragmentPrefab;
        if (count > 0) fragmentCount = count;
        if (customFuse > 0) fuseTime = customFuse;
        if (customSpread > 0) spreadAngle = customSpread;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        isLaunched = true;
        StartCoroutine(FuseCountdown());
    }

    private void Update()
    {
        if (!isLaunched) return;

        // Deslocamento da bomba
        transform.position += (Vector3)(moveDirection * (speed * Time.deltaTime));

        // Desacelera no ar logo antes de estourar para criar o suspense
        speed = Mathf.MoveTowards(speed, 1.0f, 4.5f * Time.deltaTime);
    }

    private IEnumerator FuseCountdown()
    {
        float elapsed = 0f;

        while (elapsed < fuseTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fuseTime;

            if (spriteRenderer != null)
            {
                // Pisca cada vez mais rápido
                float blinkSpeed = Mathf.Lerp(6f, 35f, progress);
                spriteRenderer.color = (Mathf.PingPong(elapsed * blinkSpeed, 1f) > 0.5f) ? warningColor : originalColor;
            }

            yield return null;
        }

        Explode();
    }

    private void Explode()
    {
        if (fragmentPrefab != null)
        {
            // Determina a direção central do cone de estilhaços
            Vector2 targetDir = moveDirection;

            if (aimAtPlayerOnExplode)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    targetDir = (player.transform.position - transform.position).normalized;
                }
            }

            // Calcula os ângulos dentro do cone fechado
            float baseAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
            float startAngle = baseAngle - (spreadAngle / 2f);
            float angleStep = (fragmentCount > 1) ? spreadAngle / (fragmentCount - 1) : 0f;

            for (int i = 0; i < fragmentCount; i++)
            {
                float currentAngle = (fragmentCount == 1) ? baseAngle : startAngle + (i * angleStep);
                Vector2 dir = Quaternion.Euler(0, 0, currentAngle) * Vector2.right;

                GameObject frag = Instantiate(fragmentPrefab, transform.position, Quaternion.identity);
                frag.SetActive(true);

                // Integração com o Projectile do projeto
                if (frag.TryGetComponent<Projectile>(out var proj))
                {
                    proj.Setup(dir, "Player", fragmentDamage);
                }
                else if (frag.TryGetComponent<SmallProjectile>(out var smallProj))
                {
                    smallProj.Launch(dir);
                }
            }
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<Health>(out var hp))
            {
                hp.TakeDamage(2);
            }
            Explode();
        }
    }
}
