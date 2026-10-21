using System.Collections;
using UnityEngine;

/// <summary>
/// Controla a movimentação da nave do jogador em um Shmup horizontal.
/// Mantém a nave travada nos limites da câmera e gerencia a habilidade de Dash desbloqueável.
/// Suporta câmera lenta / distorção temporal mantendo a velocidade normal da nave.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movimentação Básica")]
    [Tooltip("Velocidade normal de movimento da nave.")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Limites de Tela (Margens)")]
    [SerializeField] private float paddingX = 0.6f;
    [SerializeField] private float paddingY = 0.6f;

    [Header("Habilidade de Dash (Item Raro)")]
    [Tooltip("Indica se o jogador já desbloqueou o Dash.")]
    [SerializeField] private bool hasDash = false;
    [SerializeField] private float dashSpeed = 22f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 1.6f;
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip dashUnlockSound;

    private Camera mainCamera;
    private Vector2 minBounds;
    private Vector2 maxBounds;
    private Vector2 movementInput;
    private Health health;
    private SpriteRenderer spriteRenderer;

    private bool isDashing = false;
    private float nextDashTime;

    public bool HasDash => hasDash;
    public bool IsDashing => isDashing;

    private void Awake()
    {
        mainCamera = Camera.main;
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        CalculateCameraBounds();

        if (health != null)
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

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateLives(health.CurrentHealth, health.MaxHealth);
            }
        }
    }

    private void Update()
    {
        ProcessInput();

        if (hasDash && CanDash())
        {
            StartCoroutine(DashRoutine());
            return;
        }

        if (!isDashing)
        {
            MovePlayer();
            ClampPositionToBounds();
        }
    }

    private void ProcessInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        movementInput = new Vector2(x, y).normalized;
    }

    private bool CanDash()
    {
        bool pressedDash = Input.GetKeyDown(dashKey) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetMouseButtonDown(1);
        return pressedDash && !isDashing && Time.unscaledTime >= nextDashTime;
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.unscaledTime + dashCooldown;

        // Se estiver parado, dá o dash para frente (direita)
        Vector2 dashDir = movementInput != Vector2.zero ? movementInput : Vector2.right;

        if (health != null)
        {
            health.SetInvulnerable(true); // Imunidade durante o dash
        }

        if (dashSound != null)
        {
            AudioSource.PlayClipAtPoint(dashSound, transform.position);
        }

        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f); // Efeito translúcido
        }

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            // Usa unscaledDeltaTime para manter velocidade normal em câmera lenta
            transform.position += (Vector3)(dashDir * (dashSpeed * Time.unscaledDeltaTime));
            ClampPositionToBounds();
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        if (health != null)
        {
            health.SetInvulnerable(false);
        }

        isDashing = false;
    }

    /// <summary>
    /// Desbloqueia ou aprimora o Dash ao coletar o item raro de Boss.
    /// </summary>
    public void UnlockOrUpgradeDash()
    {
        if (!hasDash)
        {
            hasDash = true;
        }
        else
        {
            dashCooldown = Mathf.Max(0.7f, dashCooldown - 0.25f);
            dashSpeed += 2.5f;
        }

        if (dashUnlockSound != null)
        {
            AudioSource.PlayClipAtPoint(dashUnlockSound, transform.position);
        }
    }

    private void MovePlayer()
    {
        // Usa unscaledDeltaTime para o jogador não ser desacelerado durante Distorção Temporal
        Vector3 delta = (Vector3)(movementInput * moveSpeed * Time.unscaledDeltaTime);
        transform.position += delta;
    }

    public void CalculateCameraBounds()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (mainCamera != null)
        {
            Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
            Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));

            minBounds = new Vector2(bottomLeft.x + paddingX, bottomLeft.y + paddingY);
            maxBounds = new Vector2(topRight.x - paddingX, topRight.y - paddingY);
        }
    }

    private void ClampPositionToBounds()
    {
        float clampedX = Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(transform.position.y, minBounds.y, maxBounds.y);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}
