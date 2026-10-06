using UnityEngine;

/// <summary>
/// Spawna inimigos continuamente além da borda direita da tela.
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

    [Header("Intervalos de Spawn")]
    [Tooltip("Tempo em segundos entre o surgimento de cada inimigo.")]
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
            SpawnEnemy();
            ScheduleNextSpawn();
        }
    }

    private void ScheduleNextSpawn()
    {
        float interval = Mathf.Max(0.4f, spawnRate + Random.Range(-spawnRateVariance, spawnRateVariance));
        nextSpawnTime = Time.time + interval;
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));
        Vector3 bottomRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, 0));

        float spawnX = topRight.x + spawnDistanceOutsideScreen;
        float spawnY = Random.Range(bottomRight.y + verticalPadding, topRight.y - verticalPadding);

        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

        GameObject selectedPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        if (selectedPrefab != null)
        {
            Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
        }
    }

    /// <summary>
    /// Spawna o Chefe e pausa a geração de inimigos comuns.
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
        Instantiate(bossPrefab, bossSpawnPos, Quaternion.identity);
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
