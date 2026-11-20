using UnityEngine;

/// <summary>
/// Som dos passos do jogador, sincronizado com a animação: acompanha a altura dos dois pés
/// do personagem e toca um passo no instante em que cada pé encosta no chão.
/// Andando usa os sons de passo normal; correndo (Shift), os sons de corrida.
/// Cada passo sorteia um som da lista e varia um pouco o tom (para não ficar repetitivo).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PassosJogador : MonoBehaviour
{
    [Tooltip("Sons de passo andando (vários, para variar).")]
    public AudioClip[] passos;

    [Tooltip("Sons de passo correndo. Vazio = usa os de andar.")]
    public AudioClip[] passosCorrida;

    [Tooltip("Acima desta velocidade conta como correndo.")]
    public float velocidadeCorrida = 4.5f;

    [Range(0f, 1f)] public float volume = 0.3f;
    [Range(0f, 1f)] public float volumeCorrida = 0.4f;

    [Tooltip("Variação do tom de um passo para outro.")]
    public float variacaoTom = 0.08f;

    /// <summary>Momento (Time.time) do último passo tocado. Útil para conferir a sincronia.</summary>
    public float UltimoPasso { get; private set; }

    private Rigidbody rb;
    private AudioSource fonte;
    private Transform peEsquerdo, peDireito;
    private Pe esquerdo = new Pe(), direito = new Pe();

    /// <summary>Guarda a altura de um pé ao longo do tempo para saber quando ele pisa.</summary>
    private class Pe
    {
        public float minimo = float.MaxValue, maximo = float.MinValue;
        public float anterior;
        public bool noAr, descendo;
    }

    /// <summary>Acha os ossos dos pés e cria a fonte de som.</summary>
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;

        Animator anim = GetComponent<Animator>();
        if (anim != null && anim.isHuman)
        {
            peEsquerdo = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            peDireito = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        }
    }

    /// <summary>Depois da animação do quadro, confere se algum pé acabou de pisar.</summary>
    void LateUpdate()
    {
        if (peEsquerdo == null || peDireito == null || rb.isKinematic || Time.timeScale == 0f)
            return;

        Vector3 velocidade = rb.linearVelocity;
        velocidade.y = 0f;
        float rapidez = velocidade.magnitude;
        bool andando = rapidez > 0.4f && NoChao();

        Conferir(esquerdo, peEsquerdo, andando, rapidez);
        Conferir(direito, peDireito, andando, rapidez);
    }

    /// <summary>
    /// O pé sobe e desce durante a animação. Quando ele sobe bem (passo no ar) e depois
    /// volta para perto do ponto mais baixo, é a hora em que encosta no chão: toca o passo.
    /// </summary>
    private void Conferir(Pe pe, Transform osso, bool andando, float rapidez)
    {
        float altura = osso.position.y - transform.position.y;

        // Os limites acompanham a animação aos poucos (andar e correr mexem o pé de forma diferente).
        pe.minimo = Mathf.Min(pe.minimo, altura) + Time.deltaTime * 0.05f;
        pe.maximo = Mathf.Max(pe.maximo, altura) - Time.deltaTime * 0.05f;
        if (pe.maximo < pe.minimo)
            pe.maximo = pe.minimo;

        float amplitude = pe.maximo - pe.minimo;
        if (!andando || amplitude < 0.03f)
        {
            pe.noAr = false;
            pe.anterior = altura;
            return;
        }

        // O calcanhar bate no chão no primeiro ponto mais baixo depois do pé subir:
        // o pé vinha descendo e começa a subir de novo (ou chega bem perto do chão).
        bool parouDeDescer = pe.descendo && altura > pe.anterior && pe.anterior < pe.minimo + amplitude * 0.5f;
        bool encostou = altura < pe.minimo + amplitude * 0.15f;

        if (altura > pe.minimo + amplitude * 0.5f)
            pe.noAr = true;
        else if (pe.noAr && (parouDeDescer || encostou))
        {
            pe.noAr = false;
            Tocar(rapidez > velocidadeCorrida);
        }

        pe.descendo = altura < pe.anterior;
        pe.anterior = altura;
    }

    /// <summary>Toca um passo sorteado.</summary>
    private void Tocar(bool correndo)
    {
        AudioClip[] lista = correndo && passosCorrida != null && passosCorrida.Length > 0 ? passosCorrida : passos;
        if (lista == null || lista.Length == 0)
            return;

        UltimoPasso = Time.time;
        fonte.pitch = 1f + Random.Range(-variacaoTom, variacaoTom);
        fonte.PlayOneShot(lista[Random.Range(0, lista.Length)], correndo ? volumeCorrida : volume);
    }

    /// <summary>Confere se há chão logo abaixo dos pés.</summary>
    private bool NoChao()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.45f, ~0, QueryTriggerInteraction.Ignore);
    }
}
