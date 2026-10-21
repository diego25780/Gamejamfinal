using System.Collections;
using UnityEngine;

/// <summary>
/// Controla o Chefe (Boss) do jogo retrô:
/// - Surge da direita até a posição de combate na tela.
/// - Patrulha verticalmente subindo e descendo.
/// - Alterna entre múltiplos ataques:
///   1. Tiro em leque (Spread Shot com 3 projéteis)
///   2. Rajada rápida (Burst Shot com 4 tiros)
///   3. Bomba de Fragmentação Focada (Cluster Shotgun concentrado na nave do Player!)
/// - Ao morrer, dropa recompensas lendárias exclusivas (Criogênico, Perfurante, Dash, etc.)
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

    [Header("Ataques Básicos")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform[] firePoints;
    [SerializeField] private float attackCooldown = 2.5f;

    [Header("Ataque Especial: Bomba de Fragmentação Concentrada")]
    [Tooltip("Prefab da bomba de fragmentação (opcional: se vazio, o Boss gera automaticamente).")]
    [SerializeField] private GameObject clusterBombPrefab;
    [Tooltip("Quantidade de pequenos projéteis gerados na explosão.")]
    [SerializeField] private int clusterFragmentCount = 8;
    [Tooltip("Tempo em segundos até a bomba explodir.")]
    [SerializeField] private float clusterFuseTime = 1.3f;
    [Tooltip("Abertura do cone de estilhaços em graus (ex: 70° cria uma parede fechada de tiros na direção do Player).")]
    [SerializeField] private float clusterSpreadAngle = 70f;

    [Header("Recompensas")]
    [SerializeField] private int scoreReward = 2000;
    [SerializeField] private GameObject deathExplosionPrefab;

    [Header("Drops Especiais do Chefe (Exclusivos do Boss)")]
    [Tooltip("Prefab do item de Tiro Criogênico (Exclusivo Boss).")]
    [SerializeField] private GameObject cryoShotPickupPrefab;

    [Tooltip("Prefab do item de Tiro Perfurante (Exclusivo Boss).")]
    [SerializeField] private GameObject piercingShotPickupPrefab;

    [Tooltip("Prefab do item de Tiro Extra (Exclusivo Boss).")]
    [SerializeField] private GameObject extraShotPickupPrefab;

    [Tooltip("Prefab do item de Aumento de Dano (Exclusivo Boss).")]
    [SerializeField] private GameObject damageBoostPickupPrefab;

    [Tooltip("Prefab do item de Dash (Raro).")]
    [SerializeField] private GameObject dashPickupPrefab;

    [Header("Pesos de Probabilidade dos Drops do Boss (0 = Desativado)")]
    [Tooltip("Peso de chance do Tiro Criogênico.")]
    [SerializeField] private float cryoWeight = 25f;

    [Tooltip("Peso de chance do Tiro Perfurante.")]
    [SerializeField] private float piercingWeight = 25f;

    [Tooltip("Peso de chance do Tiro Extra.")]
    [SerializeField] private float extraShotWeight = 20f;

    [Tooltip("Peso de chance do Aumento de Dano.")]
    [SerializeField] private float damageBoostWeight = 20f;

    [Tooltip("Peso de chance do Dash.")]
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

        switch (attackPatternIndex)
        {
            case 0:
                // Ataque 1: Tiro em leque (3 projéteis)
                StartCoroutine(SpreadShotRoutine());
                attackPatternIndex = 1;
                nextAttackTime = Time.time + attackCooldown;
                break;

            case 1:
                // Ataque 2: Rajada rápida (4 projéteis em linha)
                StartCoroutine(BurstShotRoutine());
                attackPatternIndex = 2;
                nextAttackTime = Time.time + attackCooldown + 0.4f;
                break;

            case 2:
                // Ataque 3: Bomba de fragmentação concentrada na direção do Player!
                StartCoroutine(ClusterBombRoutine());
                attackPatternIndex = 0;
                nextAttackTime = Time.time + attackCooldown + 1.0f;
                break;
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

    private IEnumerator ClusterBombRoutine()
    {
        yield return new WaitForSeconds(0.35f);

        Vector3 spawnPos = GetFirePosition();

        Vector2 targetDir = Vector2.left;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            targetDir = (playerObj.transform.position - spawnPos).normalized;
        }

        GameObject bombObj;
        if (clusterBombPrefab != null)
        {
            bombObj = Instantiate(clusterBombPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            bombObj = CreateDefaultClusterBomb(spawnPos);
        }

        if (bombObj != null)
        {
            bombObj.SetActive(true);
            if (bombObj.TryGetComponent<ClusterProjectile>(out var cluster))
            {
                cluster.Launch(targetDir, bulletPrefab, clusterFragmentCount, clusterFuseTime, clusterSpreadAngle);
            }
        }
    }

    private GameObject CreateDefaultClusterBomb(Vector3 position)
    {
        GameObject bomb = new GameObject("ClusterBomb_Active");
        bomb.transform.position = position;
        bomb.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        var rend = bomb.AddComponent<SpriteRenderer>();
        rend.sprite = CreateProceduralCircle();
        rend.color = new Color(1f, 0.35f, 0.1f);
        rend.sortingOrder = 10;

        var col = bomb.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        bomb.AddComponent<ClusterProjectile>();
        return bomb;
    }

    private static Sprite CreateProceduralCircle()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;
        float radius = size * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, dist <= radius ? white : transparent);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
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

    [Header("Dano de Contato do Boss")]
    [Tooltip("Dano causado ao jogador caso ele colida diretamente com o Boss.")]
    [SerializeField] private int bossContactDamage = 1;
    [SerializeField] private float contactDamageCooldown = 0.8f;
    private float nextContactDamageTime = 0f;

    private void OnTriggerEnter2D(Collider2D collision) => HandleBossPlayerContact(collision.gameObject);
    private void OnTriggerStay2D(Collider2D collision) => HandleBossPlayerContact(collision.gameObject);
    private void OnCollisionEnter2D(Collision2D collision) => HandleBossPlayerContact(collision.gameObject);
    private void OnCollisionStay2D(Collision2D collision) => HandleBossPlayerContact(collision.gameObject);

    private void HandleBossPlayerContact(GameObject target)
    {
        if (currentState == BossState.Defeated || target == null) return;
        if (Time.time < nextContactDamageTime) return;

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
                nextContactDamageTime = Time.time + contactDamageCooldown;
                playerHealth.TakeDamage(bossContactDamage > 0 ? bossContactDamage : 1);
            }
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

        DropBossReward();
        Destroy(gameObject);
    }

    /// <summary>
    /// Sorteia e dropa a recompensa do Boss com pesos configuráveis no Inspector.
    /// Criogênico e Perfurante são exclusivos do Boss!
    /// </summary>
    private void DropBossReward()
    {
        float totalWeight = cryoWeight + piercingWeight + dashWeight + damageBoostWeight + extraShotWeight;
        if (totalWeight <= 0f) return;

        float roll = Random.Range(0f, totalWeight);
        PickupItem.PickupType selectedType;

        if (roll < cryoWeight)
        {
            selectedType = PickupItem.PickupType.CryoShot;
        }
        else if (roll < cryoWeight + piercingWeight)
        {
            selectedType = PickupItem.PickupType.PiercingShot;
        }
        else if (roll < cryoWeight + piercingWeight + dashWeight)
        {
            selectedType = PickupItem.PickupType.Dash;
        }
        else if (roll < cryoWeight + piercingWeight + dashWeight + damageBoostWeight)
        {
            selectedType = PickupItem.PickupType.DamageBoost;
        }
        else
        {
            selectedType = PickupItem.PickupType.ExtraShot;
        }

        SpawnBossPickup(selectedType);
    }

    private void SpawnBossPickup(PickupItem.PickupType type)
    {
        // 1. Tenta usar o prefab específico configurado no Inspector
        GameObject selectedPrefab = null;
        switch (type)
        {
            case PickupItem.PickupType.CryoShot:
                selectedPrefab = cryoShotPickupPrefab;
                break;
            case PickupItem.PickupType.PiercingShot:
                selectedPrefab = piercingShotPickupPrefab;
                break;
            case PickupItem.PickupType.Dash:
                selectedPrefab = dashPickupPrefab;
                break;
            case PickupItem.PickupType.DamageBoost:
                selectedPrefab = damageBoostPickupPrefab;
                break;
            case PickupItem.PickupType.ExtraShot:
                selectedPrefab = extraShotPickupPrefab;
                break;
        }

        // 2. Se o prefab específico estiver vazio, usa qualquer prefab de item existente como template
        if (selectedPrefab == null)
        {
            selectedPrefab = dashPickupPrefab != null ? dashPickupPrefab :
                           (extraShotPickupPrefab != null ? extraShotPickupPrefab : damageBoostPickupPrefab);
        }

        if (selectedPrefab != null)
        {
            GameObject itemObj = Instantiate(selectedPrefab, transform.position, Quaternion.identity);
            if (itemObj.TryGetComponent<PickupItem>(out var pickup))
            {
                pickup.SetPickupType(type);
            }
        }
    }
}
