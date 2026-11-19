using UnityEngine;

/// <summary>
/// Armário onde o jogador se esconde com E (e sai com E de novo).
/// Dentro do armário: o jogador fica invisível e parado, a câmera vai para as frestas
/// da porta (ponto "Ponto_Camera") e a Professora não consegue vê-lo nem pegá-lo.
/// Se a professora já está correndo atrás e muito perto, não dá tempo de entrar.
/// </summary>
public class ArmarioEsconderijo : Interagivel
{
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

    public AudioClip somPorta;

    private bool dentro;
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
        if (dentro)
        {
            if (Input.GetKeyDown(tecla) && !UIManager.MenuAberto)
                Sair();
            return;
        }

        base.Update();
    }

    /// <summary>Texto da dica muda se o jogador já está dentro.</summary>
    protected override string TextoDica()
    {
        return dentro ? "sair do armário" : "se esconder no armário";
    }

    /// <summary>Entra ou sai, conforme o caso.</summary>
    public override void Interagir()
    {
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
            Avisar("Ela está vendo você! Não dá tempo de se esconder.");
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
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        foreach (Renderer r in partesJogador)
            r.enabled = false;

        Animator anim = jogador.GetComponent<Animator>();
        if (anim != null)
            anim.SetFloat("Velocidade", 0f);

        if (cameraSegue != null)
            cameraSegue.enabled = false;

        if (painelFrestas != null)
            painelFrestas.SetActive(true);

        dentro = true;
        JogadorDentro = true;
        EsconderDica();
        UIManager.TocarSom2D(somPorta, 0.7f);
    }

    /// <summary>Devolve o jogador na frente do armário e a câmera normal.</summary>
    public void Sair()
    {
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
        UIManager.TocarSom2D(somPorta, 0.7f);
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
