using UnityEngine;

/// <summary>
/// Faz uma planta balançar (como uma mola) quando o jogador passa encostando nela,
/// tocando um som baixinho de folhas. O pivô da planta precisa estar na base.
/// </summary>
public class PlantaBalanco : MonoBehaviour
{
    [Tooltip("Distância (em metros) em que o jogador 'encosta' na planta.")]
    public float raio = 0.9f;

    [Tooltip("Inclinação inicial, em graus, quando o jogador encosta.")]
    public float forca = 14f;

    [Tooltip("Rigidez da mola: quanto maior, mais rápido volta.")]
    public float mola = 55f;

    [Tooltip("Quanto o balanço perde força a cada segundo.")]
    public float amortecimento = 4.5f;

    [Header("Som")]
    public AudioClip somFolhas;
    [Range(0f, 1f)] public float volume = 0.35f;
    [Tooltip("Tempo mínimo entre dois sons desta planta.")]
    public float intervaloSom = 1.5f;

    private Transform jogador;
    private Quaternion rotacaoBase;
    private Vector3 inclinacao;   // direção e tamanho (graus) da inclinação, no plano XZ
    private Vector3 velocidade;
    private bool jogadorDentro;
    private float ultimoSom = -10f;

    /// <summary>Guarda a rotação original e procura o jogador.</summary>
    void Start()
    {
        rotacaoBase = transform.rotation;
        jogador = AvisoNaTela.AcharJogador();
    }

    /// <summary>Detecta o encosto do jogador e simula a mola.</summary>
    void Update()
    {
        if (jogador == null)
            return;

        float distancia = AvisoNaTela.DistanciaPlana(transform.position, jogador.position);

        if (distancia < raio && !jogadorDentro)
        {
            jogadorDentro = true;
            Vector3 direcao = transform.position - jogador.position;
            direcao.y = 0f;
            if (direcao.sqrMagnitude < 0.0001f)
                direcao = jogador.forward;

            velocidade += direcao.normalized * forca * mola * 0.15f;

            if (somFolhas != null && Time.time - ultimoSom > intervaloSom)
            {
                ultimoSom = Time.time;
                AudioSource.PlayClipAtPoint(somFolhas, transform.position, volume);
            }
        }
        else if (distancia > raio * 1.3f)
        {
            jogadorDentro = false;
        }

        // Nada balançando: não precisa calcular
        if (inclinacao.sqrMagnitude < 0.0001f && velocidade.sqrMagnitude < 0.0001f)
            return;

        float dt = Time.deltaTime;
        Vector3 aceleracao = -mola * inclinacao - amortecimento * velocidade;
        velocidade += aceleracao * dt;
        inclinacao += velocidade * dt;
        inclinacao = Vector3.ClampMagnitude(inclinacao, 25f);

        if (inclinacao.sqrMagnitude < 0.0001f)
        {
            transform.rotation = rotacaoBase;
            return;
        }

        Vector3 eixo = Vector3.Cross(Vector3.up, inclinacao.normalized);
        transform.rotation = Quaternion.AngleAxis(inclinacao.magnitude, eixo) * rotacaoBase;
    }
}
