using UnityEngine;

/// <summary>
/// Spawna inimigos continuamente além da borda direita da tela,
/// distribuídos em posições verticais aleatórias dentro dos limites da câmera.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Inimigos a Spawnar")]
    [Tooltip("Lista de prefabs de inimigos.")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Intervalos de Spawn")]
    [Tooltip("Tempo em segundos entre o surgimento de cada inimigo.")]
    [SerializeField] private float spawnRate = 2.0f;
    [Tooltip("Variação aleatória do intervalo de spawn.")]
    [SerializeField] private float spawnRateVariance = 0.5f;

    [Header("Posicionamento")]
    [Tooltip("Distância além da borda direita onde o inimigo irá surgir.")]
    [SerializeField] private float spawnDistanceOutsideScreen = 2f;
    [Tooltip("Margem interna de altura para não spawnar colado no topo/base da tela.")]
    [SerializeField] private float verticalPadding = 0.8f;

    private Camera mainCamera;
    private float nextSpawnTime;

    private void Start()
    {
        mainCamera = Camera.main;
        ScheduleNextSpawn();
    }

    private void Update()
    {
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

        // Limites da câmera no mundo
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));
        Vector3 bottomRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, 0));

        float spawnX = topRight.x + spawnDistanceOutsideScreen;
        float spawnY = Random.Range(bottomRight.y + verticalPadding, topRight.y - verticalPadding);

        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

        // Seleciona um inimigo aleatório da lista
        GameObject selectedPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        if (selectedPrefab != null)
        {
            Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
        }
    }
}
