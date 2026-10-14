using UnityEngine;

[DisallowMultipleComponent]
public class FundoEspacialParallax : MonoBehaviour
{
    [System.Serializable]
    public class Camada
    {
        public string nome = "Camada";
        public Sprite imagem;

        [Tooltip("Velocidade do movimento para a esquerda")]
        public float velocidadeScroll = 1.5f;

        [Tooltip("Inverte cópias alternadas para eliminar o corte/emenda da repetição")]
        public bool espelharParaEmendaPerfeita = true;

        [Tooltip("Ajuste fino de altura caso queira subir ou descer o fundo")]
        public float ajusteY = 0f;

        [Tooltip("Aumenta a altura para garantir que preencha além das bordas")]
        [Range(1f, 2f)]
        public float multiplicadorAltura = 1.05f;

        public int sortingOrder = -10;
        public string sortingLayer = "Default";

        [HideInInspector] public SpriteRenderer[] renderers;
        [HideInInspector] public float largura;
    }

    [Header("Câmera")]
    [SerializeField] private Camera cameraAlvo;

    [Header("Imagem Única (Rápido)")]
    [SerializeField] private Sprite imagemFundo;
    [SerializeField] private float velocidadeScroll = 1.5f;
    [SerializeField] private bool espelharEmendas = true;
    [SerializeField] private float ajusteY = 0f;

    [Header("Ou Múltiplas Camadas")]
    [SerializeField] private Camada[] camadas;

    private float alturaCamera;
    private float larguraCamera;

    private void Start()
    {
        if (cameraAlvo == null)
        {
            cameraAlvo = Camera.main;
        }

        if ((camadas == null || camadas.Length == 0) && imagemFundo != null)
        {
            camadas = new Camada[1];
            camadas[0] = new Camada
            {
                nome = "Nebulosa",
                imagem = imagemFundo,
                velocidadeScroll = velocidadeScroll,
                espelharParaEmendaPerfeita = espelharEmendas,
                ajusteY = ajusteY,
                sortingOrder = -10
            };
        }

        ConfigurarFundo();
    }

    private void ConfigurarFundo()
    {
        if (camadas == null || camadas.Length == 0) return;

        if (cameraAlvo.orthographic)
        {
            alturaCamera = cameraAlvo.orthographicSize * 2f;
            larguraCamera = alturaCamera * cameraAlvo.aspect;
        }
        else
        {
            alturaCamera = 10f;
            larguraCamera = 16f;
        }

        Vector3 camPos = cameraAlvo.transform.position;

        for (int c = 0; c < camadas.Length; c++)
        {
            Camada camada = camadas[c];
            if (camada.imagem == null) continue;

            // Escala para cobrir 100% da altura da tela (com pequena margem de segurança)
            float escala = (alturaCamera / camada.imagem.bounds.size.y) * camada.multiplicadorAltura;
            camada.largura = camada.imagem.bounds.size.x * escala;

            // Correção automática do Pivot (evita que o fundo fique só na metade de cima da tela)
            float pivotNormY = camada.imagem.pivot.y / camada.imagem.rect.height;
            float correcaoPivotY = (pivotNormY - 0.5f) * (camada.imagem.bounds.size.y * escala);

            int totalCopias = Mathf.CeilToInt(larguraCamera / camada.largura) + 2;
            if (totalCopias < 3) totalCopias = 3;

            camada.renderers = new SpriteRenderer[totalCopias];

            GameObject container = new GameObject($"[Fundo] {camada.nome}");
            container.transform.SetParent(this.transform);

            float inicioX = camPos.x - (camada.largura * (totalCopias / 2));
            float posY = camPos.y + correcaoPivotY + camada.ajusteY;

            for (int i = 0; i < totalCopias; i++)
            {
                GameObject obj = new GameObject($"Parte_{i}");
                obj.transform.SetParent(container.transform);
                obj.transform.localScale = new Vector3(escala, escala, 1f);

                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = camada.imagem;
                sr.sortingLayerName = camada.sortingLayer;
                sr.sortingOrder = camada.sortingOrder;

                // Espelha alternadamente para costura perfeita (A -> B | B <- A)
                if (camada.espelharParaEmendaPerfeita && (i % 2 != 0))
                {
                    sr.flipX = true;
                }

                float posX = inicioX + (i * camada.largura);
                obj.transform.position = new Vector3(posX, posY, transform.position.z);

                camada.renderers[i] = sr;
            }
        }
    }

    private void Update()
    {
        if (cameraAlvo == null || camadas == null) return;

        Vector3 camPos = cameraAlvo.transform.position;
        float limiteEsquerdo = camPos.x - (larguraCamera / 2f);

        for (int c = 0; c < camadas.Length; c++)
        {
            Camada camada = camadas[c];
            if (camada.renderers == null || camada.renderers.Length == 0) continue;

            // Recalcula o Y para seguir a câmera
            float pivotNormY = camada.imagem.pivot.y / camada.imagem.rect.height;
            float correcaoPivotY = (pivotNormY - 0.5f) * (camada.imagem.bounds.size.y * camada.renderers[0].transform.localScale.y);
            float posY = camPos.y + correcaoPivotY + camada.ajusteY;

            for (int i = 0; i < camada.renderers.Length; i++)
            {
                SpriteRenderer sr = camada.renderers[i];
                Transform t = sr.transform;

                // Move para a esquerda
                t.position += Vector3.left * (camada.velocidadeScroll * Time.deltaTime);
                t.position = new Vector3(t.position.x, posY, t.position.z);
            }

            // Reposiciona as partes que saíram da tela para o final da fila
            while (true)
            {
                int indexMaisEsquerda = ObterMenorX(camada.renderers);
                Transform tMaisEsquerda = camada.renderers[indexMaisEsquerda].transform;

                if (tMaisEsquerda.position.x + (camada.largura / 2f) < limiteEsquerdo)
                {
                    int indexMaisDireita = ObterMaiorX(camada.renderers);
                    SpriteRenderer srMaisDireita = camada.renderers[indexMaisDireita];

                    float novoX = srMaisDireita.transform.position.x + camada.largura;
                    tMaisEsquerda.position = new Vector3(novoX, posY, tMaisEsquerda.position.z);

                    // Garante que o flip continue alternando perfeitamente
                    if (camada.espelharParaEmendaPerfeita)
                    {
                        camada.renderers[indexMaisEsquerda].flipX = !srMaisDireita.flipX;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    private int ObterMenorX(SpriteRenderer[] renderers)
    {
        int index = 0;
        float menor = float.MaxValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].transform.position.x < menor)
            {
                menor = renderers[i].transform.position.x;
                index = i;
            }
        }
        return index;
    }

    private int ObterMaiorX(SpriteRenderer[] renderers)
    {
        int index = 0;
        float maior = float.MinValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].transform.position.x > maior)
            {
                maior = renderers[i].transform.position.x;
                index = i;
            }
        }
        return index;
    }
}