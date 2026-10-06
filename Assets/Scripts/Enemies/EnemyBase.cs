using UnityEngine;

/// <summary>
/// Comportamento base de inimigos retrô em side-scroller.
/// Move-se da direita para a esquerda, dispara tiros opcionais,
/// colide com a nave e possui chance de dropar Vida ou Escudo ao morrer.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Movimentação")]
    [Tooltip("Velocidade horizontal em direção à esquerda.")]
    [SerializeField] private float speed = 4f;

    [Tooltip("Habilita movimento ondulado senoidal clássico estilo arcade.")]
    [SerializeField] private bool useWaveMovement = false;
    [SerializeField] private float waveFrequency = 3f;
    [SerializeField] private float waveAmplitude = 1.5f;

    [Header("Colisão & Dano de Contato")]
    [SerializeField] private int contactDamage = 1;

    [Header("Ataque / Disparo Opcional")]
    [SerializeField] private bool canShoot = false;
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootInterval = 1.8f;

    [Header("Pontuação")]
    [SerializeField] private int scoreValue = 100;

    [Header("Drops / Power-ups")]
    [Tooltip("Chance percentual (0 a 100) do inimigo dropar um item ao morrer.")]
    [Range(0, 100)]
    [SerializeField] private float dropChance = 25f;

    [Tooltip("Prefab do item de recuperar vida.")]
    [SerializeField] private GameObject healthPickupPrefab;

    [Tooltip("Prefab do item de escudo.")]
    [SerializeField] private GameObject shieldPickupPrefab;

    [Tooltip("Distribuição entre Vida e Escudo (0.5 = 50% chance de cada).")]
    [Range(0f, 1f)]
    [SerializeField] private float healthDropRatio = 0.5f;

    private float startY;
    private float nextShootTime;
    private Camera mainCamera;

    private void Start()
    {
        startY = transform.position.y;
        mainCamera = Camera.main;
        nextShootTime = Time.time + Random.Range(0.5f, shootInterval);

        if (TryGetComponent<Health>(out var health))
        {
            health.onDeath.AddListener(OnEnemyDeath);
        }
    }

    private void OnEnemyDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }

        TryDropPickup();
    }

    private void TryDropPickup()
    {
        if (Random.Range(0f, 100f) <= dropChance)
        {
            GameObject selectedPrefab = (Random.value <= healthDropRatio) ? healthPickupPrefab : shieldPickupPrefab;

            // Se o sorteado não estiver configurado, usa o que estiver disponível
            if (selectedPrefab == null)
            {
                selectedPrefab = healthPickupPrefab != null ? healthPickupPrefab : shieldPickupPrefab;
            }

            if (selectedPrefab != null)
            {
                Instantiate(selectedPrefab, transform.position, Quaternion.identity);
            }
        }
    }

    private void Update()
    {
        Move();
        HandleShooting();
        CheckOutOfBounds();
    }

    private void Move()
    {
        float xMovement = -speed * Time.deltaTime;
        float newY = transform.position.y;

        if (useWaveMovement)
        {
            newY = startY + Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;
        }

        transform.position = new Vector3(transform.position.x + xMovement, newY, transform.position.z);
    }

    private void HandleShooting()
    {
        if (!canShoot || enemyBulletPrefab == null) return;

        if (Time.time >= nextShootTime)
        {
            Shoot();
            nextShootTime = Time.time + shootInterval;
        }
    }

    private void Shoot()
    {
        Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position;
        GameObject bullet = Instantiate(enemyBulletPrefab, spawnPos, Quaternion.identity);

        if (bullet.TryGetComponent<Projectile>(out var proj))
        {
            proj.Setup(Vector2.left, "Player");
        }
    }

    private void CheckOutOfBounds()
    {
        if (mainCamera == null) return;

        Vector3 screenPos = mainCamera.WorldToViewportPoint(transform.position);

        if (screenPos.x < -0.3f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<Health>(out var playerHealth))
            {
                playerHealth.TakeDamage(contactDamage);
            }

            if (TryGetComponent<Health>(out var myHealth))
            {
                myHealth.TakeDamage(myHealth.MaxHealth);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    public int GetScoreValue() => scoreValue;
}
