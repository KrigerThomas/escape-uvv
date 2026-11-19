using UnityEngine;

// =====================================================================
//  GatoCinza  (Nível 2 - Pátio)
//  Um gato cinza que fica sentado perto dos bancos. Quando o jogador
//  chega perto ele levanta, olha e mia. Apertando E o jogador faz
//  carinho e ele ronrona.
//
//  EASTER EGG: carinho demais na mesma visita (3 vezes) e ele dá uma
//  patada, chia e vai sentar em outro lugar, emburrado.
//
//  COMO USAR:
//   1. Coloque este script num objeto vazio; o modelo do gato (com o
//      Animator usando o "Cat.controller" do Realistic Cat Demo) fica
//      como filho.
//   2. Preencha "Lugares" com objetos vazios onde ele pode sentar.
//  Estados do Animator usados: Sitting_Idle, Idle, Walk e Attack_R.
// =====================================================================
[RequireComponent(typeof(AudioSource))]
public class GatoCinza : MonoBehaviour
{
    [Header("Lugares")]
    [Tooltip("Pontos onde o gato senta. Ele começa no primeiro e muda para o próximo depois da patada. A rotação do ponto vira a direção em que ele fica sentado.")]
    public Transform[] lugares;
    public float velocidadeAndar = 0.8f;
    public float velocidadeGiro = 6f;

    [Header("Interação")]
    public float distanciaInteracao = 2.2f;
    public KeyCode teclaCarinho = KeyCode.E;
    [Tooltip("Quantos carinhos seguidos ele aguenta antes da patada.")]
    public int carinhosAteArranhar = 3;
    [Tooltip("Se o jogador ficar este tempo sem fazer carinho, a paciência volta ao normal.")]
    public float tempoParaEsquecer = 15f;

    [Header("Sons")]
    public AudioClip somMiado;
    public AudioClip somRonronar;
    public AudioClip somChiado;
    [Tooltip("Miado mais grave que o da Misty, para parecer outro gato.")]
    public float tomMiado = 0.8f;

    [Header("Textos")]
    public string textoDica = "[E] Fazer carinho no gato cinza";
    public string[] textosCarinho =
    {
        "Prrrr... o gato cinza ronrona.",
        "Ele fecha os olhos e ronrona mais alto.",
    };
    [TextArea] public string textoPatada = "PSSST! O Cinza tem limite de carinho por noite. Ele foi sentar em outro lugar.";

    private enum Estado { Sentado, Atento, Carinho, Patada, Andando }

    private Estado estado = Estado.Sentado;
    private Animator animator;
    private AudioSource audioSource;
    private Transform jogador;
    private int lugarAtual;
    private int carinhos;
    private float ultimoCarinho = -100f;
    private float timer;

    /// <summary>Pega os componentes, configura o som 3D e senta no primeiro lugar.</summary>
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 18f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        jogador = AvisoNaTela.AcharJogador();

        if (lugares != null && lugares.Length > 0 && lugares[0] != null)
        {
            transform.position = NoChao(lugares[0].position);
            transform.rotation = lugares[0].rotation;
        }

        Sentar();
    }

    /// <summary>Máquina de estados simples do gato.</summary>
    void Update()
    {
        bool perto = jogador != null
            && AvisoNaTela.DistanciaPlana(transform.position, jogador.position) < distanciaInteracao;

        if (Time.time - ultimoCarinho > tempoParaEsquecer)
            carinhos = 0;

        switch (estado)
        {
            case Estado.Sentado:
                if (perto)
                {
                    estado = Estado.Atento;
                    TrocarAnimacao("Idle");
                    Miar();
                    AvisoNaTela.Mostrar(textoDica);
                }
                break;

            case Estado.Atento:
                OlharPara(jogador.position);
                if (!perto)
                {
                    Sentar();
                }
                else if (Input.GetKeyDown(teclaCarinho) && !UIManager.MenuAberto)
                {
                    FazerCarinho();
                }
                break;

            case Estado.Carinho:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    estado = Estado.Atento;
                    TrocarAnimacao("Idle");
                }
                else if (Input.GetKeyDown(teclaCarinho) && perto && !UIManager.MenuAberto)
                {
                    FazerCarinho();
                }
                break;

            case Estado.Patada:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                    IrParaProximoLugar();
                break;

            case Estado.Andando:
                Andar();
                break;
        }
    }

    /// <summary>Carinho: ronrona; no último carinho permitido, dá a patada.</summary>
    private void FazerCarinho()
    {
        carinhos++;
        ultimoCarinho = Time.time;

        if (carinhos >= carinhosAteArranhar)
        {
            DarPatada();
            return;
        }

        estado = Estado.Carinho;
        TrocarAnimacao("Sitting_Idle");

        if (somRonronar != null)
        {
            audioSource.Stop();
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(somRonronar, 0.9f);
        }

        if (textosCarinho != null && textosCarinho.Length > 0)
            AvisoNaTela.Mostrar(textosCarinho[Mathf.Min(carinhos - 1, textosCarinho.Length - 1)]);

        timer = somRonronar != null ? somRonronar.length : 2.5f;
    }

    /// <summary>Easter egg: patada, chiado e aviso. Depois ele muda de lugar.</summary>
    private void DarPatada()
    {
        carinhos = 0;
        estado = Estado.Patada;
        timer = 1.2f;

        if (jogador != null)
            OlharPara(jogador.position, true);

        TrocarAnimacao("Attack_R");

        audioSource.Stop();
        if (somChiado != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(somChiado);
        }

        AvisoNaTela.Mostrar(textoPatada);
    }

    /// <summary>Escolhe o próximo lugar da lista e começa a andar até ele.</summary>
    private void IrParaProximoLugar()
    {
        if (lugares == null || lugares.Length < 2)
        {
            Sentar();
            return;
        }

        lugarAtual = (lugarAtual + 1) % lugares.Length;
        estado = Estado.Andando;
        TrocarAnimacao("Walk");
    }

    /// <summary>Anda até o lugar atual e senta quando chega.</summary>
    private void Andar()
    {
        Transform lugar = lugares[lugarAtual];
        if (lugar == null)
        {
            Sentar();
            return;
        }

        Vector3 alvo = NoChao(lugar.position);
        Vector3 novaPosicao = Vector3.MoveTowards(transform.position, alvo, velocidadeAndar * Time.deltaTime);
        transform.position = NoChao(novaPosicao);
        OlharPara(alvo);

        if (AvisoNaTela.DistanciaPlana(transform.position, alvo) < 0.05f)
        {
            transform.rotation = lugar.rotation;
            Sentar();
        }
    }

    /// <summary>Volta para a pose sentada.</summary>
    private void Sentar()
    {
        estado = Estado.Sentado;
        TrocarAnimacao("Sitting_Idle");
    }

    /// <summary>Toca o miado com o tom deste gato.</summary>
    private void Miar()
    {
        if (somMiado == null)
            return;

        audioSource.pitch = tomMiado * Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(somMiado);
    }

    /// <summary>Gira só no eixo Y para olhar um ponto.</summary>
    private void OlharPara(Vector3 alvo, bool instantaneo = false)
    {
        Vector3 direcao = alvo - transform.position;
        direcao.y = 0f;
        if (direcao.sqrMagnitude < 0.0001f)
            return;

        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcao);
        transform.rotation = instantaneo
            ? rotacaoAlvo
            : Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeGiro * Time.deltaTime);
    }

    /// <summary>Mesmo ponto, com o Y no chão do terreno.</summary>
    private Vector3 NoChao(Vector3 ponto)
    {
        ponto.y = AvisoNaTela.AlturaDoChao(ponto);
        return ponto;
    }

    /// <summary>Troca o estado do Animator com uma transição suave.</summary>
    private void TrocarAnimacao(string nomeEstado)
    {
        if (animator != null)
            animator.CrossFadeInFixedTime(nomeEstado, 0.2f);
    }

    /// <summary>Desenha os lugares e o raio de interação na Scene View.</summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.7f, 0.7f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, distanciaInteracao);
        if (lugares == null) return;
        foreach (Transform lugar in lugares)
            if (lugar != null) Gizmos.DrawWireCube(lugar.position + Vector3.up * 0.15f, new Vector3(0.4f, 0.3f, 0.4f));
    }
}
