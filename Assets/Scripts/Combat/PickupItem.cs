using UnityEngine;

/// <summary>
/// Controla o comportamento de itens coletáveis (Power-ups) que dropam de inimigos ou Chefes.
/// Suporta:
/// - Customização de logos/sprites individuais no Inspector para cada Power-up
/// - Vida e Escudo (Inimigos Comuns)
/// - Distorção Temporal / Câmera Lenta (Inimigos Comuns)
/// - Tiro Criogênico (Exclusivo Boss - Permanente)
/// - Tiro Perfurante (Exclusivo Boss - Permanente)
/// - Tiro Extra e Aumento de Dano (Exclusivo Boss - Permanente)
/// - Dash (Exclusivo Boss - Permanente)
/// </summary>
public class PickupItem : MonoBehaviour
{
    public enum PickupType
    {
        Health,
        Shield,
        ExtraShot,
        DamageBoost,
        Dash,
        CryoShot,
        TimeDilation,
        PiercingShot
    }

    [Header("Tipo de Item")]
    [SerializeField] private PickupType itemType = PickupType.Health;

    [Header("Logos / Sprites dos Power-ups")]
    [Tooltip("Sprite personalizado para Vida.")]
    [SerializeField] private Sprite healthSprite;

    [Tooltip("Sprite personalizado para Escudo.")]
    [SerializeField] private Sprite shieldSprite;

    [Tooltip("Sprite personalizado para Tiro Extra.")]
    [SerializeField] private Sprite extraShotSprite;

    [Tooltip("Sprite personalizado para Aumento de Dano.")]
    [SerializeField] private Sprite damageBoostSprite;

    [Tooltip("Sprite personalizado para Dash.")]
    [SerializeField] private Sprite dashSprite;

    [Tooltip("Sprite personalizado para Tiro Criogênico.")]
    [SerializeField] private Sprite cryoShotSprite;

    [Tooltip("Sprite personalizado para Distorção Temporal (Câmera Lenta).")]
    [SerializeField] private Sprite timeDilationSprite;

    [Tooltip("Sprite personalizado para Tiro Perfurante.")]
    [SerializeField] private Sprite piercingShotSprite;

    [Tooltip("Se verdadeiro, aplica a cor temática sobre o sprite. Se falso, mantém as cores 100% originais da sua imagem.")]
    [SerializeField] private bool tintCustomSprites = false;

    [Header("Atributos dos Efeitos")]
    [Tooltip("Quantidade de vida restaurada (caso seja Health).")]
    [SerializeField] private int healAmount = 1;

    [Tooltip("Dano bônus adicionado (caso seja DamageBoost).")]
    [SerializeField] private int bonusDamage = 1;

    [Tooltip("Aumento de escala visual do tiro (caso seja DamageBoost).")]
    [SerializeField] private float bonusScale = 0.35f;

    [Tooltip("Duração da Distorção Temporal em segundos.")]
    [SerializeField] private float timeDilationDuration = 5f;

    [Header("Movimentação Flutuante")]
    [Tooltip("Velocidade com que o item flutua para a esquerda.")]
    [SerializeField] private float floatSpeed = 2.2f;
    [Tooltip("Oscilação vertical suave.")]
    [SerializeField] private float bobbingFrequency = 3f;
    [SerializeField] private float bobbingAmplitude = 0.4f;

    [Header("Efeitos & Áudio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private GameObject pickupEffectPrefab;

    private float startY;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        startY = transform.position.y;
        mainCamera = Camera.main;

        if (TryGetComponent<Collider2D>(out var col))
        {
            col.isTrigger = true;
        }

        UpdateVisuals();
    }

    public void SetPickupType(PickupType type)
    {
        itemType = type;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        Sprite customSprite = GetSpriteForType(itemType);

        if (customSprite != null)
        {
            spriteRenderer.sprite = customSprite;

            if (!tintCustomSprites)
            {
                spriteRenderer.color = Color.white; // Preserva as cores originais da arte
                return;
            }
        }

        // Tonalidade temática (usada se não houver sprite customizado ou se tintCustomSprites for true)
        switch (itemType)
        {
            case PickupType.Health:
                spriteRenderer.color = new Color(0.2f, 1f, 0.3f, 1f); // Verde vida
                break;
            case PickupType.Shield:
                spriteRenderer.color = new Color(0.2f, 0.7f, 1f, 1f); // Azul escudo
                break;
            case PickupType.ExtraShot:
                spriteRenderer.color = new Color(1f, 0.9f, 0.2f, 1f); // Amarelo
                break;
            case PickupType.DamageBoost:
                spriteRenderer.color = new Color(1f, 0.3f, 0.1f, 1f); // Laranja avermelhado
                break;
            case PickupType.Dash:
                spriteRenderer.color = Color.white;
                break;
            case PickupType.CryoShot:
                spriteRenderer.color = new Color(0.3f, 0.9f, 1f, 1f); // Ciano gelo
                break;
            case PickupType.TimeDilation:
                spriteRenderer.color = new Color(1f, 0.85f, 0.2f, 1f); // Dourado tempo
                break;
            case PickupType.PiercingShot:
                spriteRenderer.color = new Color(0.9f, 0.2f, 1f, 1f); // Violeta/Magenta neon
                break;
        }
    }

    private Sprite GetSpriteForType(PickupType type)
    {
        switch (type)
        {
            case PickupType.Health: return healthSprite;
            case PickupType.Shield: return shieldSprite;
            case PickupType.ExtraShot: return extraShotSprite;
            case PickupType.DamageBoost: return damageBoostSprite;
            case PickupType.Dash: return dashSprite;
            case PickupType.CryoShot: return cryoShotSprite;
            case PickupType.TimeDilation: return timeDilationSprite;
            case PickupType.PiercingShot: return piercingShotSprite;
            default: return null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateVisuals();
    }
#endif

    private float bobbingTimer = 0f;

    private void Update()
    {
        // Se o jogo estiver pausado (Menu de Pause / Time.timeScale == 0), congela o movimento na tela!
        if (Time.timeScale <= 0.0001f || (GameManager.Instance != null && GameManager.Instance.IsPaused))
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        bobbingTimer += dt;

        float x = transform.position.x - (floatSpeed * dt);
        float y = startY + Mathf.Sin(bobbingTimer * bobbingFrequency) * bobbingAmplitude;

        transform.position = new Vector3(x, y, transform.position.z);

        CheckOutOfBounds();
    }

    private void CheckOutOfBounds()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 screenPos = mainCamera.WorldToViewportPoint(transform.position);
        if (screenPos.x < -0.2f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ApplyEffect(collision.gameObject);

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }

    private void ApplyEffect(GameObject player)
    {
        switch (itemType)
        {
            case PickupType.Health:
                if (player.TryGetComponent<Health>(out var health))
                {
                    health.Heal(healAmount);
                }
                break;

            case PickupType.Shield:
                if (player.TryGetComponent<PlayerShield>(out var shield))
                {
                    shield.ActivateShield();
                }
                break;

            case PickupType.ExtraShot:
                if (player.TryGetComponent<PlayerShooter>(out var extraShooter))
                {
                    extraShooter.AddExtraShot();
                }
                break;

            case PickupType.DamageBoost:
                if (player.TryGetComponent<PlayerShooter>(out var damageShooter))
                {
                    damageShooter.AddDamageBoost(bonusDamage, bonusScale);
                }
                break;

            case PickupType.Dash:
                if (player.TryGetComponent<PlayerController>(out var controller))
                {
                    controller.UnlockOrUpgradeDash();
                }
                break;

            case PickupType.CryoShot:
                if (player.TryGetComponent<PlayerShooter>(out var cryoShooter))
                {
                    cryoShooter.ActivateCryo(); // Permanente!
                }
                break;

            case PickupType.TimeDilation:
                EnsureTimeDilationManager();
                TimeDilationManager.Instance.ActivateDilation(timeDilationDuration);
                break;

            case PickupType.PiercingShot:
                if (player.TryGetComponent<PlayerShooter>(out var pierceShooter))
                {
                    pierceShooter.ActivatePiercing(); // Permanente!
                }
                break;
        }
    }

    private void EnsureTimeDilationManager()
    {
        if (TimeDilationManager.Instance == null)
        {
            GameObject mgr = new GameObject("TimeDilationManager");
            mgr.AddComponent<TimeDilationManager>();
        }
    }
}
