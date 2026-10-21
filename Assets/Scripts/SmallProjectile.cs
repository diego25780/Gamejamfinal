using UnityEngine;

public class SmallProjectile : MonoBehaviour
{
    [Header("Configurações do Fragmento")]
    [SerializeField] private float speed = 9f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int damage = 1;

    private Vector2 moveDirection;
    private bool isLaunched = false;

    public void Launch(Vector2 direction)
    {
        moveDirection = direction.normalized;
        isLaunched = true;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isLaunched) return;
        transform.Translate(moveDirection * (speed * Time.deltaTime), Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Se você tiver um componente de vida (ex: PlayerHealth), chame aqui:
            // if (collision.TryGetComponent<PlayerHealth>(out var hp)) hp.TakeDamage(damage);
            
            Debug.Log($"Fragmento atingiu o Player causando {damage} de dano!");
            Destroy(gameObject);
        }
    }
}
