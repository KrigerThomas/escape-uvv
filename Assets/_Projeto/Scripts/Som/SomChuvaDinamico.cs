using UnityEngine;

// Controla o som da chuva do Nível 3.
// O volume BASE é o "Volume" do próprio AudioSource (o que aparece no Inspector).
// Este script só multiplica esse valor:
//   - dentro do banheiro: fica mais baixo e abafado (filtro passa-baixa)
//   - com o painel do quadro aberto: fica ainda mais baixo
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class SomChuvaDinamico : MonoBehaviour
{
    [Header("Área fechada (abafa o som)")]
    public Collider areaFechada;

    [Header("Volume (multiplica o Volume do AudioSource)")]
    [Range(0f, 1f)] public float multiplicadorAreaFechada = 0.6f;
    [Range(0f, 1f)] public float multiplicadorPainelAberto = 0.35f;

    [Header("Abafamento (frequência de corte, em Hz)")]
    public float corteNormal = 4500f;
    public float corteAreaFechada = 1300f;
    public float suavidade = 3f;

    private AudioSource fonte;
    private AudioLowPassFilter filtro;
    private Transform jogador;
    private float volumeBase;

    void Start()
    {
        fonte = GetComponent<AudioSource>();
        filtro = GetComponent<AudioLowPassFilter>();
        volumeBase = fonte.volume; // valor do Inspector
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jogador = j.transform;
    }

    void Update()
    {
        bool dentro = jogador != null && areaFechada != null && areaFechada.bounds.Contains(jogador.position);

        float volumeAlvo = volumeBase * (dentro ? multiplicadorAreaFechada : 1f);
        float corteAlvo = dentro ? corteAreaFechada : corteNormal;
        if (UIManager.MenuAberto) volumeAlvo = Mathf.Min(volumeAlvo, volumeBase * multiplicadorPainelAberto);

        // unscaledDeltaTime: continua funcionando com o jogo pausado (quadro aberto)
        float t = 1f - Mathf.Exp(-suavidade * Time.unscaledDeltaTime);
        fonte.volume = Mathf.Lerp(fonte.volume, volumeAlvo, t);
        filtro.cutoffFrequency = Mathf.Lerp(filtro.cutoffFrequency, corteAlvo, t);
    }
}
