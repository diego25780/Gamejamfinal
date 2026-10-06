using UnityEngine;

/// <summary>
/// Controla o comportamento de itens coletáveis (Power-ups) que dropam de inimigos ou Chefes.
/// Suporta: Vida, Escudo, Tiro Extra (Multi-shot), Aumento de Dano (Tiros Gigantes) e Dash (Raro).
/// </summary>
public class PickupItem : MonoBehaviour
{
    public enum PickupType
    {
        Health,
        Shield,
        ExtraShot,
        DamageBoost,
        Dash
    }

    [Header("Tipo de Item")]
    [SerializeField] private PickupType itemType = PickupType.Health;

    [Tooltip("Quantidade de vida restaurada (caso seja Health).")]
    [SerializeField] private int healAmount = 1;

    [Tooltip("Dano bônus adicionado (caso seja DamageBoost).")]
    [SerializeField] private int bonusDamage = 1;

    [Tooltip("Aumento de escala visual do tiro (caso seja DamageBoost).")]
    [SerializeField] private float bonusScale = 0.35f;

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

    private void Start()
    {
        startY = transform.position.y;
        mainCamera = Camera.main;

        if (TryGetComponent<Collider2D>(out var col))
        {
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        float x = transform.position.x - (floatSpeed * Time.deltaTime);
        float y = startY + Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;

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
        }
    }
}
