using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Professora do Nível 5. Anda pela sala com um NavMeshAgent seguindo os pontos de ronda
/// e, quando vê o calouro, corre atrás dele. Se encostar no calouro, o nível reinicia.
///
/// Estados:
///  - Ronda: vai de ponto em ponto, esperando um pouco em cada um.
///  - Perseguindo: corre até o jogador. Se perder o jogador de vista por alguns
///    segundos, desiste e volta para a ronda.
///
/// Como ela "vê": o jogador precisa estar perto, na frente dela (dentro do ângulo de visão)
/// e sem parede ou armário no meio (Raycast). Dentro de um Esconderijo ela não vê ninguém.
/// Ela também "ouve" o jogador correndo (Shift) bem perto, mesmo de costas.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Professora : MonoBehaviour
{
    /// <summary>O que a professora está fazendo agora.</summary>
    public enum Estado { Ronda, Perseguindo }

    [Header("Ronda")]
    [Tooltip("Pontos por onde ela passa, em ordem. Depois do último, volta ao primeiro.")]
    public Transform[] pontosRonda;

    [Tooltip("Velocidade andando na ronda (metros por segundo).")]
    public float velocidadeRonda = 1.8f;

    [Tooltip("Segundos parada em cada ponto, olhando a sala.")]
    public float esperaEmCadaPonto = 1.5f;

    [Header("Visão")]
    [Tooltip("Até que distância ela enxerga o jogador.")]
    public float distanciaVisao = 6.5f;

    [Tooltip("Ângulo total do campo de visão, em graus (90 = 45 para cada lado).")]
    [Range(10f, 360f)] public float anguloVisao = 110f;

    [Tooltip("Altura dos olhos, de onde sai o raio que testa se há algo no meio.")]
    public float alturaOlhos = 1.7f;

    [Header("Perseguição")]
    [Tooltip("Velocidade correndo atrás do jogador. Deixe menor que a corrida do jogador para dar para fugir.")]
    public float velocidadePerseguicao = 4.2f;

    [Tooltip("Segundos sem ver o jogador até ela desistir e voltar para a ronda.")]
    public float tempoParaDesistir = 3f;

    [Tooltip("Distância em que ela alcança o jogador.")]
    public float distanciaPegar = 1.0f;

    [Tooltip("Raio em que ela ouve o jogador correndo, mesmo de costas. Use 0 para desligar.")]
    public float raioAudicaoCorrida = 3f;

    [Tooltip("Quando a porta destrancar, ela percebe e vai atrás do jogador.")]
    public bool perseguirQuandoPortaAbrir = true;

    [Header("Sons (opcionais)")]
    [Tooltip("Som quando ela vê o jogador.")]
    public AudioClip somAvistou;

    [Tooltip("Som quando ela pega o jogador.")]
    public AudioClip somPegou;

    [Tooltip("Música que toca só durante a perseguição.")]
    public AudioSource musicaPerseguicao;

    [Header("Textos")]
    public string textoAvistou = "A professora te viu! Corra!";
    public string textoDesistiu = "Ufa... ela te perdeu de vista.";
    public string textoPegou = "A professora te pegou! Voltando para o começo da sala...";
    public string textoPortaAbriu = "A porta abriu, mas a professora ouviu o barulho! Corra para a saída!";

    [Tooltip("Segundos entre ser pego e o nível reiniciar.")]
    public float tempoAntesDeReiniciar = 2.5f;

    /// <summary>Estado atual (aparece no Inspector em modo Debug).</summary>
    public Estado EstadoAtual { get; private set; }

    private NavMeshAgent agente;
    private Animator anim;
    private Transform jogador;
    private PlayerController controleJogador;
    private int indicePonto;
    private float esperaRestante;
    private float tempoSemVer;
    private Vector3 ultimaPosicaoVista;
    private bool pegouJogador;
    private bool portaJaAbriu;

    /// <summary>Pega os componentes e encontra o jogador pela tag Player.</summary>
    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
            anim.applyRootMotion = false;

        GameObject objJogador = GameObject.FindGameObjectWithTag("Player");
        if (objJogador != null)
        {
            jogador = objJogador.transform;
            controleJogador = objJogador.GetComponent<PlayerController>();
        }

        VoltarParaRonda(false);
    }

    /// <summary>Roda a cada quadro: decide o estado, move a professora e confere se pegou o jogador.</summary>
    void Update()
    {
        if (pegouJogador || jogador == null)
            return;

        ChecarPortaAberta();

        bool ve = VeJogador();

        if (EstadoAtual == Estado.Ronda)
        {
            FazerRonda();
            if (ve)
                ComecarPerseguicao(textoAvistou);
        }
        else
        {
            Perseguir(ve);
        }

        if (DistanciaAteJogador() <= distanciaPegar)
            PegarJogador();

        AtualizarAnimacao();
    }

    /// <summary>Anda até o ponto de ronda atual; ao chegar, espera e segue para o próximo.</summary>
    private void FazerRonda()
    {
        if (pontosRonda == null || pontosRonda.Length == 0)
            return;

        if (agente.pathPending || agente.remainingDistance > agente.stoppingDistance + 0.1f)
            return;

        esperaRestante -= Time.deltaTime;
        if (esperaRestante > 0f)
            return;

        indicePonto = (indicePonto + 1) % pontosRonda.Length;
        agente.SetDestination(pontosRonda[indicePonto].position);
        esperaRestante = esperaEmCadaPonto;
    }

    /// <summary>
    /// Corre até o jogador enquanto vê; sem ver, vai até o último lugar onde o viu.
    /// Depois de tempoParaDesistir segundos sem ver, volta para a ronda.
    /// </summary>
    /// <param name="ve">Se o jogador está visível neste quadro.</param>
    private void Perseguir(bool ve)
    {
        if (ve)
        {
            tempoSemVer = 0f;
            ultimaPosicaoVista = jogador.position;
        }
        else
        {
            tempoSemVer += Time.deltaTime;
        }

        agente.SetDestination(ultimaPosicaoVista);

        if (tempoSemVer >= tempoParaDesistir)
            VoltarParaRonda(true);
    }

    /// <summary>Muda para o estado de perseguição, acelera e toca o som de alerta.</summary>
    /// <param name="aviso">Mensagem mostrada na tela.</param>
    public void ComecarPerseguicao(string aviso)
    {
        EstadoAtual = Estado.Perseguindo;
        agente.speed = velocidadePerseguicao;
        tempoSemVer = 0f;
        ultimaPosicaoVista = jogador.position;

        if (somAvistou != null)
            AudioSource.PlayClipAtPoint(somAvistou, transform.position);

        if (musicaPerseguicao != null && !musicaPerseguicao.isPlaying)
            musicaPerseguicao.Play();

        MostrarAviso(aviso);
    }

    /// <summary>Volta a andar devagar pelos pontos de ronda, começando pelo mais próximo.</summary>
    /// <param name="avisar">Se deve mostrar o aviso de que ela desistiu.</param>
    private void VoltarParaRonda(bool avisar)
    {
        EstadoAtual = Estado.Ronda;
        agente.speed = velocidadeRonda;
        esperaRestante = esperaEmCadaPonto;

        if (musicaPerseguicao != null)
            musicaPerseguicao.Stop();

        if (pontosRonda != null && pontosRonda.Length > 0)
        {
            indicePonto = PontoMaisProximo();
            agente.SetDestination(pontosRonda[indicePonto].position);
        }

        if (avisar)
            MostrarAviso(textoDesistiu);
    }

    /// <summary>
    /// Testa se ela enxerga o jogador: distância, ângulo e nada no meio do caminho.
    /// Também "ouve" o jogador correndo perto dela.
    /// </summary>
    /// <returns>Verdadeiro se o jogador foi visto (ou ouvido).</returns>
    private bool VeJogador()
    {
        if (Esconderijo.JogadorEscondido)
            return false;

        float distancia = DistanciaAteJogador();

        bool correndo = controleJogador != null && controleJogador.VelocidadeAtual > controleJogador.velocidadeAndar + 0.1f;
        if (correndo && distancia <= raioAudicaoCorrida)
            return true;

        if (distancia > distanciaVisao)
            return false;

        Vector3 direcao = jogador.position - transform.position;
        direcao.y = 0f;
        if (Vector3.Angle(transform.forward, direcao) > anguloVisao * 0.5f)
            return false;

        Vector3 olhos = transform.position + Vector3.up * alturaOlhos;
        Vector3 alvo = jogador.position + Vector3.up * 1f;

        if (Physics.Linecast(olhos, alvo, out RaycastHit batida, ~0, QueryTriggerInteraction.Ignore))
            return batida.transform == jogador || batida.transform.IsChildOf(jogador);

        return true;
    }

    /// <summary>Se a porta acabou de destrancar, ela vai atrás do jogador (uma vez só).</summary>
    private void ChecarPortaAberta()
    {
        if (!perseguirQuandoPortaAbrir || portaJaAbriu || GameManager.Instancia == null)
            return;

        if (GameManager.Instancia.PortaDestrancada)
        {
            portaJaAbriu = true;
            ComecarPerseguicao(textoPortaAbriu);
        }
    }

    /// <summary>Para tudo, trava o jogador, avisa na tela e reinicia o nível depois de um tempo.</summary>
    private void PegarJogador()
    {
        pegouJogador = true;
        agente.isStopped = true;

        if (musicaPerseguicao != null)
            musicaPerseguicao.Stop();

        if (somPegou != null)
            AudioSource.PlayClipAtPoint(somPegou, transform.position);

        if (controleJogador != null)
        {
            controleJogador.enabled = false;
            Rigidbody rb = controleJogador.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = Vector3.zero;

            Animator animJogador = controleJogador.GetComponent<Animator>();
            if (animJogador != null)
                animJogador.SetFloat("Velocidade", 0f);
        }

        Vector3 olharPara = jogador.position;
        olharPara.y = transform.position.y;
        transform.LookAt(olharPara);

        MostrarAviso(textoPegou);
        Invoke(nameof(Reiniciar), tempoAntesDeReiniciar);
    }

    /// <summary>Reinicia o nível pelo GameManager.</summary>
    private void Reiniciar()
    {
        if (GameManager.Instancia != null)
            GameManager.Instancia.ReiniciarNivel();
    }

    /// <summary>Manda a velocidade atual para o Animator (parada, andando ou correndo).</summary>
    private void AtualizarAnimacao()
    {
        if (anim == null)
            return;

        anim.SetFloat("Velocidade", agente.velocity.magnitude, 0.1f, Time.deltaTime);
        anim.SetBool("NoChao", true);
    }

    /// <summary>Distância no chão (sem contar a altura) até o jogador.</summary>
    private float DistanciaAteJogador()
    {
        Vector3 a = transform.position;
        Vector3 b = jogador.position;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    /// <summary>Índice do ponto de ronda mais perto da professora.</summary>
    private int PontoMaisProximo()
    {
        int melhor = 0;
        float menor = float.MaxValue;

        for (int i = 0; i < pontosRonda.Length; i++)
        {
            float d = Vector3.Distance(transform.position, pontosRonda[i].position);
            if (d < menor)
            {
                menor = d;
                melhor = i;
            }
        }

        return melhor;
    }

    /// <summary>Mostra um aviso pela interface do nível, se houver.</summary>
    private void MostrarAviso(string texto)
    {
        if (GameManager.Instancia != null && GameManager.Instancia.ui != null && !string.IsNullOrEmpty(texto))
            GameManager.Instancia.ui.MostrarAviso(texto);
    }

    /// <summary>Desenha na Scene View o alcance da visão (amarelo) e da audição (azul).</summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origem = transform.position + Vector3.up * 0.1f;
        Vector3 esquerda = Quaternion.Euler(0f, -anguloVisao * 0.5f, 0f) * transform.forward;
        Vector3 direita = Quaternion.Euler(0f, anguloVisao * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(origem, origem + esquerda * distanciaVisao);
        Gizmos.DrawLine(origem, origem + direita * distanciaVisao);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, raioAudicaoCorrida);

        if (pontosRonda == null)
            return;

        Gizmos.color = Color.red;
        for (int i = 0; i < pontosRonda.Length; i++)
        {
            if (pontosRonda[i] == null)
                continue;
            Gizmos.DrawWireSphere(pontosRonda[i].position, 0.2f);
            Transform proximo = pontosRonda[(i + 1) % pontosRonda.Length];
            if (proximo != null)
                Gizmos.DrawLine(pontosRonda[i].position, proximo.position);
        }
    }
}
