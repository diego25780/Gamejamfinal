using UnityEngine;

/// <summary>
/// Controla a movimentação da nave do jogador em um Shmup horizontal (esquerda para a direita).
/// Mantém a nave travada dentro dos limites visíveis da câmera.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movimentação")]
    [Tooltip("Velocidade de movimento da nave.")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Limites de Tela (Margens)")]
    [Tooltip("Margem interna horizontal para não cortar o sprite nas bordas da tela.")]
    [SerializeField] private float paddingX = 0.6f;
    [Tooltip("Margem interna vertical para não cortar o sprite nas bordas da tela.")]
    [SerializeField] private float paddingY = 0.6f;

    private Camera mainCamera;
    private Vector2 minBounds;
    private Vector2 maxBounds;
    private Vector2 movementInput;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        CalculateCameraBounds();

        if (TryGetComponent<Health>(out var health))
        {
            health.onHealthChanged.AddListener((current, max) =>
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateLives(current, max);
                }
            });

            health.onDeath.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }
            });

            // Envia o valor inicial de vida para a UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateLives(health.CurrentHealth, health.MaxHealth);
            }
        }
    }

    private void Update()
    {
        ProcessInput();
        MovePlayer();
        ClampPositionToBounds();
    }

    /// <summary>
    /// Captura a entrada do teclado (Setas ou WASD).
    /// </summary>
    private void ProcessInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        movementInput = new Vector2(x, y).normalized;
    }

    /// <summary>
    /// Aplica o deslocamento da nave baseado na velocidade e tempo delta.
    /// </summary>
    private void MovePlayer()
    {
        Vector3 delta = (Vector3)(movementInput * moveSpeed * Time.deltaTime);
        transform.position += delta;
    }

    /// <summary>
    /// Calcula os limites do mundo com base no Viewport da câmera ortográfica.
    /// </summary>
    public void CalculateCameraBounds()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            // Converte os cantos da tela (0,0 e 1,1) para coordenadas no mundo
            Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
            Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));

            minBounds = new Vector2(bottomLeft.x + paddingX, bottomLeft.y + paddingY);
            maxBounds = new Vector2(topRight.x - paddingX, topRight.y - paddingY);
        }
    }

    /// <summary>
    /// Trava a posição da nave para não ultrapassar os limites da câmera.
    /// </summary>
    private void ClampPositionToBounds()
    {
        float clampedX = Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(transform.position.y, minBounds.y, maxBounds.y);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}
