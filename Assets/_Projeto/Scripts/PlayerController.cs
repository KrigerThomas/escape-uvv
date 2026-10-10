using UnityEngine;

/// <summary>
/// Controla o jogador em terceira pessoa com movimentação física estável.
/// Utiliza velocidade física (linearVelocity) e desativa obrigatoriamente o Root Motion
/// do Animator para garantir que o personagem nunca seja lançado para o alto.
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

    [Tooltip("Distância do raio para checar se está no chão.")]
    public float distanciaChecagemChao = 0.2f;

    [Header("Segurança")]
    [Tooltip("Se o jogador cair abaixo desta altura, volta ao ponto inicial.")]
    public float alturaMinima = -10f;

    [Header("Referências")]
    public Transform cameraReferencia;

    public bool NoChao { get; private set; }
    public float VelocidadeAtual { get; private set; }

    private Rigidbody rb;
    private CapsuleCollider capsula;
    private Animator anim;
    private Vector3 posicaoInicial;
    private Quaternion rotacaoInicial;
    private Vector3 direcaoDesejada;
    private bool correndo;
    private bool querPular;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsula = GetComponent<CapsuleCollider>();
        anim = GetComponent<Animator>();

        // Desativa Root Motion para evitar que animações do Mixamo controlem a posição no ar
        if (anim != null)
            anim.applyRootMotion = false;

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        posicaoInicial = transform.position;
        rotacaoInicial = transform.rotation;
    }

    void Update()
    {
        // Garante que o Root Motion permaneça desativado mesmo ao trocar de controller
        if (anim != null && anim.applyRootMotion)
            anim.applyRootMotion = false;

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

    void FixedUpdate()
    {
        Vector3 origem = transform.position + Vector3.up * 0.2f;
        int layerMask = ~(1 << gameObject.layer);
        NoChao = Physics.Raycast(origem, Vector3.down, 0.2f + distanciaChecagemChao, layerMask, QueryTriggerInteraction.Ignore);

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
            transform.rotation = Quaternion.Slerp(transform.rotation, alvoRot, velocidadeGiro * Time.fixedDeltaTime);
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
            anim.SetFloat("Velocidade", VelocidadeAtual);
            anim.SetBool("NoChao", NoChao);
        }
    }

    private void Reaparecer()
    {
        rb.linearVelocity = Vector3.zero;
        transform.position = posicaoInicial;
        transform.rotation = rotacaoInicial;
    }
}
