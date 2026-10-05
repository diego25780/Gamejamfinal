using UnityEngine;

/// <summary>
/// Efeito de rolagem infinita horizontal (Parallax / Starfield).
/// Move o fundo para a esquerda simulando a nave voando em alta velocidade para a direita.
/// Funciona com SpriteRenderer (deslocando a posição e reposicionando ao sair da tela)
/// ou com Material/Quad (rolando Texture Offset).
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Configuração de Rolagem")]
    [Tooltip("Velocidade com que o fundo se desloca para a esquerda.")]
    [SerializeField] private float scrollSpeed = 2f;

    [Tooltip("Largura do sprite para reposicionar quando sair da tela.")]
    [SerializeField] private float spriteWidth = 20f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        // Move o objeto para a esquerda continuamente
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        // Quando o fundo se deslocar uma largura completa para a esquerda, reseta para a posição inicial
        if (transform.position.x <= startPosition.x - spriteWidth)
        {
            transform.position = startPosition;
        }
    }
}
