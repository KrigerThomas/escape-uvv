using UnityEngine;

/// <summary>
/// Som dos passos do jogador, sincronizado com a animação: acompanha a altura dos dois pés
/// do personagem e toca um passo no instante em que cada pé encosta no chão.
/// O som muda conforme o chão embaixo do jogador: piso (dentro dos prédios), pedra/cascalho
/// (caminhos do pátio) ou grama. No terreno do Nível 2 vale a textura mais forte naquele ponto;
/// nos outros objetos vale o nome do material.
/// Cada passo sorteia um som da lista, sem repetir o último, e varia um pouco o tom.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PassosJogador : MonoBehaviour
{
    [Tooltip("Passos em piso liso (dentro dos prédios).")]
    public AudioClip[] passos;

    [Tooltip("Passos em pedra e cascalho (caminhos do pátio).")]
    public AudioClip[] passosPedra;

    [Tooltip("Passos na grama e na terra.")]
    public AudioClip[] passosGrama;

    [Tooltip("Pedaços do nome do material ou da textura do terreno que contam como pedra.")]
    public string[] palavrasPedra = { "pedra", "cascalho", "rocha", "seixo", "calcamento", "calcada", "paving", "gravel", "rock" };

    [Tooltip("Pedaços do nome do material ou da textura do terreno que contam como grama.")]
    public string[] palavrasGrama = { "grama", "lama", "terra", "folha", "areia", "gramado", "grass", "ground" };

    [Tooltip("Acima desta velocidade conta como correndo.")]
    public float velocidadeCorrida = 4.5f;

    [Range(0f, 1f)] public float volume = 0.3f;
    [Range(0f, 1f)] public float volumeCorrida = 0.4f;

    [Tooltip("Tempo mínimo entre dois passos andando e correndo (evita passo dobrado quando a animação troca).")]
    public float intervaloMinimoAndando = 0.28f;
    public float intervaloMinimoCorrendo = 0.18f;

    [Tooltip("Variação do tom de um passo para outro.")]
    public float variacaoTom = 0.08f;

    /// <summary>Momento (Time.time) do último passo tocado. Útil para conferir a sincronia.</summary>
    public float UltimoPasso { get; private set; }

    private Rigidbody rb;
    private AudioSource fonte;
    private Transform peEsquerdo, peDireito;
    private Pe esquerdo = new Pe(), direito = new Pe();
    private AudioClip ultimoSom;

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
        bool noChao = NoChao();
        bool andando = rapidez > 0.4f && noChao;

        // No ar (pulo ou queda) os pés fazem outro movimento: esquece os limites para recomeçar do zero ao pousar.
        if (!noChao)
        {
            Zerar(esquerdo);
            Zerar(direito);
            return;
        }

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

    /// <summary>
    /// Esquece a altura mínima e máxima guardadas de um pé.
    /// </summary>
    /// <param name="pe">Pé a zerar.</param>
    private void Zerar(Pe pe)
    {
        pe.minimo = float.MaxValue;
        pe.maximo = float.MinValue;
        pe.noAr = false;
        pe.descendo = false;
    }

    /// <summary>Toca um passo sorteado da lista do chão em que o jogador está.</summary>
    private void Tocar(bool correndo)
    {
        if (Time.time - UltimoPasso < (correndo ? intervaloMinimoCorrendo : intervaloMinimoAndando))
            return;
        AudioClip[] lista = ListaDoChao();
        if (lista == null || lista.Length == 0)
            return;

        // sorteia sem repetir o último som
        AudioClip som = lista[Random.Range(0, lista.Length)];
        if (som == ultimoSom && lista.Length > 1)
            som = lista[(System.Array.IndexOf(lista, som) + Random.Range(1, lista.Length)) % lista.Length];
        ultimoSom = som;

        UltimoPasso = Time.time;
        fonte.pitch = 1f + Random.Range(-variacaoTom, variacaoTom) + (correndo ? 0.04f : 0f);
        fonte.PlayOneShot(som, (correndo ? volumeCorrida : volume) * Random.Range(0.85f, 1f));
    }

    /// <summary>Descobre o tipo de chão embaixo do jogador e devolve a lista de sons certa.</summary>
    private AudioClip[] ListaDoChao()
    {
        string nome = "";
        if (Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out RaycastHit chao, 1f, ~0, QueryTriggerInteraction.Ignore))
        {
            Terrain terreno = chao.collider.GetComponent<Terrain>();
            if (terreno != null)
                nome = CamadaMaisForte(terreno, chao.point);
            else
            {
                Renderer r = chao.collider.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null)
                    nome = r.sharedMaterial.name;
            }
        }
        nome = nome.ToLowerInvariant();

        if (Contem(nome, palavrasPedra) && passosPedra != null && passosPedra.Length > 0)
            return passosPedra;
        if (Contem(nome, palavrasGrama) && passosGrama != null && passosGrama.Length > 0)
            return passosGrama;
        return passos;
    }

    /// <summary>Verdadeiro se o nome tiver algum dos pedaços da lista.</summary>
    private static bool Contem(string nome, string[] palavras)
    {
        if (palavras == null)
            return false;
        foreach (string palavra in palavras)
            if (!string.IsNullOrEmpty(palavra) && nome.Contains(palavra))
                return true;
        return false;
    }

    /// <summary>Nome da textura do terreno que mais aparece no ponto.</summary>
    private static string CamadaMaisForte(Terrain terreno, Vector3 ponto)
    {
        TerrainData dados = terreno.terrainData;
        Vector3 local = ponto - terreno.transform.position;
        int x = Mathf.Clamp(Mathf.RoundToInt(local.x / dados.size.x * (dados.alphamapWidth - 1)), 0, dados.alphamapWidth - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt(local.z / dados.size.z * (dados.alphamapHeight - 1)), 0, dados.alphamapHeight - 1);
        float[,,] pesos = dados.GetAlphamaps(x, z, 1, 1);

        int melhor = 0;
        for (int i = 1; i < pesos.GetLength(2); i++)
            if (pesos[0, 0, i] > pesos[0, 0, melhor])
                melhor = i;

        TerrainLayer camada = melhor < dados.terrainLayers.Length ? dados.terrainLayers[melhor] : null;
        return camada != null ? camada.name : "";
    }

    /// <summary>Confere se há chão logo abaixo dos pés.</summary>
    private bool NoChao()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.45f, ~0, QueryTriggerInteraction.Ignore);
    }
}
