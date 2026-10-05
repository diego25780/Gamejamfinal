using UnityEngine;

/// <summary>
/// Controla o disparo de armas do jogador (tiro contínuo com cadência).
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [Header("Projétil & Ponto de Disparo")]
    [Tooltip("Prefab do tiro a ser disparado.")]
    [SerializeField] private GameObject laserPrefab;

    [Tooltip("Distância à frente da nave para o tiro sair (caso não use FirePoints).")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0.8f, 0f, 0f);

    [Tooltip("Pontos de saída do tiro (permite arma simples, dupla ou tripla).")]
    [SerializeField] private Transform[] firePoints;

    [Header("Cadência")]
    [Tooltip("Intervalo mínimo em segundos entre cada disparo.")]
    [SerializeField] private float fireRate = 0.2f;

    [Header("Áudio")]
    [Tooltip("Efeito sonoro ao atirar.")]
    [SerializeField] private AudioClip shootSound;

    private float nextFireTime;

    private void Update()
    {
        // Dispara ao segurar a tecla de espaço ou botão de tiro
        if (Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1"))
        {
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    private void Shoot()
    {
        if (laserPrefab == null)
        {
            Debug.LogWarning("PlayerShooter: Nenhum 'laserPrefab' configurado no Inspector!");
            return;
        }

        // Se nenhum ponto de disparo for passado, dispara um pouco à frente da nave
        if (firePoints == null || firePoints.Length == 0)
        {
            InstantiateLaser(transform.position + spawnOffset);
        }
        else
        {
            foreach (Transform point in firePoints)
            {
                if (point != null)
                {
                    InstantiateLaser(point.position);
                }
            }
        }

        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, transform.position);
        }
    }

    private void InstantiateLaser(Vector3 spawnPosition)
    {
        GameObject laser = Instantiate(laserPrefab, spawnPosition, Quaternion.identity);

        if (laser.TryGetComponent<Projectile>(out var proj))
        {
            proj.Setup(Vector2.right, "Enemy");
        }
    }
}
