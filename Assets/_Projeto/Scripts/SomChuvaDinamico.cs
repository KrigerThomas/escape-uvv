using UnityEngine;

/// <summary>
/// Controla o som da chuva do Nível 3:
///  - abafa e abaixa o som quando o jogador entra numa área fechada (ex.: o banheiro);
///  - abaixa mais ainda enquanto um painel com mouse está aberto (ex.: o Quadro do Abrantes).
/// Coloque no mesmo objeto do AudioSource da chuva (precisa de um AudioLowPassFilter também).
/// A troca é suave (vai mudando um pouco a cada quadro) e funciona com o jogo pausado.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class SomChuvaDinamico : MonoBehaviour
{
    [Header("Área fechada (abafa o som)")]
    [Tooltip("Caixa (BoxCollider) que marca a área fechada. Quando o jogador está dentro dela, o som fica abafado.")]
    public Collider areaFechada;

    [Header("Volume")]
    [Range(0f, 1f)] public float volumeNormal = 0.5f;
    [Range(0f, 1f)] public float volumeAreaFechada = 0.22f;
    [Range(0f, 1f)] public float volumeComPainelAberto = 0.12f;

    [Header("Abafamento (frequência de corte, em Hz)")]
    [Tooltip("Quanto maior, mais \"aberto\" o som. 22000 = sem filtro.")]
    public float corteNormal = 4500f;
    public float corteAreaFechada = 650f;

    [Tooltip("Velocidade da transição entre os estados.")]
    public float suavidade = 3f;

    private AudioSource fonte;
    private AudioLowPassFilter filtro;
    private Transform jogador;

    /// <summary>Pega os componentes e procura o jogador pela tag Player.</summary>
    void Start()
    {
        fonte = GetComponent<AudioSource>();
        filtro = GetComponent<AudioLowPassFilter>();

        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null)
            jogador = j.transform;
    }

    /// <summary>Decide o volume e o corte desejados e chega neles aos poucos.</summary>
    void Update()
    {
        bool dentro = jogador != null && areaFechada != null && areaFechada.bounds.Contains(jogador.position);

        float volumeAlvo = dentro ? volumeAreaFechada : volumeNormal;
        float corteAlvo = dentro ? corteAreaFechada : corteNormal;

        if (UIManager.MenuAberto)
            volumeAlvo = Mathf.Min(volumeAlvo, volumeComPainelAberto);

        // unscaledDeltaTime: continua funcionando quando o quadro pausa o jogo (timeScale = 0)
        float t = 1f - Mathf.Exp(-suavidade * Time.unscaledDeltaTime);
        fonte.volume = Mathf.Lerp(fonte.volume, volumeAlvo, t);
        filtro.cutoffFrequency = Mathf.Lerp(filtro.cutoffFrequency, corteAlvo, t);
    }
}
