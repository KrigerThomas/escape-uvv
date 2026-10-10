using UnityEngine;

/// <summary>
/// Controla o jogador em terceira pessoa: andar, correr, pular e girar para onde está indo.
/// O deslocamento horizontal usa Transform.Translate e o pulo usa Rigidbody.AddForce.
/// A direção do movimento é relativa à câmera: a tecla W anda para onde a câmera olha.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Velocidade ao andar, em metros por segundo.")]
    public float velocidadeAndar = 3.5f;

    [Tooltip("Velocidade ao correr (segure Shift).")]
    public float velocidadeCorrer = 6.5f;

    [Tooltip("Rapidez com que o personagem gira para a direção do movimento.")]
    public float velocidadeGiro = 12f;

    [Header("Pulo")]
    [Tooltip("Velocidade vertical dada ao pular (AddForce com ForceMode.VelocityChange).")]
    public float forcaPulo = 5.5f;

    [Tooltip("Distância extra abaixo do colisor para considerar que o jogador está no chão.")]
    public float margemChao = 0.15f;

    [Header("Segurança")]
    [Tooltip("Se o jogador cair abaixo desta altura, volta ao ponto inicial.")]
    public float alturaMinima = -15f;

    [Header("Referências")]
    [Tooltip("Câmera usada como referência de direção. Se vazio, usa a Main Camera.")]
    public Transform cameraReferencia;

    /// <summary>Verdadeiro quando o jogador está tocando o chão.</summary>
    public bool NoChao { get; private set; }

    /// <summary>Velocidade horizontal atual em m/s. Será usada pelo Animator (parado, andando, correndo).</summary>
    public float VelocidadeAtual { get; private set; }

    private Rigidbody rb;
    private CapsuleCollider capsula;
    private Vector3 posicaoInicial;
    private Quaternion rotacaoInicial;
    private float entradaH;
    private float entradaV;
    private bool correndo;
    private bool querPular;

    /// <summary>
    /// Guarda as referências e configura o Rigidbody para o personagem não tombar.
    /// Também memoriza o ponto inicial, usado se o jogador cair do mapa.
    /// </summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsula = GetComponent<CapsuleCollider>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        posicaoInicial = transform.position;
        rotacaoInicial = transform.rotation;
    }

    /// <summary>A cada quadro: lê o teclado, move o personagem e confere se ele caiu do mapa.</summary>
    void Update()
    {
        LerEntrada();
        Mover();

        if (transform.position.y < alturaMinima)
            Reaparecer();
    }

    /// <summary>A cada passo de física: confere o chão e aplica o pulo, se foi pedido.</summary>
    void FixedUpdate()
    {
        VerificarChao();

        if (querPular && NoChao)
            Pular();

        querPular = false;
    }

    /// <summary>Lê as teclas de movimento (WASD ou setas), corrida (Shift) e pulo (Espaço).</summary>
    private void LerEntrada()
    {
        entradaH = Input.GetAxisRaw("Horizontal");
        entradaV = Input.GetAxisRaw("Vertical");
        correndo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (Input.GetKeyDown(KeyCode.Space))
            querPular = true;
    }

    /// <summary>
    /// Move o personagem com Translate, relativo à direção da câmera,
    /// e gira suavemente para a direção do movimento.
    /// </summary>
    private void Mover()
    {
        Transform cam = cameraReferencia;
        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;

        Vector3 frente = Vector3.forward;
        Vector3 direita = Vector3.right;
        if (cam != null)
        {
            frente = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            direita = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        }

        Vector3 direcao = frente * entradaV + direita * entradaH;
        if (direcao.sqrMagnitude > 1f)
            direcao.Normalize();

        float velocidade = correndo ? velocidadeCorrer : velocidadeAndar;
        VelocidadeAtual = direcao.magnitude * velocidade;

        if (direcao.sqrMagnitude > 0.0001f)
        {
            transform.Translate(direcao * velocidade * Time.deltaTime, Space.World);

            Quaternion alvo = Quaternion.LookRotation(direcao, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, alvo, velocidadeGiro * Time.deltaTime);
        }
    }

    /// <summary>Dispara um raio para baixo a partir do centro do colisor para saber se há chão logo abaixo.</summary>
    private void VerificarChao()
    {
        Bounds limites = capsula.bounds;
        float distancia = limites.extents.y + margemChao;
        NoChao = Physics.Raycast(limites.center, Vector3.down, distancia, ~0, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Faz o jogador pular, aplicando uma força para cima com AddForce.</summary>
    private void Pular()
    {
        rb.AddForce(Vector3.up * forcaPulo, ForceMode.VelocityChange);
    }

    /// <summary>Leva o jogador de volta ao ponto inicial e zera a velocidade da queda.</summary>
    private void Reaparecer()
    {
        rb.linearVelocity = Vector3.zero;
        transform.position = posicaoInicial;
        transform.rotation = rotacaoInicial;
    }
}
