using UnityEngine;

/// <summary>
/// Efeito de rolagem infinita horizontal (Parallax / Starfield).
/// Move o fundo simulando a nave voando em alta velocidade.
/// Suporta:
/// 1) Movimento contínuo suave com compensação de frame (sem trancos ou pulos visuais).
/// 2) Detecção automática da largura do sprite (bounds.size.x).
/// 3) Duplicação automática de sprite para não deixar buraco preto na tela.
/// 4) Modo UV Texture Offset (Quad/Material com Wrap Mode = Repeat).
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    public enum ParallaxMode
    {
        SpriteTransform, // Move o Transform do SpriteRenderer
        UVTextureOffset  // Rola o offset da textura do Material (para Quads)
    }

    [Header("Modo de Operação")]
    [Tooltip("SpriteTransform para SpriteRenderers ou UVTextureOffset para Quads/Materiais.")]
    [SerializeField] private ParallaxMode mode = ParallaxMode.SpriteTransform;

    [Header("Velocidade e Direção")]
    [Tooltip("Velocidade do fundo. Diferentes camadas com velocidades diferentes criam o efeito Parallax de profundidade.")]
    [SerializeField] private float scrollSpeed = 2f;

    [Tooltip("Se verdadeiro, rola para a esquerda (padrão de nave voando para a direita).")]
    [SerializeField] private bool scrollLeft = true;

    [Header("Configurações de Sprite (SpriteTransform)")]
    [Tooltip("Calcula a largura automaticamente baseada no SpriteRenderer.")]
    [SerializeField] private bool autoDetectWidth = true;

    [Tooltip("Largura manual caso a auto-detecção esteja desativada ou precise de ajuste fino.")]
    [SerializeField] private float spriteWidth = 20f;

    [Tooltip("Cria automaticamente um clone do sprite adjacente para garantir rolagem contínua sem tela preta.")]
    [SerializeField] private bool autoCreateDuplicate = true;

    private Vector3 startPosition;
    private Renderer targetRenderer;
    private Material targetMaterial;
    private Vector2 uvOffset = Vector2.zero;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null && mode == ParallaxMode.UVTextureOffset)
        {
            targetMaterial = targetRenderer.material;
        }
    }

    private void Start()
    {
        startPosition = transform.position;

        if (mode == ParallaxMode.SpriteTransform)
        {
            SetupSpriteLoop();
        }
    }

    private void SetupSpriteLoop()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && autoDetectWidth && sr.sprite != null)
        {
            spriteWidth = sr.bounds.size.x;
        }

        // Se ativado e ainda não tem filho duplicado, cria uma cópia posicionada lado a lado
        if (autoCreateDuplicate && transform.childCount == 0)
        {
            GameObject clone = new GameObject(gameObject.name + "_Clone");
            clone.transform.SetParent(transform);
            clone.transform.position = transform.position + Vector3.right * spriteWidth;
            clone.transform.rotation = transform.rotation;
            clone.transform.localScale = Vector3.one;

            if (sr != null)
            {
                SpriteRenderer cloneSr = clone.AddComponent<SpriteRenderer>();
                cloneSr.sprite = sr.sprite;
                cloneSr.sortingLayerID = sr.sortingLayerID;
                cloneSr.sortingOrder = sr.sortingOrder;
                cloneSr.color = sr.color;
                cloneSr.material = sr.material;
            }
        }
    }

    private void Update()
    {
        float direction = scrollLeft ? -1f : 1f;

        if (mode == ParallaxMode.SpriteTransform)
        {
            // Move o objeto horizontalmente
            transform.position += Vector3.right * (direction * scrollSpeed * Time.deltaTime);

            // Verifica o loop com compensação de overshoot (evita engasgos na tela)
            if (scrollLeft)
            {
                if (transform.position.x <= startPosition.x - spriteWidth)
                {
                    transform.position += Vector3.right * spriteWidth;
                }
            }
            else
            {
                if (transform.position.x >= startPosition.x + spriteWidth)
                {
                    transform.position -= Vector3.right * spriteWidth;
                }
            }
        }
        else if (mode == ParallaxMode.UVTextureOffset && targetMaterial != null)
        {
            // Deslocamento contínuo da textura
            uvOffset.x += -direction * (scrollSpeed * 0.1f) * Time.deltaTime;
            targetMaterial.mainTextureOffset = uvOffset;
        }
    }

    private void OnDestroy()
    {
        if (mode == ParallaxMode.UVTextureOffset && targetMaterial != null)
        {
            Destroy(targetMaterial);
        }
    }
}
