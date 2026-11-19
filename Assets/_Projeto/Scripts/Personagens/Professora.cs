using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Professora do Nível 5. Anda pelo bloco com um NavMeshAgent seguindo os pontos de ronda,
/// carregando uma lanterna. Quando vê o calouro, corre atrás dele. Se encostar, o nível reinicia.
///
/// Estados:
///  - Ronda: vai de ponto em ponto, esperando um pouco em cada um.
///  - Perseguindo: corre até o jogador. Se perder o jogador de vista por alguns
///    segundos, desiste e volta para a ronda.
///
/// Como ela "vê": o jogador precisa estar perto, na frente dela (dentro do ângulo de visão,
/// o mesmo da lanterna) e sem parede ou móvel alto no meio (Linecast).
/// Ela também ouve o jogador correndo perto, e vê de mais longe se a lanterna do celular estiver ligada.
/// Dentro de um armário (ArmarioEsconderijo) ela não vê nem pega ninguém.
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
    public float velocidadeRonda = 1.6f;

    [Tooltip("Segundos parada em cada ponto, olhando em volta.")]
    public float esperaEmCadaPonto = 2f;

    [Header("Visão")]
    [Tooltip("Até que distância ela enxerga o jogador.")]
    public float distanciaVisao = 7f;

    [Tooltip("Ângulo total do campo de visão, em graus. Deixe parecido com o ângulo da lanterna.")]
    [Range(10f, 360f)] public float anguloVisao = 70f;

    [Tooltip("Multiplica a distância de visão quando a lanterna do celular do jogador está ligada.")]
    public float multiplicadorLanternaJogador = 1.6f;

    [Tooltip("Altura dos olhos, de onde sai o raio que testa se há algo no meio.")]
    public float alturaOlhos = 1.7f;

    [Header("Perseguição")]
    [Tooltip("Velocidade correndo atrás do jogador. Deixe menor que a corrida do jogador para dar para fugir.")]
    public float velocidadePerseguicao = 4.4f;

    [Tooltip("Segundos sem ver o jogador até ela desistir e voltar para a ronda.")]
    public float tempoParaDesistir = 4f;

    [Tooltip("Distância em que ela alcança o jogador.")]
    public float distanciaPegar = 1.0f;

    [Tooltip("Raio em que ela ouve o jogador correndo, mesmo de costas. Use 0 para desligar.")]
    public float raioAudicaoCorrida = 3.5f;

    [Tooltip("Quando a porta destrancar, ela ouve o barulho e vai atrás do jogador.")]
    public bool perseguirQuandoPortaAbrir = true;

    [Header("Lanterna e sons")]
    [Tooltip("Spot Light da lanterna dela (fica vermelha-alaranjada na perseguição).")]
    public Light lanterna;

    [Tooltip("Som dos passos em loop. Toca só enquanto ela anda, mais rápido quando corre.")]
    public AudioSource passos;

    [Tooltip("Som quando ela vê o jogador (\"Ei!\").")]
    public AudioClip somAvistou;

    [Tooltip("Som quando ela pega o jogador.")]
    public AudioClip somPegou;

    [Header("Textos")]
    public string textoAvistou = "Ela te viu! Corra ou se esconda!";
    public string textoDesistiu = "Ufa... ela te perdeu de vista.";
    public string textoPegou = "A professora te pegou.";
    public string textoPortaAbriu = "A porta fez barulho... ela ouviu! Corra!";

    [Tooltip("Segundos entre ser pego e o nível reiniciar.")]
    public float tempoAntesDeReiniciar = 3f;

    /// <summary>Estado atual.</summary>
    public Estado EstadoAtual { get; private set; }

    /// <summary>Verdadeiro depois que ela pegou o jogador (usado para abaixar a música).</summary>
    public bool PegouJogador => pegouJogador;

    private NavMeshAgent agente;
    private Animator anim;
    private Transform jogador;
    private PlayerController controleJogador;
    private Color corLanterna;
    private int indicePonto;
    private float esperaRestante;
    private float tempoSemVer;
    private Vector3 ultimaPosicaoVista;
    private bool pegouJogador;
    private float volumePassos = 1f;
    private bool portaJaAbriu;

    /// <summary>Pega os componentes e encontra o jogador pela tag Player.</summary>
    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
            anim.applyRootMotion = false;

        if (lanterna != null)
            corLanterna = lanterna.color;

        if (passos != null)
        {
            volumePassos = passos.volume;
            passos.volume = 0f;
        }

        GameObject objJogador = GameObject.FindGameObjectWithTag("Player");
        if (objJogador != null)
        {
            jogador = objJogador.transform;
            controleJogador = objJogador.GetComponent<PlayerController>();
        }

        indicePonto = -1;
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

        if (!ArmarioEsconderijo.JogadorDentro && DistanciaAteJogador() <= distanciaPegar)
            PegarJogador();

        AtualizarAnimacaoESom();
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
    public void ComecarPerseguicao(string aviso)
    {
        EstadoAtual = Estado.Perseguindo;
        agente.speed = velocidadePerseguicao;
        tempoSemVer = 0f;
        ultimaPosicaoVista = jogador.position;

        if (somAvistou != null)
            UIManager.TocarSom2D(somAvistou, 1f);

        if (lanterna != null)
            lanterna.color = new Color(1f, 0.55f, 0.4f);

        MostrarAviso(aviso);
    }

    /// <summary>Volta a andar devagar pelos pontos de ronda, começando pelo mais próximo.</summary>
    private void VoltarParaRonda(bool avisar)
    {
        EstadoAtual = Estado.Ronda;
        agente.speed = velocidadeRonda;
        esperaRestante = esperaEmCadaPonto;

        if (lanterna != null)
            lanterna.color = corLanterna;

        if (pontosRonda != null && pontosRonda.Length > 0)
        {
            indicePonto = avisar ? PontoMaisProximo() : 0;
            agente.SetDestination(pontosRonda[indicePonto].position);
        }

        if (avisar)
            MostrarAviso(textoDesistiu);
    }

    /// <summary>
    /// Testa se ela enxerga o jogador: distância, ângulo e nada no meio do caminho.
    /// Também "ouve" o jogador correndo perto dela.
    /// </summary>
    private bool VeJogador()
    {
        if (ArmarioEsconderijo.JogadorDentro || Esconderijo.JogadorEscondido)
            return false;

        float distancia = DistanciaAteJogador();

        bool correndo = controleJogador != null && controleJogador.VelocidadeAtual > controleJogador.velocidadeAndar + 0.1f;
        if (correndo && distancia <= raioAudicaoCorrida)
            return true;

        float alcance = distanciaVisao * (LanternaCelular.Ligada ? multiplicadorLanternaJogador : 1f);
        if (distancia > alcance)
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

    /// <summary>Para tudo, trava o jogador, escurece a tela e reinicia o nível depois de um tempo.</summary>
    private void PegarJogador()
    {
        pegouJogador = true;
        agente.isStopped = true;

        if (passos != null)
            StartCoroutine(AbaixarSom(passos, 0.8f));

        if (somPegou != null)
            UIManager.TocarSom2D(somPegou, 1f);

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

        if (TelaPreta.Instancia != null)
            TelaPreta.Instancia.Escurecer(textoPegou);
        else
            MostrarAviso(textoPegou);

        Invoke(nameof(Reiniciar), tempoAntesDeReiniciar);
    }

    /// <summary>Reinicia o nível pelo GameManager.</summary>
    private void Reiniciar()
    {
        if (GameManager.Instancia != null)
            GameManager.Instancia.ReiniciarNivel();
    }

    /// <summary>Manda a velocidade para o Animator e liga/desliga o som dos passos.</summary>
    private void AtualizarAnimacaoESom()
    {
        float velocidade = agente.velocity.magnitude;

        if (anim != null)
        {
            anim.SetFloat("Velocidade", velocidade, 0.1f, Time.deltaTime);
            anim.SetBool("NoChao", true);
        }

        if (passos != null)
        {
            // Os passos sobem e descem de volume aos poucos, para não cortar seco quando ela para.
            bool andando = velocidade > 0.2f;
            if (andando && !passos.isPlaying)
                passos.Play();

            float alvo = andando ? volumePassos : 0f;
            passos.volume = Mathf.MoveTowards(passos.volume, alvo, Time.deltaTime * volumePassos * 3f);
            if (!andando && passos.volume <= 0.001f && passos.isPlaying)
                passos.Pause();

            float pitchAlvo = EstadoAtual == Estado.Perseguindo ? 1.5f : 1f;
            passos.pitch = Mathf.MoveTowards(passos.pitch, pitchAlvo, Time.deltaTime);
        }
    }

    /// <summary>Abaixa o volume de um som até zero em alguns segundos e para.</summary>
    private IEnumerator AbaixarSom(AudioSource som, float duracao)
    {
        float inicio = som.volume;
        float t = 0f;
        while (t < duracao)
        {
            t += Time.deltaTime;
            som.volume = Mathf.Lerp(inicio, 0f, t / duracao);
            yield return null;
        }
        som.Stop();
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

    /// <summary>Desenha na Scene View o alcance da visão (amarelo), da audição (azul) e a rota (vermelho).</summary>
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
