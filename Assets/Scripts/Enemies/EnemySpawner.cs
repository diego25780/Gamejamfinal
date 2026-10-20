using UnityEngine;

/// <summary>
/// Spawna inimigos continuamente além da borda direita da tela.
/// Integrado ao DifficultyManager para escalonar vida máxima dos inimigos,
/// intervalo de spawn mais rápido e surgimento de esquadrões (múltiplos inimigos por onda).
/// Permite pausar as ondas normais para dar lugar ao Chefe (Boss) e retomar depois.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Inimigos Comuns")]
    [Tooltip("Lista de prefabs de inimigos comuns.")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Chefe (Boss)")]
    [Tooltip("Prefab do Chefe que surge a cada 5000 pontos.")]
    [SerializeField] private GameObject bossPrefab;

    [Header("Intervalos de Spawn (Base)")]
    [Tooltip("Tempo base em segundos entre o surgimento de cada onda.")]
    [SerializeField] private float spawnRate = 2.0f;
    [Tooltip("Variação aleatória do intervalo de spawn.")]
    [SerializeField] private float spawnRateVariance = 0.5f;

    [Header("Posicionamento")]
    [Tooltip("Distância além da borda direita onde os inimigos irão surgir.")]
    [SerializeField] private float spawnDistanceOutsideScreen = 2f;
    [Tooltip("Margem interna de altura para não spawnar colado no topo/base da tela.")]
    [SerializeField] private float verticalPadding = 0.8f;

    private Camera mainCamera;
    private float nextSpawnTime;
    private bool isSpawningPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (isSpawningPaused) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemyWave();
            ScheduleNextSpawn();
        }
    }

    private void ScheduleNextSpawn()
    {
        float currentRate = spawnRate;
        if (DifficultyManager.Instance != null)
        {
            currentRate = DifficultyManager.Instance.GetScaledSpawnRate(spawnRate);
        }

        float interval = Mathf.Max(0.35f, currentRate + Random.Range(-spawnRateVariance, spawnRateVariance));
        nextSpawnTime = Time.time + interval;
    }

    /// <summary>
    /// Gera uma onda de inimigos, permitindo 1, 2 ou 3 inimigos simultâneos conforme a dificuldade avança.
    /// </summary>
    private void SpawnEnemyWave()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));
        Vector3 bottomRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, 0));

        float spawnX = topRight.x + spawnDistanceOutsideScreen;
        float minY = bottomRight.y + verticalPadding;
        float maxY = topRight.y - verticalPadding;

        // Quantidade de inimigos a spawnar nesta onda (dificuldade adaptativa)
        int enemiesToSpawn = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.GetSpawnCount() : 1;

        // Distribuição vertical para evitar sobreposição quando surgem múltiplos
        float yStep = (maxY - minY) / Mathf.Max(1, enemiesToSpawn + 1);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            float spawnY = (enemiesToSpawn == 1)
                ? Random.Range(minY, maxY)
                : minY + yStep * (i + 1) + Random.Range(-0.25f, 0.25f);

            spawnY = Mathf.Clamp(spawnY, minY, maxY);

            // Leve escalonamento horizontal em formação
            Vector3 spawnPos = new Vector3(spawnX + (i * 0.45f), spawnY, 0f);

            GameObject selectedPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (selectedPrefab != null)
            {
                GameObject enemyObj = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
                ApplyDifficultyModifiers(enemyObj);
            }
        }
    }

    /// <summary>
    /// Aplica bônus de vida e velocidade ao inimigo recém-gerado.
    /// </summary>
    private void ApplyDifficultyModifiers(GameObject enemyObj)
    {
        if (DifficultyManager.Instance == null || enemyObj == null) return;

        // Escala a Vida Máxima do inimigo
        int healthBonus = DifficultyManager.Instance.GetEnemyHealthBonus();
        if (healthBonus > 0 && enemyObj.TryGetComponent<Health>(out var health))
        {
            health.AddMaxHealthBonus(healthBonus);
        }

        // Escala ligeiramente a velocidade
        float speedMultiplier = DifficultyManager.Instance.GetEnemySpeedMultiplier();
        if (speedMultiplier > 1f && enemyObj.TryGetComponent<EnemyBase>(out var enemyBase))
        {
            enemyBase.ApplySpeedMultiplier(speedMultiplier);
        }
    }

    /// <summary>
    /// Spawna o Chefe (Boss) com bônus de vida adaptativa e pausa a geração de inimigos comuns.
    /// </summary>
    public void SpawnBoss()
    {
        PauseSpawning();

        if (bossPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: Nenhum 'Boss Prefab' atribuído no Inspector!");
            return;
        }

        if (mainCamera == null) mainCamera = Camera.main;
        float spawnX = (mainCamera != null ? mainCamera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x : 10f) + 4f;

        Vector3 bossSpawnPos = new Vector3(spawnX, 0f, 0f);
        GameObject bossObj = Instantiate(bossPrefab, bossSpawnPos, Quaternion.identity);

        if (DifficultyManager.Instance != null && bossObj != null)
        {
            if (bossObj.TryGetComponent<Health>(out var bossHealth))
            {
                int bossBonus = DifficultyManager.Instance.GetBossHealthBonus();
                bossHealth.AddMaxHealthBonus(bossBonus);
            }

            DifficultyManager.Instance.RegisterBossEncounter();
        }
    }

    public void PauseSpawning()
    {
        isSpawningPaused = true;
    }

    public void ResumeSpawning()
    {
        isSpawningPaused = false;
        ScheduleNextSpawn();
    }
}
