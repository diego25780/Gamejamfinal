using System.Collections;
using UnityEngine;

/// <summary>
/// Controla o escudo de energia da nave do jogador.
/// Absorve impactos sem que a nave perca vida.
/// Possui busca automática de visual, ordenação de camadas (Sorting Order),
/// rotação/pulso contínuo e criação procedural caso o visual não esteja configurado.
/// </summary>
public class PlayerShield : MonoBehaviour
{
    [Header("Configurações do Escudo")]
    [Tooltip("Objeto visual do escudo (ex: sprite de círculo/bolha como filho da nave). Se vazio, busca automaticamente nos filhos ou cria um visual padrão.")]
    [SerializeField] private GameObject shieldVisual;

    [Tooltip("Quantidade de golpes que o escudo suporta antes de se quebrar.")]
    [SerializeField] private int hitsToAbsorb = 1;

    [Header("Efeitos Visuais")]
    [Tooltip("Tamanho do escudo. Diminua este valor (ex: 0.7 ou 0.8) para deixar o escudo menor.")]
    [Range(0.2f, 3.0f)]
    [SerializeField] private float shieldSize = 0.9f;

    [Tooltip("Deslocamento de posição (X e Y) do escudo para centralizá-lo perfeitamente no corpo da nave.")]
    [SerializeField] private Vector2 shieldOffset = Vector2.zero;

    [Tooltip("Velocidade de rotação do escudo.")]
    [SerializeField] private float rotationSpeed = 90f;
    [Tooltip("Efeito de pulsação suave.")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmount = 0.08f;
    [Tooltip("Cor do brilho do escudo quando ativo.")]
    [SerializeField] private Color shieldColor = new Color(0.2f, 0.8f, 1f, 0.75f);
    [Tooltip("Cor quando o escudo toma um golpe e absorve dano.")]
    [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f, 0.9f);

    [Header("Efeitos Sonoros")]
    [SerializeField] private AudioClip shieldActivateSound;
    [SerializeField] private AudioClip shieldBreakSound;
    [SerializeField] private AudioClip shieldHitSound;

    public bool IsShieldActive { get; private set; }
    private int currentHits;

    private SpriteRenderer shieldSpriteRenderer;
    private SpriteRenderer playerSpriteRenderer;
    private Vector3 baseObjectScale = Vector3.one;
    private Coroutine hitFlashCoroutine;

    private void Awake()
    {
        playerSpriteRenderer = GetComponent<SpriteRenderer>();

        // Configura, busca ou cria o visual do escudo
        SetupShieldVisual();

        // Só desativa no início se o escudo não tiver sido ativado por outro script
        if (!IsShieldActive)
        {
            DeactivateShieldVisual();
        }
    }

    private void Start()
    {
        // Se ainda não estiver ativo, garante que o visual comece desativado
        if (!IsShieldActive)
        {
            DeactivateShieldVisual();
        }
    }

    private void Update()
    {
        if (!IsShieldActive || shieldVisual == null || !shieldVisual.activeSelf) return;

        // Animação contínua: rotação suave do escudo
        if (rotationSpeed != 0f)
        {
            shieldVisual.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }

        // Animação contínua: pulsação de energia
        if (enablePulse)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            shieldVisual.transform.localScale = (baseObjectScale * shieldSize) * pulse;
        }
        else
        {
            shieldVisual.transform.localScale = baseObjectScale * shieldSize;
        }
    }

    private void OnValidate()
    {
        if (shieldVisual != null)
        {
            shieldVisual.transform.localScale = baseObjectScale * shieldSize;
            shieldVisual.transform.localPosition = new Vector3(shieldOffset.x, shieldOffset.y, -0.05f);
        }
    }

    /// <summary>
    /// Configura, busca ou cria o objeto visual do escudo de forma segura.
    /// </summary>
    private void SetupShieldVisual()
    {
        // 1. Evita que o usuário tenha arrastado a própria nave do jogador no slot
        if (shieldVisual == gameObject)
        {
            Debug.LogWarning("PlayerShield: O campo 'Shield Visual' estava apontando para o próprio Player! Corrigindo automaticamente...");
            shieldVisual = null;
        }

        // 2. Se o campo estiver vazio, tenta encontrar um filho existente
        if (shieldVisual == null)
        {
            Transform found = transform.Find("Shield") ?? 
                              transform.Find("Escudo") ?? 
                              transform.Find("ShieldVisual") ?? 
                              transform.Find("Shield_Visual");

            if (found != null)
            {
                shieldVisual = found.gameObject;
            }
            else
            {
                // Procura o primeiro filho que possua um SpriteRenderer e não seja o próprio player
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<SpriteRenderer>() != null)
                    {
                        shieldVisual = child.gameObject;
                        break;
                    }
                }
            }
        }

        // 3. Se ainda não existir nenhum visual nos filhos, cria um visual procedural funcional
        if (shieldVisual == null)
        {
            GameObject autoShield = new GameObject("AutoShieldVisual");
            autoShield.transform.SetParent(transform);
            autoShield.transform.localPosition = new Vector3(shieldOffset.x, shieldOffset.y, -0.05f);
            autoShield.transform.localRotation = Quaternion.identity;
            autoShield.transform.localScale = Vector3.one;

            var sr = autoShield.AddComponent<SpriteRenderer>();
            sr.sprite = CreateProceduralShieldSprite();
            sr.color = shieldColor;

            shieldVisual = autoShield;
        }

        // 4. Configura referências do SpriteRenderer do escudo
        if (shieldVisual != null)
        {
            baseObjectScale = shieldVisual.transform.localScale;
            shieldVisual.transform.localScale = baseObjectScale * shieldSize;
            shieldSpriteRenderer = shieldVisual.GetComponent<SpriteRenderer>();

            // Garante que o SpriteRenderer do escudo esteja sempre na frente do sprite do jogador
            if (shieldSpriteRenderer != null)
            {
                if (playerSpriteRenderer != null)
                {
                    shieldSpriteRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
                    shieldSpriteRenderer.sortingOrder = playerSpriteRenderer.sortingOrder + 1;
                }
                else
                {
                    shieldSpriteRenderer.sortingOrder = 10;
                }

                // Garante que a cor esteja visível com alpha
                if (shieldSpriteRenderer.color.a <= 0.05f)
                {
                    shieldSpriteRenderer.color = shieldColor;
                }
            }

            // Garante que o escudo use o deslocamento (offset) configurado para centralizar na nave
            shieldVisual.transform.localPosition = new Vector3(shieldOffset.x, shieldOffset.y, -0.05f);
        }
    }

    /// <summary>
    /// Ativa o escudo com uma quantidade de cargas/bloqueios.
    /// </summary>
    public void ActivateShield(int hits = 1)
    {
        IsShieldActive = true;
        currentHits = Mathf.Max(hitsToAbsorb, hits);

        if (shieldVisual == null)
        {
            SetupShieldVisual();
        }

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(true);

            if (shieldSpriteRenderer != null)
            {
                shieldSpriteRenderer.enabled = true;
                shieldSpriteRenderer.color = shieldColor;
            }

            shieldVisual.transform.localPosition = new Vector3(shieldOffset.x, shieldOffset.y, -0.05f);
            shieldVisual.transform.localScale = baseObjectScale * shieldSize;
        }

        if (shieldActivateSound != null)
        {
            AudioSource.PlayClipAtPoint(shieldActivateSound, transform.position);
        }
    }

    /// <summary>
    /// Chamado pelo Health.cs antes de subtrair vidas.
    /// Retorna true se o escudo absorveu o golpe.
    /// </summary>
    public bool TryAbsorbDamage()
    {
        if (!IsShieldActive) return false;

        currentHits--;

        if (shieldHitSound != null)
        {
            AudioSource.PlayClipAtPoint(shieldHitSound, transform.position);
        }

        if (currentHits <= 0)
        {
            BreakShield();
        }
        else
        {
            // Efeito visual de tremor/flash ao absorver um golpe sem quebrar
            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        return true;
    }

    private IEnumerator HitFlashRoutine()
    {
        if (shieldSpriteRenderer != null)
        {
            shieldSpriteRenderer.color = hitColor;
            yield return new WaitForSeconds(0.12f);
            shieldSpriteRenderer.color = shieldColor;
        }
    }

    private void BreakShield()
    {
        IsShieldActive = false;
        DeactivateShieldVisual();

        if (shieldBreakSound != null)
        {
            AudioSource.PlayClipAtPoint(shieldBreakSound, transform.position);
        }
    }

    private void DeactivateShieldVisual()
    {
        if (shieldVisual != null)
        {
            // Se o shieldVisual for acidentalmente este próprio GameObject, só desativa o renderer
            if (shieldVisual == gameObject)
            {
                if (shieldSpriteRenderer != null) shieldSpriteRenderer.enabled = false;
            }
            else
            {
                shieldVisual.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Cria uma textura procedural de círculo/bolha de energia caso o projeto não possua sprite de escudo configurado.
    /// </summary>
    private Sprite CreateProceduralShieldSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = size / 2f;
        float outerRadius = (size / 2f) - 3f;
        float innerRadius = outerRadius - 14f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= outerRadius && dist >= innerRadius)
                {
                    // Anel brilhante externo
                    float norm = (dist - innerRadius) / (outerRadius - innerRadius);
                    float alpha = Mathf.Sin(norm * Mathf.PI);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else if (dist < innerRadius)
                {
                    // Centro translúcido de energia
                    float fillAlpha = (dist / innerRadius) * 0.3f;
                    texture.SetPixel(x, y, new Color(0.8f, 0.95f, 1f, fillAlpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void OnDrawGizmosSelected()
    {
        // Desenha uma prévia no Scene View do Unity para facilitar a centralização
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Vector3 center = transform.TransformPoint(new Vector3(shieldOffset.x, shieldOffset.y, 0f));
        Gizmos.DrawWireSphere(center, 0.6f * shieldSize);
    }
}
