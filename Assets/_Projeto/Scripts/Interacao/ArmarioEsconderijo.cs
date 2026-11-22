using System.Collections;
using UnityEngine;

/// <summary>
/// Armário onde o jogador se esconde com E (e sai com E de novo).
/// Dentro do armário: o jogador fica invisível e parado, a câmera vai para as frestas
/// da porta (ponto "Ponto_Camera") e a Professora não consegue vê-lo nem pegá-lo.
/// Se a professora já está correndo atrás e muito perto, não dá tempo de entrar.
/// </summary>
public class ArmarioEsconderijo : Interagivel
{
    [Header("Volumes dos sons")]
    [Range(0f, 1f)] public float volumePorta = 0.7f;

    [Tooltip("Dica do E do lado de fora.")]
    [TextArea] public string textoAcaoEntrar = "se esconder no armário";
    [Tooltip("Dica do E dentro do armário.")]
    [TextArea] public string textoAcaoSair = "sair do armário";
    [Tooltip("Ao tentar entrar com a professora olhando.")]
    [TextArea] public string textoSemTempo = "Ela está vendo você, não dá tempo de se esconder";

    /// <summary>Verdadeiro enquanto o jogador está dentro de algum armário.</summary>
    public static bool JogadorDentro { get; private set; }

    [Header("Armário")]
    [Tooltip("Filho dentro do armário, olhando para fora: de onde a câmera vê pelas frestas.")]
    public Transform pontoCamera;

    [Tooltip("Filho na frente do armário: onde o jogador aparece ao sair.")]
    public Transform pontoSaida;

    [Tooltip("Painel da interface com as frestas escuras (fica ligado só dentro do armário).")]
    public GameObject painelFrestas;

    [Tooltip("Distância em que a professora, correndo atrás, impede o jogador de se esconder.")]
    public float distanciaSegura = 4f;

    [Tooltip("Som ao entrar no armário.")]
    public AudioClip somPorta;

    [Tooltip("Som ao sair do armário (vazio = usa o mesmo de entrar).")]
    public AudioClip somSair;

    [Header("Tempo (combina com o som da porta)")]
    [Tooltip("Segundos entre apertar E e a porta bater com o jogador já dentro.")]
    public float tempoEntrar = 0f;

    [Tooltip("Segundos entre apertar E e a porta abrir para o jogador sair.")]
    public float tempoSair = 0f;

    private bool dentro;
    private bool emTransicao;

    /// <summary>Quadro em que o jogador entrou ou saiu de um armário (evita que o armário do lado reaja ao mesmo E).</summary>
    private static int quadroDaTroca = -1;
    private GameObject jogador;
    private Rigidbody rb;
    private PlayerController controle;
    private CameraFollow cameraSegue;
    private Renderer[] partesJogador;
    private Vector3 posicaoAntes;

    /// <summary>
    /// Dentro do armário, o próprio armário lê o E para sair (o jogador fica parado e invisível,
    /// então a área de alcance pode deixar de detectá-lo). Fora, funciona como qualquer Interagivel.
    /// </summary>
    protected override void Update()
    {
        if (emTransicao)
            return;

        if (dentro)
        {
            if (Input.GetKeyDown(tecla) && !UIManager.MenuAberto)
                Sair();
            return;
        }

        // Os armários ficam lado a lado e as áreas se encostam: enquanto o jogador está
        // dentro de um, ou no mesmo quadro em que entrou/saiu, os outros não reagem ao E.
        if (JogadorDentro || Time.frameCount == quadroDaTroca)
            return;

        base.Update();
    }

    /// <summary>Sem dica enquanto a porta abre/fecha, e nos armários vizinhos com o jogador escondido.</summary>
    public override bool PodeInteragir()
    {
        return !emTransicao && (dentro || !JogadorDentro);
    }

    /// <summary>Texto da dica muda se o jogador já está dentro.</summary>
    protected override string TextoDica()
    {
        return dentro ? textoAcaoSair : textoAcaoEntrar;
    }

    /// <summary>Entra ou sai, conforme o caso.</summary>
    public override void Interagir()
    {
        if (emTransicao)
            return;
        if (dentro)
            Sair();
        else
            Entrar();
    }

    /// <summary>Esconde o jogador e leva a câmera para dentro do armário.</summary>
    private void Entrar()
    {
        Professora professora = FindFirstObjectByType<Professora>();
        if (professora != null && professora.EstadoAtual == Professora.Estado.Perseguindo &&
            Vector3.Distance(professora.transform.position, transform.position) < distanciaSegura)
        {
            Avisar(textoSemTempo);
            return;
        }

        jogador = GameObject.FindGameObjectWithTag("Player");
        if (jogador == null)
            return;

        rb = jogador.GetComponent<Rigidbody>();
        controle = jogador.GetComponent<PlayerController>();
        partesJogador = jogador.GetComponentsInChildren<Renderer>();
        cameraSegue = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        posicaoAntes = jogador.transform.position;

        controle.enabled = false;
        if (!rb.isKinematic)
            rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        Animator anim = jogador.GetComponent<Animator>();
        if (anim != null)
            anim.SetFloat("Velocidade", 0f);

        JogadorDentro = true;
        quadroDaTroca = Time.frameCount;
        EsconderDica();
        UIManager.TocarSom2D(somPorta, volumePorta);
        StartCoroutine(EntrarAosPoucos());
    }

    /// <summary>
    /// A porta abre no começo do som: o jogador vira e dá um passo para dentro.
    /// Quando a porta bate (fim do tempo), ele some e a câmera vai para as frestas.
    /// </summary>
    private IEnumerator EntrarAosPoucos()
    {
        emTransicao = true;
        Vector3 inicio = jogador.transform.position;
        Vector3 alvo = pontoCamera != null ? pontoCamera.position : transform.position;
        alvo.y = inicio.y;
        Vector3 olhar = alvo - inicio;
        olhar.y = 0f;
        if (olhar.sqrMagnitude > 0.001f)
            jogador.transform.rotation = Quaternion.LookRotation(olhar);

        float t = 0f;
        while (tempoEntrar > 0.05f && t < tempoEntrar)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.1f) / (tempoEntrar - 0.15f)));
            Vector3 p = Vector3.Lerp(inicio, alvo, k * 0.8f);
            jogador.transform.position = p;
            rb.position = p;
            yield return null;
        }

        foreach (Renderer r in partesJogador)
            r.enabled = false;
        if (cameraSegue != null)
            cameraSegue.enabled = false;
        if (painelFrestas != null)
            painelFrestas.SetActive(true);

        dentro = true;
        emTransicao = false;
        Atual = this;
        AtualizarDica();
    }

    /// <summary>Devolve o jogador na frente do armário e a câmera normal.</summary>
    public void Sair()
    {
        if (emTransicao)
            return;
        UIManager.TocarSom2D(somSair != null ? somSair : somPorta, 0.7f);
        EsconderDica();
        StartCoroutine(SairAosPoucos());
    }

    /// <summary>Espera a porta abrir (começo do som) e devolve o jogador na frente do armário.</summary>
    private IEnumerator SairAosPoucos()
    {
        emTransicao = true;
        if (tempoSair > 0f)
            yield return new WaitForSeconds(tempoSair);

        Vector3 destino = pontoSaida != null ? pontoSaida.position : posicaoAntes;
        jogador.transform.position = destino;
        rb.position = destino;
        rb.isKinematic = false;
        controle.enabled = true;
        foreach (Renderer r in partesJogador)
            r.enabled = true;

        if (cameraSegue != null)
            cameraSegue.enabled = true;

        if (painelFrestas != null)
            painelFrestas.SetActive(false);

        dentro = false;
        JogadorDentro = false;
        quadroDaTroca = Time.frameCount;
        emTransicao = false;
    }

    /// <summary>Mantém a câmera presa nas frestas enquanto o jogador está dentro.</summary>
    void LateUpdate()
    {
        if (dentro && pontoCamera != null && Camera.main != null)
            Camera.main.transform.SetPositionAndRotation(pontoCamera.position, pontoCamera.rotation);
    }

    /// <summary>Evita ficar "escondido para sempre" ao trocar de cena.</summary>
    void OnDestroy()
    {
        if (dentro)
            JogadorDentro = false;
    }
}
