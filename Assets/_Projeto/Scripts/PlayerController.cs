using UnityEngine;

/// <summary>
/// Controla o jogador em terceira pessoa com movimentação física estável.
/// Usa a velocidade do Rigidbody (linearVelocity) para andar e correr, AddForce para pular
/// e desativa o Root Motion do Animator para que as animações não empurrem o personagem.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Velocidade ao andar em metros por segundo.")]
    public float velocidadeAndar = 3.5f;

    [Tooltip("Velocidade ao correr (segurando Shift).")]
    public float velocidadeCorrer = 6.0f;

    [Tooltip("Rapidez com que o personagem gira para a direção do movimento.")]
    public float velocidadeGiro = 12f;

    [Header("Pulo e Chão")]
    [Tooltip("Força vertical do pulo.")]
    public float forcaPulo = 5.0f;

    [Tooltip("Distância extra do raio para checar se está no chão.")]
    public float distanciaChecagemChao = 0.2f;

    [Header("Segurança")]
    [Tooltip("Se o jogador cair abaixo desta altura, volta ao ponto inicial.")]
    public float alturaMinima = -10f;

    [Header("Referências")]
    public Transform cameraReferencia;

    /// <summary>Verdadeiro quando há chão logo abaixo do jogador.</summary>
    public bool NoChao { get; private set; }

    /// <summary>Velocidade horizontal atual, usada pelo Animator.</summary>
    public float VelocidadeAtual { get; private set; }

    private Rigidbody rb;
    private Animator anim;
    private Vector3 posicaoInicial;
    private Quaternion rotacaoInicial;
    private Vector3 direcaoDesejada;
    private bool correndo;
    private bool querPular;

    /// <summary>Vazio de propósito: bloqueia o Root Motion das animações do Mixamo.</summary>
    void OnAnimatorMove()
    {
    }

    /// <summary>Pega os componentes, configura o Rigidbody e guarda o ponto inicial.</summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        if (anim != null)
            anim.applyRootMotion = false;

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        posicaoInicial = transform.position;
        rotacaoInicial = transform.rotation;
    }

    /// <summary>Lê o teclado e calcula a direção do movimento em relação à câmera.</summary>
    void Update()
    {
        if (UIManager.MenuAberto)
        {
            direcaoDesejada = Vector3.zero;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        correndo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (Input.GetKeyDown(KeyCode.Space))
            querPular = true;

        Transform cam = cameraReferencia != null ? cameraReferencia : (Camera.main != null ? Camera.main.transform : null);
        Vector3 frente = Vector3.forward;
        Vector3 direita = Vector3.right;

        if (cam != null)
        {
            frente = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            direita = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        }

        direcaoDesejada = (frente * v + direita * h).normalized;

        if (transform.position.y < alturaMinima)
            Reaparecer();
    }

    /// <summary>Aplica movimento, rotação e pulo na física e atualiza o Animator.</summary>
    void FixedUpdate()
    {
        NoChao = ChecarChao();

        float vel = correndo ? velocidadeCorrer : velocidadeAndar;
        Vector3 velHorizontal = direcaoDesejada * vel;

        Vector3 velAtual = rb.linearVelocity;
        velAtual.x = velHorizontal.x;
        velAtual.z = velHorizontal.z;
        rb.linearVelocity = velAtual;

        VelocidadeAtual = direcaoDesejada.magnitude * vel;

        if (direcaoDesejada.sqrMagnitude > 0.001f)
        {
            Quaternion alvoRot = Quaternion.LookRotation(direcaoDesejada, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, alvoRot, velocidadeGiro * Time.fixedDeltaTime));
        }

        if (querPular && NoChao)
        {
            Vector3 velPulo = rb.linearVelocity;
            velPulo.y = 0f;
            rb.linearVelocity = velPulo;
            rb.AddForce(Vector3.up * forcaPulo, ForceMode.Impulse);
        }
        querPular = false;

        if (anim != null)
        {
            anim.SetFloat("Velocidade", VelocidadeAtual, 0.1f, Time.fixedDeltaTime);
            anim.SetBool("NoChao", NoChao);
        }
    }

    /// <summary>
    /// Lança um raio para baixo a partir dos pés e ignora os colliders do próprio jogador.
    /// </summary>
    /// <returns>Verdadeiro se encontrou algum chão ou objeto logo abaixo.</returns>
    private bool ChecarChao()
    {
        Vector3 origem = transform.position + Vector3.up * 0.2f;
        RaycastHit[] batidas = Physics.RaycastAll(origem, Vector3.down, 0.2f + distanciaChecagemChao, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < batidas.Length; i++)
            if (!batidas[i].transform.IsChildOf(transform))
                return true;

        return false;
    }

    /// <summary>Volta o jogador ao ponto inicial se ele cair do mapa.</summary>
    private void Reaparecer()
    {
        rb.linearVelocity = Vector3.zero;
        rb.position = posicaoInicial;
        rb.rotation = rotacaoInicial;
    }
}
