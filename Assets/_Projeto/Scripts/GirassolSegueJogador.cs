using UnityEngine;

/// <summary>
/// Easter egg: os girassóis do pátio giram devagar para olhar o jogador, como se ele fosse o sol.
/// Um dos girassóis (com "mostrar Aviso" ligado) avisa na tela e toca um som na primeira vez.
/// </summary>
public class GirassolSegueJogador : MonoBehaviour
{
    [Tooltip("Só segue o jogador quando ele está mais perto que isso.")]
    public float distanciaMaxima = 14f;

    [Tooltip("Rapidez do giro (quanto menor, mais 'assustador').")]
    public float velocidadeGiro = 1.2f;

    [Tooltip("Ajuste do ângulo do modelo para a flor ficar de frente para o jogador.")]
    public float correcaoAngulo = 0f;

    [Header("Aviso (só em um dos girassóis)")]
    public bool mostrarAviso = false;
    public float distanciaAviso = 4.5f;
    [TextArea] public string textoAviso = "Os girassóis estão olhando pra você... de noite?!";
    public AudioClip somAviso;
    [Range(0f, 1f)] public float volumeAviso = 0.7f;

    private Transform jogador;
    private Quaternion rotacaoBase;
    private bool avisoMostrado;

    /// <summary>Guarda a rotação original e procura o jogador.</summary>
    void Start()
    {
        rotacaoBase = transform.rotation;
        jogador = AvisoNaTela.AcharJogador();
    }

    /// <summary>Gira só no eixo Y em direção ao jogador (ou volta ao normal quando ele está longe).</summary>
    void Update()
    {
        if (jogador == null)
            return;

        Vector3 direcao = jogador.position - transform.position;
        direcao.y = 0f;
        float distancia = direcao.magnitude;

        Quaternion alvo = rotacaoBase;
        if (distancia < distanciaMaxima && distancia > 0.2f)
            alvo = Quaternion.LookRotation(direcao) * Quaternion.Euler(0f, correcaoAngulo, 0f);

        transform.rotation = Quaternion.Slerp(transform.rotation, alvo, velocidadeGiro * Time.deltaTime);

        if (mostrarAviso && !avisoMostrado && distancia < distanciaAviso)
        {
            avisoMostrado = true;
            AvisoNaTela.Mostrar(textoAviso);
            if (somAviso != null)
                AudioSource.PlayClipAtPoint(somAviso, transform.position, volumeAviso);
        }
    }
}
