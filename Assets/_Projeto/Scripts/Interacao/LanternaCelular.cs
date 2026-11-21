using UnityEngine;

/// <summary>
/// Lanterna do celular (fica no Jogador). Nos níveis 1 a 4 funciona desde o começo; no Nível 5 só depois que o jogador pega o item "Celular".
/// A tecla F liga e desliga. A luz acompanha a câmera (para onde o jogador está olhando),
/// saindo da altura do peito, um pouco à frente, para não iluminar as costas do personagem.
/// Atenção: com a lanterna ligada, a Professora vê o jogador de mais longe.
/// </summary>
public class LanternaCelular : MonoBehaviour
{
    /// <summary>Verdadeiro enquanto a lanterna está acesa.</summary>
    public static bool Ligada { get; private set; }

    [Tooltip("Spot Light da lanterna.")]
    public Light luz;

    [Tooltip("Nome do item que libera a lanterna.")]
    public string itemNecessario = "Celular";

    public KeyCode tecla = KeyCode.F;
    public AudioClip somClique;

    [Header("Posição da luz")]
    [Tooltip("Altura da luz em relação aos pés do jogador.")]
    public float altura = 1.3f;

    [Tooltip("Quanto a luz fica à frente do corpo.")]
    public float frente = 0.45f;

    [Tooltip("Quanto a luz desce em relação ao olhar da câmera (graus).")]
    public float inclinacao = 6f;

    [Tooltip("Rapidez com que a luz acompanha a câmera.")]
    public float suavidade = 14f;

    [Tooltip("Segundos para a luz acender por completo (efeito de ligar).")]
    public float tempoAcender = 0.15f;

    private float intensidadeMaxima;
    private float brilho;

    /// <summary>Começa apagada.</summary>
    void Start()
    {
        Ligada = false;
        if (luz != null)
        {
            intensidadeMaxima = luz.intensity;
            luz.enabled = false;
        }
    }

    /// <summary>Liga/desliga com F. Dentro do armário, a lanterna apaga sozinha.</summary>
    void Update()
    {
        if (luz == null || !Liberada())
            return;

        if (ArmarioEsconderijo.JogadorDentro && Ligada)
            Trocar();

        if (Input.GetKeyDown(tecla) && !UIManager.MenuAberto && !ArmarioEsconderijo.JogadorDentro)
            Trocar();

        // Acende e apaga rapidinho, sem piscar seco.
        brilho = Mathf.MoveTowards(brilho, Ligada ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, tempoAcender));
        luz.intensity = intensidadeMaxima * brilho;
        luz.enabled = brilho > 0f;
    }

    /// <summary>Coloca a luz na frente do peito, apontando para onde a câmera olha.</summary>
    void LateUpdate()
    {
        if (luz == null || !luz.enabled)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 frenteCamera = cam.transform.forward;
        frenteCamera.y = 0f;
        if (frenteCamera.sqrMagnitude < 0.001f)
            frenteCamera = transform.forward;
        frenteCamera.Normalize();

        Vector3 posicao = transform.position + Vector3.up * altura + frenteCamera * frente;
        Quaternion rotacao = cam.transform.rotation * Quaternion.Euler(inclinacao, 0f, 0f);

        luz.transform.position = posicao;
        luz.transform.rotation = Quaternion.Slerp(luz.transform.rotation, rotacao, Time.deltaTime * suavidade);
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
        UIManager.TocarSom2D(somClique, 0.5f);
    }

    /// <summary>Apaga ao sair da cena.</summary>
    void OnDestroy()
    {
        Ligada = false;
    }
}
