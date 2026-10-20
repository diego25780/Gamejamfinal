using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de Dificuldade Adaptativa para o jogo espacial ("Light of The Galaxy").
/// Escala o desafio conforme a pontuação (Score) do jogador aumenta:
/// - A cada X pontos (scorePerDifficultyLevel), a dificuldade sobe um nível (Tier).
/// - Aumenta a vida máxima dos inimigos comuns (+HP).
/// - Aumenta substancialmente a vida máxima e a resistência do Boss.
/// - Reduz o tempo entre spawns (ondas mais frequentes).
/// - Aumenta a quantidade de inimigos que surgem simultaneamente (duplas e trios).
/// - Aumenta ligeiramente a velocidade dos inimigos para acompanhar os Power-ups do jogador.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    private static DifficultyManager _instance;
    public static DifficultyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DifficultyManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DifficultyManager");
                    _instance = go.AddComponent<DifficultyManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Escalonamento por Pontuação")]
    [Tooltip("A cada quantos pontos de Score a dificuldade sobe um nível.")]
    [SerializeField] private int scorePerDifficultyLevel = 1000;

    [Tooltip("Nível máximo de dificuldade.")]
    [SerializeField] private int maxDifficultyLevel = 15;

    [Header("Vida dos Inimigos Comuns")]
    [Tooltip("Bônus de vida máxima adicionado aos inimigos a cada nível de dificuldade.")]
    [SerializeField] private int enemyHealthBonusPerLevel = 1;

    [Tooltip("Limite máximo de bônus de vida para inimigos comuns.")]
    [SerializeField] private int maxEnemyHealthBonus = 10;

    [Header("Vida do Chefe (Boss)")]
    [Tooltip("Bônus de vida máxima adicionado ao Boss por nível de dificuldade.")]
    [SerializeField] private int bossHealthBonusPerLevel = 15;

    [Tooltip("Bônus adicional cumulativo a cada vez que um Boss é invocado.")]
    [SerializeField] private int bossHealthBonusPerEncounter = 25;

    [Header("Frequência de Spawn (Intervalo entre ondas)")]
    [Tooltip("Quantos segundos são reduzidos do intervalo de spawn a cada nível.")]
    [SerializeField] private float spawnRateReductionPerLevel = 0.12f;

    [Tooltip("Intervalo mínimo de spawn permitido (em segundos) para evitar poluição visual excessiva.")]
    [SerializeField] private float minSpawnRate = 0.6f;

    [Header("Quantidade de Inimigos por Onda")]
    [Tooltip("Chance (0 a 100%) de gerar múltiplos inimigos na mesma onda conforme o nível avança.")]
    [Range(0, 100)]
    [SerializeField] private float multiSpawnChancePerLevel = 15f;

    [Tooltip("Quantidade máxima de inimigos que podem surgir em uma única onda.")]
    [SerializeField] private int maxEnemiesPerSpawn = 3;

    [Header("Velocidade e Agressividade")]
    [Tooltip("Multiplicador extra de velocidade de movimento dos inimigos por nível (ex: 0.03 = +3%).")]
    [SerializeField] private float enemySpeedBonusPerLevel = 0.03f;

    [Tooltip("Multiplicador máximo de velocidade permitido.")]
    [SerializeField] private float maxSpeedMultiplier = 1.4f;

    [Header("Eventos")]
    public UnityEvent<int> onDifficultyLevelUp;

    private int currentDifficultyLevel = 0;
    private int bossEncounterCount = 0;

    public int CurrentDifficultyLevel => currentDifficultyLevel;
    public int BossEncounterCount => bossEncounterCount;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        int score = GameManager.Instance.CurrentScore;
        int targetLevel = Mathf.Clamp(score / Mathf.Max(100, scorePerDifficultyLevel), 0, maxDifficultyLevel);

        if (targetLevel > currentDifficultyLevel)
        {
            currentDifficultyLevel = targetLevel;
            OnLevelUp(currentDifficultyLevel);
        }
    }

    private void OnLevelUp(int newLevel)
    {
        int enemyHpBonus = GetEnemyHealthBonus();
        float currentRate = GetScaledSpawnRate(2.0f);

        Debug.Log($"<color=#ffaa00><b>[Dificuldade Adaptativa]</b></color> Nível {newLevel} alcançado! " +
                  $"Inimigos com +{enemyHpBonus} HP | Spawn mais rápido ({currentRate:F2}s)!");

        onDifficultyLevelUp?.Invoke(newLevel);
    }

    /// <summary>
    /// Retorna o bônus de vida máxima para um inimigo comum baseado no nível atual.
    /// </summary>
    public int GetEnemyHealthBonus()
    {
        int bonus = currentDifficultyLevel * enemyHealthBonusPerLevel;
        return Mathf.Min(bonus, maxEnemyHealthBonus);
    }

    /// <summary>
    /// Retorna o bônus de vida máxima para o Boss baseado no nível e na quantidade de encontros.
    /// </summary>
    public int GetBossHealthBonus()
    {
        int levelBonus = currentDifficultyLevel * bossHealthBonusPerLevel;
        int encounterBonus = bossEncounterCount * bossHealthBonusPerEncounter;
        return levelBonus + encounterBonus;
    }

    /// <summary>
    /// Registra que um Boss foi invocado, aumentando o bônus de encontros futuros.
    /// </summary>
    public void RegisterBossEncounter()
    {
        bossEncounterCount++;
    }

    /// <summary>
    /// Calcula o intervalo de spawn escalonado pela dificuldade atual.
    /// </summary>
    public float GetScaledSpawnRate(float baseSpawnRate)
    {
        float reduction = currentDifficultyLevel * spawnRateReductionPerLevel;
        return Mathf.Max(minSpawnRate, baseSpawnRate - reduction);
    }

    /// <summary>
    /// Determina quantos inimigos devem surgir na onda (1, 2 ou 3) de acordo com o nível atual.
    /// </summary>
    public int GetSpawnCount()
    {
        if (currentDifficultyLevel <= 0 || maxEnemiesPerSpawn <= 1)
        {
            return 1;
        }

        // Chance de spawn duplo/triplo cresce com o nível
        float multiChance = Mathf.Clamp(currentDifficultyLevel * multiSpawnChancePerLevel, 0f, 85f);
        if (Random.Range(0f, 100f) < multiChance)
        {
            if (currentDifficultyLevel >= 4 && maxEnemiesPerSpawn >= 3 && Random.value < 0.4f)
            {
                return 3; // Trio
            }
            return 2; // Dupla
        }

        return 1;
    }

    /// <summary>
    /// Retorna o multiplicador de velocidade para inimigos normais.
    /// </summary>
    public float GetEnemySpeedMultiplier()
    {
        float mult = 1f + (currentDifficultyLevel * enemySpeedBonusPerLevel);
        return Mathf.Min(mult, maxSpeedMultiplier);
    }

    /// <summary>
    /// Reinicia a dificuldade (chamado ao reiniciar a partida).
    /// </summary>
    public void ResetDifficulty()
    {
        currentDifficultyLevel = 0;
        bossEncounterCount = 0;
    }
}
