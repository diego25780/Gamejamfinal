using UnityEngine;

/// <summary>
/// Comportamento base de inimigos retrô em side-scroller.
/// Move-se da direita para a esquerda em linha reta ou em ondas senoidais,
/// causa dano por colisão com a nave e pode disparar periodicamente.
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

        // Se o inimigo passou bem para a esquerda da tela, destrói sem pontuar
        if (screenPos.x < -0.3f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Se bater diretamente na nave do jogador
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<Health>(out var playerHealth))
            {
                playerHealth.TakeDamage(contactDamage);
            }

            // Destrói o inimigo ao colidir
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
