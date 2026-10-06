using System.Collections;
using UnityEngine;

/// <summary>
/// Controla o Chefe (Boss) do jogo retrô:
/// - Surge da direita até a posição de combate na tela.
/// - Patrulha verticalmente subindo e descendo.
/// - Alterna entre múltiplos ataques (Tiro em leque / Spread e Rajada rápida).
/// - Dá pontuação alta ao morrer e solta Power-ups raros (Tiro Extra, Aumento de Dano/Tiro Maior ou Dash).
/// </summary>
[RequireComponent(typeof(Health))]
public class BossController : MonoBehaviour
{
    private enum BossState { Entering, Fighting, Defeated }

    [Header("Fase de Entrada")]
    [Tooltip("Posição horizontal (X) onde o chefe vai parar para lutar.")]
    [SerializeField] private float battleStationX = 6.5f;
    [SerializeField] private float entranceSpeed = 3f;

    [Header("Movimentação de Combate")]
    [SerializeField] private float verticalSpeed = 3f;
    [SerializeField] private float minY = -3.5f;
    [SerializeField] private float maxY = 3.5f;

    [Header("Ataques")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform[] firePoints;
    [SerializeField] private float attackCooldown = 2.5f;

    [Header("Recompensas")]
    [SerializeField] private int scoreReward = 2000;
    [SerializeField] private GameObject deathExplosionPrefab;

    [Header("Drops de Chefe (Power-ups Ponderados)")]
    [Tooltip("Prefab do item de Tiro Extra.")]
    [SerializeField] private GameObject extraShotPickupPrefab;

    [Tooltip("Prefab do item de Aumento de Dano (Tiros Maiores).")]
    [SerializeField] private GameObject damageBoostPickupPrefab;

    [Tooltip("Prefab do item de Dash (Raro).")]
    [SerializeField] private GameObject dashPickupPrefab;

    [Header("Pesos de Probabilidade dos Drops")]
    [Tooltip("Peso de chance do Tiro Extra (ex: 45).")]
    [SerializeField] private float extraShotWeight = 45f;

    [Tooltip("Peso de chance do Aumento de Dano (ex: 45).")]
    [SerializeField] private float damageBoostWeight = 45f;

    [Tooltip("Peso de chance do Dash (ex: 10 - chance bem menor/mais raro).")]
    [SerializeField] private float dashWeight = 10f;

    private BossState currentState = BossState.Entering;
    private bool movingUp = true;
    private Health health;
    private float nextAttackTime;
    private int attackPatternIndex = 0;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void Start()
    {
        if (health != null)
        {
            health.onDeath.AddListener(OnBossDefeated);
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 top = cam.ViewportToWorldPoint(new Vector3(1, 0.85f, 0));
            Vector3 bottom = cam.ViewportToWorldPoint(new Vector3(1, 0.15f, 0));
            maxY = top.y;
            minY = bottom.y;
        }

        nextAttackTime = Time.time + 1.5f;
    }

    private void Update()
    {
        switch (currentState)
        {
            case BossState.Entering:
                HandleEntrance();
                break;

            case BossState.Fighting:
                HandleMovement();
                HandleAttacks();
                break;

            case BossState.Defeated:
                break;
        }
    }

    private void HandleEntrance()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            new Vector3(battleStationX, transform.position.y, transform.position.z),
            entranceSpeed * Time.deltaTime
        );

        if (Mathf.Abs(transform.position.x - battleStationX) <= 0.05f)
        {
            transform.position = new Vector3(battleStationX, transform.position.y, transform.position.z);
            currentState = BossState.Fighting;
            nextAttackTime = Time.time + 1f;
        }
    }

    private void HandleMovement()
    {
        float dir = movingUp ? 1f : -1f;
        transform.Translate(Vector3.up * (dir * verticalSpeed * Time.deltaTime));

        if (transform.position.y >= maxY)
        {
            movingUp = false;
        }
        else if (transform.position.y <= minY)
        {
            movingUp = true;
        }
    }

    private void HandleAttacks()
    {
        if (Time.time < nextAttackTime || bulletPrefab == null) return;

        if (attackPatternIndex == 0)
        {
            StartCoroutine(SpreadShotRoutine());
            attackPatternIndex = 1;
            nextAttackTime = Time.time + attackCooldown;
        }
        else
        {
            StartCoroutine(BurstShotRoutine());
            attackPatternIndex = 0;
            nextAttackTime = Time.time + attackCooldown + 0.5f;
        }
    }

    private IEnumerator SpreadShotRoutine()
    {
        Vector2[] directions = new Vector2[]
        {
            new Vector2(-1f, 0f),
            new Vector2(-1f, 0.35f),
            new Vector2(-1f, -0.35f)
        };

        Vector3 spawnPos = GetFirePosition();

        foreach (Vector2 dir in directions)
        {
            SpawnBullet(spawnPos, dir);
        }

        yield return null;
    }

    private IEnumerator BurstShotRoutine()
    {
        int burstCount = 4;
        float burstDelay = 0.15f;

        for (int i = 0; i < burstCount; i++)
        {
            SpawnBullet(GetFirePosition(), Vector2.left);
            yield return new WaitForSeconds(burstDelay);
        }
    }

    private Vector3 GetFirePosition()
    {
        if (firePoints != null && firePoints.Length > 0 && firePoints[0] != null)
        {
            return firePoints[Random.Range(0, firePoints.Length)].position;
        }
        return transform.position;
    }

    private void SpawnBullet(Vector3 pos, Vector2 dir)
    {
        GameObject bullet = Instantiate(bulletPrefab, pos, Quaternion.identity);
        if (bullet.TryGetComponent<Projectile>(out var proj))
        {
            proj.Setup(dir, "Player");
        }
    }

    private void OnBossDefeated()
    {
        if (currentState == BossState.Defeated) return;
        currentState = BossState.Defeated;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreReward);
            GameManager.Instance.OnBossDefeated();
        }

        if (deathExplosionPrefab != null)
        {
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
        }

        // Dropar recompensa ponderada (Tiro Extra, Dano/Tiro Maior ou Dash)
        DropBossReward();

        Destroy(gameObject);
    }

    /// <summary>
    /// Sorteia um dos Power-ups com base nos pesos (Dash tem chance menor por ser raro).
    /// </summary>
    private void DropBossReward()
    {
        float totalWeight = 0f;
        if (extraShotPickupPrefab != null) totalWeight += extraShotWeight;
        if (damageBoostPickupPrefab != null) totalWeight += damageBoostWeight;
        if (dashPickupPrefab != null) totalWeight += dashWeight;

        if (totalWeight <= 0f) return;

        float roll = Random.Range(0f, totalWeight);
        float currentSum = 0f;
        GameObject selectedPrefab = null;

        if (extraShotPickupPrefab != null)
        {
            currentSum += extraShotWeight;
            if (roll <= currentSum) selectedPrefab = extraShotPickupPrefab;
        }

        if (selectedPrefab == null && damageBoostPickupPrefab != null)
        {
            currentSum += damageBoostWeight;
            if (roll <= currentSum) selectedPrefab = damageBoostPickupPrefab;
        }

        if (selectedPrefab == null && dashPickupPrefab != null)
        {
            selectedPrefab = dashPickupPrefab;
        }

        if (selectedPrefab != null)
        {
            Instantiate(selectedPrefab, transform.position, Quaternion.identity);
        }
    }
}
