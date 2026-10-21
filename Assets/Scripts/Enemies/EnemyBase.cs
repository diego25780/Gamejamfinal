using UnityEngine;

/// <summary>
/// Comportamento base de inimigos retrô em side-scroller.
/// Move-se da direita para a esquerda, dispara tiros opcionais,
/// colide com a nave e possui chance de dropar Vida, Escudo ou Power-ups especiais ao morrer.
/// Suporta escalonamento de velocidade e tiros com dificuldade adaptativa.
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
            GameObject templatePrefab = healthPickupPrefab != null ? healthPickupPrefab : shieldPickupPrefab;
            if (templatePrefab == null) return;

            GameObject spawnedItem = Instantiate(templatePrefab, transform.position, Quaternion.identity);

            // 70% de chance de Vida/Escudo e 30% de chance de Distorção Temporal (Câmera Lenta)
            if (spawnedItem.TryGetComponent<PickupItem>(out var pickup))
            {
                float roll = Random.value;
                if (roll < 0.70f)
                {
                    pickup.SetPickupType(Random.value <= healthDropRatio ? PickupItem.PickupType.Health : PickupItem.PickupType.Shield);
                }
                else
                {
                    pickup.SetPickupType(PickupItem.PickupType.TimeDilation);
                }
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

    private void OnTriggerEnter2D(Collider2D collision) => HandlePlayerContact(collision.gameObject);
    private void OnTriggerStay2D(Collider2D collision) => HandlePlayerContact(collision.gameObject);
    private void OnCollisionEnter2D(Collision2D collision) => HandlePlayerContact(collision.gameObject);
    private void OnCollisionStay2D(Collision2D collision) => HandlePlayerContact(collision.gameObject);

    private void HandlePlayerContact(GameObject target)
    {
        if (target == null) return;

        // Detecta se colidiu com o jogador (por tag no objeto, no pai ou raiz)
        bool isPlayer = target.CompareTag("Player") || 
                        (target.transform.root != null && target.transform.root.CompareTag("Player"));

        Health playerHealth = target.GetComponentInParent<Health>();
        if (playerHealth == null && target.transform.root != null)
        {
            playerHealth = target.transform.root.GetComponentInChildren<Health>();
        }

        if (isPlayer || target.GetComponentInParent<PlayerController>() != null)
        {
            if (playerHealth != null)
            {
                int damage = contactDamage > 0 ? contactDamage : 1;
                playerHealth.TakeDamage(damage);
            }

            // Inimigo comum se destrói ao colidir com o jogador
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

    /// <summary>
    /// Aplica multiplicador de velocidade ao inimigo (dificuldade adaptativa).
    /// </summary>
    public void ApplySpeedMultiplier(float multiplier)
    {
        if (multiplier > 0f)
        {
            speed *= multiplier;
        }
    }

    /// <summary>
    /// Aumenta a cadência de disparos do inimigo em dificuldades mais altas.
    /// </summary>
    public void ApplyShootIntervalReduction(float factor)
    {
        shootInterval = Mathf.Max(0.6f, shootInterval * factor);
    }

    public int GetScoreValue() => scoreValue;
}
