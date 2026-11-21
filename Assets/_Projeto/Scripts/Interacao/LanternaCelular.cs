using UnityEngine;

/// <summary>
/// Lanterna do celular (fica no Jogador). Nos níveis 1 a 4 funciona desde o começo; no Nível 5 só depois que o jogador pega o item "Celular".
/// A tecla F liga e desliga, inclusive dentro do armário.
/// A luz sai da altura do peito e aponta para o ponto que está no meio da tela,
/// então ilumina exatamente para onde o jogador está olhando.
/// Atenção: com a lanterna ligada, a Professora vê o jogador de mais longe.
/// </summary>
public class LanternaCelular : MonoBehaviour
{
    /// <summary>Verdadeiro enquanto a lanterna está acesa.</summary>
    public static bool Ligada { get; private set; }

    /// <summary>Verdadeiro enquanto o jogo pede para o jogador ligar a lanterna (mostra a dica do F).</summary>
    public static bool PedindoParaLigar { get; private set; }

    [Tooltip("Spot Light da lanterna.")]
    public Light luz;

    [Tooltip("Nome do item que libera a lanterna.")]
    public string itemNecessario = "Celular";

    public KeyCode tecla = KeyCode.F;
    public AudioClip somClique;

    [Header("Feixe de luz")]
    [Tooltip("Força da luz.")]
    public float intensidade = 22f;

    [Tooltip("Até onde a luz alcança.")]
    public float alcance = 28f;

    [Tooltip("Abertura total do feixe (graus).")]
    public float abertura = 80f;

    [Tooltip("Abertura do miolo mais forte do feixe (graus).")]
    public float aberturaMiolo = 38f;

    [Header("Posição da luz")]
    [Tooltip("Altura da luz em relação aos pés do jogador.")]
    public float altura = 1.3f;

    [Tooltip("Quanto a luz fica à frente do corpo.")]
    public float frente = 0.45f;

    [Tooltip("Rapidez com que a luz acompanha o olhar.")]
    public float suavidade = 16f;

    [Tooltip("Distância máxima para procurar o ponto do meio da tela.")]
    public float distanciaMira = 40f;

    [Tooltip("Segundos para a luz acender por completo (efeito de ligar).")]
    public float tempoAcender = 0.15f;

    private float brilho;
    private LightShadows sombraNormal;
    private readonly RaycastHit[] acertos = new RaycastHit[16];

    /// <summary>Pede para o jogador ligar a lanterna: a dica do F aparece até ele apertar.</summary>
    public static void PedirParaLigar()
    {
        if (!Ligada)
            PedindoParaLigar = true;
    }

    /// <summary>Começa apagada e com o feixe ajustado.</summary>
    void Start()
    {
        Ligada = false;
        PedindoParaLigar = false;
        if (luz != null)
        {
            luz.type = LightType.Spot;
            luz.range = alcance;
            luz.spotAngle = abertura;
            luz.innerSpotAngle = aberturaMiolo;
            sombraNormal = luz.shadows;
            luz.enabled = false;
        }
    }

    /// <summary>Liga/desliga com F (também dentro do armário).</summary>
    void Update()
    {
        if (luz == null || !Liberada())
            return;

        if (Input.GetKeyDown(tecla) && !UIManager.MenuAberto)
            Trocar();

        // Acende e apaga rapidinho, sem piscar seco.
        brilho = Mathf.MoveTowards(brilho, Ligada ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, tempoAcender));
        luz.intensity = intensidade * brilho;
        luz.enabled = brilho > 0f;
    }

    /// <summary>Coloca a luz no peito (ou na fresta do armário) apontando para o meio da tela.</summary>
    void LateUpdate()
    {
        if (luz == null || !luz.enabled)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        // Dentro do armário a luz sai da fresta, junto com a câmera.
        // A porta do armário faria sombra e taparia a luz: lá dentro a lanterna não projeta sombra.
        luz.shadows = ArmarioEsconderijo.JogadorDentro ? LightShadows.None : sombraNormal;
        if (ArmarioEsconderijo.JogadorDentro)
        {
            luz.transform.SetPositionAndRotation(cam.transform.position + cam.transform.forward * 0.15f, cam.transform.rotation);
            return;
        }

        Vector3 frenteCamera = cam.transform.forward;
        frenteCamera.y = 0f;
        if (frenteCamera.sqrMagnitude < 0.001f)
            frenteCamera = transform.forward;
        frenteCamera.Normalize();

        Vector3 posicao = transform.position + Vector3.up * altura + frenteCamera * frente;
        Vector3 alvo = PontoDoMeioDaTela(cam, posicao);
        Vector3 direcao = alvo - posicao;
        if (direcao.sqrMagnitude < 0.01f)
            direcao = cam.transform.forward;

        luz.transform.position = posicao;
        luz.transform.rotation = Quaternion.Slerp(luz.transform.rotation, Quaternion.LookRotation(direcao), Time.deltaTime * suavidade);
    }

    /// <summary>
    /// Primeiro ponto que a câmera "enxerga" no meio da tela, depois do jogador
    /// (o que fica entre a câmera e o personagem é ignorado).
    /// </summary>
    private Vector3 PontoDoMeioDaTela(Camera cam, Vector3 origemLuz)
    {
        Ray raio = new Ray(cam.transform.position, cam.transform.forward);
        float pularAte = Vector3.Dot(origemLuz - raio.origin, raio.direction) + 0.3f;
        int n = Physics.RaycastNonAlloc(raio, acertos, distanciaMira, ~0, QueryTriggerInteraction.Ignore);
        float melhor = distanciaMira;
        Vector3 ponto = raio.GetPoint(distanciaMira);
        for (int i = 0; i < n; i++)
        {
            if (acertos[i].distance < pularAte || acertos[i].transform.IsChildOf(transform.root) || acertos[i].distance >= melhor)
                continue;
            melhor = acertos[i].distance;
            ponto = acertos[i].point;
        }
        return ponto;
    }

    /// <summary>
    /// Confere se o jogador já pode usar a lanterna.
    /// </summary>
    /// <returns>Verdadeiro se não precisa de item (níveis 1 a 4) ou se o item já foi pego (Nível 5).</returns>
    public bool Liberada()
    {
        return string.IsNullOrEmpty(itemNecessario) || Inventario.Tem(itemNecessario);
    }

    /// <summary>Inverte o estado da lanterna.</summary>
    private void Trocar()
    {
        Ligada = !Ligada;
        if (Ligada)
            PedindoParaLigar = false;
        UIManager.TocarSom2D(somClique, 0.5f);
    }

    /// <summary>Apaga ao sair da cena.</summary>
    void OnDestroy()
    {
        Ligada = false;
        PedindoParaLigar = false;
    }
}
