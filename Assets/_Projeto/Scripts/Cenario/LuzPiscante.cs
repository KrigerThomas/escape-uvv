using UnityEngine;

/// <summary>
/// Faz uma luz falhar (lâmpada ou poste com mau contato), com o som sempre junto.
///
/// - Som CURTO (um estalo, menos de 1 s): a luz pisca em momentos sorteados e o
///   estalo toca a cada vez que ela apaga.
/// - Som LONGO (um zumbido de lâmpada falhando): o som toca em loop e a luz apaga
///   exatamente nos estalos do áudio.
/// - Sem som: só a luz pisca.
///
/// O brilho do material da lâmpada (Emission) acompanha a luz.
/// </summary>
[RequireComponent(typeof(Light))]
public class LuzPiscante : MonoBehaviour
{
    [Tooltip("Renderer da lâmpada, para apagar o brilho junto com a luz (opcional).")]
    public Renderer lampada;

    [Header("Piscadas sorteadas (som curto ou sem som)")]
    [Tooltip("Menor e maior tempo, em segundos, entre uma falha e outra.")]
    public Vector2 intervaloEntreFalhas = new Vector2(2f, 6f);

    [Tooltip("Quantas piscadas, no máximo, cada falha tem.")]
    public int piscadasMaximas = 4;

    [Header("Som")]
    [Tooltip("Estalo curto ou zumbido longo da lâmpada falhando.")]
    public AudioClip somFalha;

    [Range(0f, 1f)] public float volumeSom = 0.5f;

    [Tooltip("Até que distância (em metros) dá para ouvir.")]
    public float distanciaSom = 14f;

    [Tooltip("Só para som longo: quanto o som precisa subir para a luz apagar (0 = qualquer barulho, 1 = só o pico).")]
    [Range(0.05f, 0.95f)] public float sensibilidade = 0.3f;

    private const float JANELA = 0.02f;       // o volume do som é medido a cada 20 ms
    private const float SOM_CURTO = 1f;       // até 1 s conta como estalo

    private AudioSource fonteSom;
    private float[] envelope;
    private float limiar;

    private Light luz;
    private float intensidadeNormal;
    private Color emissaoNormal;
    private MaterialPropertyBlock bloco;
    private float proximaFalha;
    private int piscadasRestantes;
    private float trocaEstado;
    private bool acesa = true;

    void Start()
    {
        luz = GetComponent<Light>();
        intensidadeNormal = luz.intensity;
        bloco = new MaterialPropertyBlock();

        if (lampada != null && lampada.sharedMaterial != null && lampada.sharedMaterial.HasProperty("_EmissionColor"))
            emissaoNormal = lampada.sharedMaterial.GetColor("_EmissionColor");

        if (somFalha != null)
        {
            fonteSom = gameObject.AddComponent<AudioSource>();
            fonteSom.playOnAwake = false;
            fonteSom.volume = volumeSom;
            fonteSom.spatialBlend = 1f;
            fonteSom.rolloffMode = AudioRolloffMode.Linear;
            fonteSom.minDistance = 1.5f;
            fonteSom.maxDistance = distanciaSom;
            fonteSom.dopplerLevel = 0f;

            if (somFalha.length > SOM_CURTO)
                PrepararSomLongo();
        }

        proximaFalha = Time.time + Random.Range(intervaloEntreFalhas.x, intervaloEntreFalhas.y);
    }

    /// <summary>Som longo: mede o volume a cada 20 ms e toca em loop.</summary>
    private void PrepararSomLongo()
    {
        // garante que o áudio está carregado antes de ler (no jogo exportado pode não estar)
        if (somFalha.loadState != AudioDataLoadState.Loaded) somFalha.LoadAudioData();

        int canais = somFalha.channels;
        float[] amostras = new float[somFalha.samples * canais];
        if (!somFalha.GetData(amostras, 0))
        {
            Debug.LogWarning("LuzPiscante: não deu para ler o áudio " + somFalha.name + " (Load Type precisa ser Decompress On Load).");
            return;
        }

        int porJanela = Mathf.Max(1, Mathf.RoundToInt(somFalha.frequency * JANELA)) * canais;
        envelope = new float[amostras.Length / porJanela + 1];
        float menor = float.MaxValue, maior = 0f;
        for (int j = 0; j < envelope.Length; j++)
        {
            double soma = 0; int n = 0;
            for (int i = j * porJanela; i < Mathf.Min((j + 1) * porJanela, amostras.Length); i++) { soma += amostras[i] * amostras[i]; n++; }
            envelope[j] = n > 0 ? Mathf.Sqrt((float)(soma / n)) : 0f;
            menor = Mathf.Min(menor, envelope[j]);
            maior = Mathf.Max(maior, envelope[j]);
        }
        limiar = Mathf.Lerp(menor, maior, sensibilidade);

        fonteSom.clip = somFalha;
        fonteSom.loop = true;
        fonteSom.Play();
    }

    void Update()
    {
        // Som longo: a luz segue o áudio
        if (envelope != null)
        {
            int j = Mathf.Clamp(Mathf.FloorToInt(fonteSom.time / JANELA), 0, envelope.Length - 1);
            bool estalo = fonteSom.isPlaying && envelope[j] > limiar;
            if (estalo == acesa) { acesa = !estalo; AplicarEstado(acesa); }
            return;
        }

        // Piscadas sorteadas (o estalo curto toca a cada apagada)
        if (piscadasRestantes <= 0)
        {
            if (Time.time < proximaFalha) return;
            piscadasRestantes = Random.Range(1, piscadasMaximas + 1) * 2;
            trocaEstado = Time.time;
        }

        if (Time.time < trocaEstado) return;

        acesa = !acesa;
        AplicarEstado(acesa);
        if (!acesa && fonteSom != null)
        {
            fonteSom.pitch = Random.Range(0.9f, 1.1f);
            fonteSom.PlayOneShot(somFalha, volumeSom);
        }
        piscadasRestantes--;
        trocaEstado = Time.time + Random.Range(0.06f, 0.18f);

        if (piscadasRestantes <= 0)
        {
            acesa = true;
            AplicarEstado(true);
            proximaFalha = Time.time + Random.Range(intervaloEntreFalhas.x, intervaloEntreFalhas.y);
        }
    }

    /// <summary>Liga ou desliga a luz e o brilho da lâmpada.</summary>
    private void AplicarEstado(bool ligada)
    {
        luz.intensity = ligada ? intensidadeNormal : intensidadeNormal * 0.05f;

        if (lampada == null) return;

        lampada.GetPropertyBlock(bloco);
        bloco.SetColor("_EmissionColor", ligada ? emissaoNormal : Color.black);
        lampada.SetPropertyBlock(bloco);
    }
}
