using UnityEngine;

/// <summary>
/// Controla o disparo de armas do jogador.
/// Suporta:
/// - Tiros extras (multi-shot) e aumento de dano (Permanentes)
/// - Tiro Criogênico e Tiro Perfurante obtidos do Boss (Permanentes)
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
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip powerUpSound;

    [Header("Upgrades Básicos (Permanentes)")]
    [SerializeField] private int shotLevel = 1;
    [SerializeField] private int damageBonus = 0;
    [SerializeField] private float bulletScaleMultiplier = 1f;

    [Header("Upgrades Lendários do Boss (Permanentes)")]
    [Tooltip("Habilita o Tiro de Gelo que congela/desacelera inimigos para sempre.")]
    [SerializeField] private bool isCryoActive = false;

    [Tooltip("Habilita o Tiro Perfurante que atravessa fileiras de inimigos para sempre.")]
    [SerializeField] private bool isPiercingActive = false;

    private float nextFireTime;

    public int ShotLevel => shotLevel;
    public int DamageBonus => damageBonus;
    public float BulletScaleMultiplier => bulletScaleMultiplier;
    public bool IsCryoActive => isCryoActive;
    public bool IsPiercingActive => isPiercingActive;

    private void Update()
    {
        // Disparo (usa unscaledTime para não ser afetado por câmera lenta)
        if (Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1"))
        {
            if (Time.unscaledTime >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.unscaledTime + fireRate;
            }
        }
    }

    private void Shoot()
    {
        if (laserPrefab == null) return;

        Vector3 baseSpawn = transform.position + spawnOffset;

        if (firePoints != null && firePoints.Length > 0)
        {
            foreach (Transform point in firePoints)
            {
                if (point != null) InstantiateLaser(point.position, Vector2.right);
            }
        }
        else
        {
            switch (shotLevel)
            {
                case 1:
                    InstantiateLaser(baseSpawn, Vector2.right);
                    break;

                case 2:
                    InstantiateLaser(baseSpawn + new Vector3(0f, 0.25f, 0f), Vector2.right);
                    InstantiateLaser(baseSpawn + new Vector3(0f, -0.25f, 0f), Vector2.right);
                    break;

                case 3:
                    InstantiateLaser(baseSpawn, Vector2.right);
                    InstantiateLaser(baseSpawn + new Vector3(0f, 0.2f, 0f), new Vector2(1f, 0.25f));
                    InstantiateLaser(baseSpawn + new Vector3(0f, -0.2f, 0f), new Vector2(1f, -0.25f));
                    break;

                default:
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
        laser.transform.localScale *= bulletScaleMultiplier;

        if (laser.TryGetComponent<Projectile>(out var proj))
        {
            int totalDamage = 1 + damageBonus;
            proj.Setup(direction, "Enemy", totalDamage);
            proj.SetupModifiers(isPiercingActive, 3, isCryoActive);
        }
    }

    // --- MÉTODOS DE ATIVAÇÃO PERMANENTES ---

    /// <summary>
    /// Ativa o Tiro Criogênico de forma PERMANENTE na nave para o resto da partida.
    /// </summary>
    public void ActivateCryo(float duration = -1f)
    {
        isCryoActive = true;
        PlayPowerUpFeedback();
    }

    /// <summary>
    /// Ativa o Tiro Perfurante de forma PERMANENTE na nave para o resto da partida.
    /// </summary>
    public void ActivatePiercing(float duration = -1f)
    {
        isPiercingActive = true;
        PlayPowerUpFeedback();
    }

    /// <summary>
    /// Aumenta o nível de tiro (duplo, triplo, etc.) de forma PERMANENTE.
    /// </summary>
    public void AddExtraShot()
    {
        shotLevel++;
        PlayPowerUpFeedback();
    }

    /// <summary>
    /// Aumenta o dano e a escala visual do tiro de forma PERMANENTE.
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
