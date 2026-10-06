using UnityEngine;

/// <summary>
/// Controla o disparo de armas do jogador.
/// Suporta progressão de tiros extras (multi-shot), aumento de dano e tiros gigantes.
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [Header("Projétil & Ponto de Disparo")]
    [Tooltip("Prefab do tiro a ser disparado.")]
    [SerializeField] private GameObject laserPrefab;

    [Tooltip("Distância à frente da nave para o tiro sair (caso não use FirePoints).")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0.8f, 0f, 0f);

    [Tooltip("Pontos de saída do tiro customizados (opcional).")]
    [SerializeField] private Transform[] firePoints;

    [Header("Cadência")]
    [Tooltip("Intervalo mínimo em segundos entre cada disparo.")]
    [SerializeField] private float fireRate = 0.2f;

    [Header("Áudio")]
    [Tooltip("Efeito sonoro ao atirar.")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip powerUpSound;

    [Header("Upgrades (Tiros Extras e Dano)")]
    [Tooltip("Nível de tiros extras (1 = simples, 2 = duplo, 3 = triplo, etc.).")]
    [SerializeField] private int shotLevel = 1;

    [Tooltip("Bônus de dano adicionado a cada tiro.")]
    [SerializeField] private int damageBonus = 0;

    [Tooltip("Multiplicador da escala visual do tiro (tiros maiores).")]
    [SerializeField] private float bulletScaleMultiplier = 1f;

    private float nextFireTime;

    public int ShotLevel => shotLevel;
    public int DamageBonus => damageBonus;
    public float BulletScaleMultiplier => bulletScaleMultiplier;

    private void Update()
    {
        if (Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1"))
        {
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    private void Shoot()
    {
        if (laserPrefab == null) return;

        Vector3 baseSpawn = transform.position + spawnOffset;

        // Se tiver firePoints definidos manualmente no Inspector, usa eles
        if (firePoints != null && firePoints.Length > 0)
        {
            foreach (Transform point in firePoints)
            {
                if (point != null)
                {
                    InstantiateLaser(point.position, Vector2.right);
                }
            }
        }
        else
        {
            // Sistema dinâmico de tiros extras por nível (Multi-shot estilo arcade retrô)
            switch (shotLevel)
            {
                case 1:
                    // Tiro simples no centro
                    InstantiateLaser(baseSpawn, Vector2.right);
                    break;

                case 2:
                    // Tiro duplo paralelo
                    InstantiateLaser(baseSpawn + new Vector3(0f, 0.25f, 0f), Vector2.right);
                    InstantiateLaser(baseSpawn + new Vector3(0f, -0.25f, 0f), Vector2.right);
                    break;

                case 3:
                    // Tiro triplo em leque (1 reto, 1 inclinado para cima, 1 para baixo)
                    InstantiateLaser(baseSpawn, Vector2.right);
                    InstantiateLaser(baseSpawn + new Vector3(0f, 0.2f, 0f), new Vector2(1f, 0.25f));
                    InstantiateLaser(baseSpawn + new Vector3(0f, -0.2f, 0f), new Vector2(1f, -0.25f));
                    break;

                default: // Nível 4 ou superior
                    // Tiro quádruplo / leque avançado
                    InstantiateLaser(baseSpawn + new Vector3(0f, 0.35f, 0f), new Vector2(1f, 0.35f));
                    InstantiateLaser(baseSpawn + new Vector3(0f, 0.12f, 0f), Vector2.right);
                    InstantiateLaser(baseSpawn + new Vector3(0f, -0.12f, 0f), Vector2.right);
                    InstantiateLaser(baseSpawn + new Vector3(0f, -0.35f, 0f), new Vector2(1f, -0.35f));
                    break;
            }
        }

        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, transform.position);
        }
    }

    private void InstantiateLaser(Vector3 spawnPosition, Vector2 direction)
    {
        GameObject laser = Instantiate(laserPrefab, spawnPosition, Quaternion.identity);

        // Aplica o aumento de escala visual
        laser.transform.localScale *= bulletScaleMultiplier;

        if (laser.TryGetComponent<Projectile>(out var proj))
        {
            int totalDamage = 1 + damageBonus;
            proj.Setup(direction, "Enemy", totalDamage);
        }
    }

    /// <summary>
    /// Adiciona um nível de tiro extra (tiro duplo, triplo, etc.).
    /// </summary>
    public void AddExtraShot()
    {
        shotLevel++;
        PlayPowerUpFeedback();
    }

    /// <summary>
    /// Aumenta o dano e faz o projétil ficar fisicamente maior na tela.
    /// </summary>
    public void AddDamageBoost(int extraDamage = 1, float extraScale = 0.35f)
    {
        damageBonus += extraDamage;
        bulletScaleMultiplier += extraScale;
        PlayPowerUpFeedback();
    }

    private void PlayPowerUpFeedback()
    {
        if (powerUpSound != null)
        {
            AudioSource.PlayClipAtPoint(powerUpSound, transform.position);
        }
    }
}
