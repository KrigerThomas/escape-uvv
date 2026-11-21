using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O que acontece na biblioteca (Nível 4) conforme o jogador pega os livros:
///  - a cada livro, as luzes piscam e cai um raio perto, com trovão;
///  - no último livro, a energia cai: tudo apaga, o zumbido das lâmpadas some, só ficam
///    as luzes de emergência e a chuva lá fora. A catraca destrava e o jogador só precisa
///    pegar a mochila e sair.
/// Percebe cada livro pelo contador do GameManager (não precisa mexer no script dos livros).
/// </summary>
public class EventosBiblioteca : MonoBehaviour
{
    [Header("Luzes")]
    [Tooltip("Objeto com todas as luminárias de dentro (luzes e lâmpadas acesas).")]
    public Transform luminarias;

    [Tooltip("Luzes de emergência: ficam desligadas até a energia cair.")]
    public GameObject luzesEmergencia;

    [Tooltip("Objetos que somem quando a energia cai (ex.: telas acesas dos computadores).")]
    public GameObject[] esconderNoApagao;

    [Tooltip("Luz do ambiente depois que a energia cai (fração da luz normal).")]
    [Range(0f, 1f)] public float ambienteNoApagao = 0.12f;

    [Tooltip("Reflexo do ambiente no chão e nos vidros depois que a energia cai (fração do normal). Sem isso o escuro fica cinza.")]
    [Range(0f, 1f)] public float reflexoNoApagao = 0.3f;

    [Header("Sons")]
    public AudioSource zumbido;

    [Tooltip("Som de quando a energia cai.")]
    public AudioClip somQuedaEnergia;

    [Range(0f, 1f)] public float volumeQuedaEnergia = 0.8f;

    [Tooltip("O som da queda de energia começa devagar (fade in). Ele toca estes segundos ANTES das luzes apagarem de vez, para o 'tum' bater junto com o escuro.")]
    public float adiantarSomQueda = 0.7f;

    [Tooltip("Som das lâmpadas falhando enquanto as luzes piscam.")]
    public AudioClip somLuzFalhando;

    [Range(0f, 1f)] public float volumeLuzFalhando = 0.6f;

    [Tooltip("Scripts desligados quando a energia cai (ex.: livros caindo ao longe).")]
    public Behaviour[] desligarNoApagao;

    [Tooltip("Sons de fora que ficam mais baixos no apagão (chuva, trovões).")]
    public AudioSource[] abaixarNoApagao;

    [Range(0f, 1f)] public float fracaoSomFora = 0.5f;

    [Header("Saída sem energia")]
    public CatracaFacial catraca;
    public GuardaVolumes guardaVolumes;
    public PortaComChave portaSaida;

    [TextArea] public string avisoApagao = "A energia caiu";
    [TextArea] public string objetivoApagao = "Pegue a mochila no armário 23 e saia da biblioteca";

    private readonly List<Light> luzes = new List<Light>();
    private Renderer[] lampadas;
    private MaterialPropertyBlock apagada;
    private int faltandoAntes = -1;
    private bool apagou;
    private AudioSource fonteLuz;

    /// <summary>Guarda as luzes acesas e deixa a emergência desligada.</summary>
    void Start()
    {
        if (luminarias != null)
        {
            foreach (Light l in luminarias.GetComponentsInChildren<Light>())
                if (l.enabled)
                    luzes.Add(l);
            lampadas = luminarias.GetComponentsInChildren<Renderer>();
        }

        apagada = new MaterialPropertyBlock();
        apagada.SetColor("_EmissionColor", Color.black);

        if (luzesEmergencia != null)
            luzesEmergencia.SetActive(false);

        fonteLuz = gameObject.AddComponent<AudioSource>();
        fonteLuz.playOnAwake = false;
        fonteLuz.spatialBlend = 0f;
    }

    /// <summary>Confere se o jogador acabou de pegar um livro.</summary>
    void Update()
    {
        GameManager gm = GameManager.Instancia;
        if (gm == null || apagou)
            return;

        int faltando = gm.ItensFaltando;
        if (faltandoAntes < 0)
            faltandoAntes = faltando;

        if (faltando < faltandoAntes)
        {
            faltandoAntes = faltando;
            if (Tempestade.Instancia != null)
                Tempestade.Instancia.Relampago(true);

            if (faltando == 0)
                StartCoroutine(Apagao());
            else
                StartCoroutine(Piscar(1.0f));
        }
    }

    /// <summary>As luzes piscam por alguns instantes e voltam.</summary>
    private IEnumerator Piscar(float duracao)
    {
        if (somLuzFalhando != null)
        {
            fonteLuz.Stop();
            fonteLuz.volume = volumeLuzFalhando;
            fonteLuz.pitch = Random.Range(0.95f, 1.05f);
            fonteLuz.clip = somLuzFalhando;
            fonteLuz.Play();
        }

        float t = 0f;
        while (t < duracao)
        {
            Ligar(Random.value > 0.55f);
            float espera = Random.Range(0.04f, 0.13f);
            t += espera;
            yield return new WaitForSeconds(espera);
        }
        Ligar(true);
        StartCoroutine(SumirSomLuz());
    }

    /// <summary>Abaixa rapidinho o som das lâmpadas falhando quando elas param de piscar.</summary>
    private IEnumerator SumirSomLuz()
    {
        float inicio = fonteLuz.volume;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.15f)
        {
            fonteLuz.volume = Mathf.Lerp(inicio, 0f, t);
            yield return null;
        }
        fonteLuz.Stop();
    }

    /// <summary>
    /// Toca o som da queda de energia depois de um tempo.
    /// </summary>
    /// <param name="espera">Segundos até tocar.</param>
    private IEnumerator TocarQuedaDepois(float espera)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, espera));
        UIManager.TocarSom2D(somQuedaEnergia, volumeQuedaEnergia);
    }

    /// <summary>A energia cai: luzes apagam, sons somem e a saída fica liberada.</summary>
    private IEnumerator Apagao()
    {
        apagou = true;
        const float tempoPiscando = 1.4f;
        StartCoroutine(TocarQuedaDepois(tempoPiscando - adiantarSomQueda));
        yield return Piscar(tempoPiscando);
        Ligar(false);
        foreach (GameObject g in esconderNoApagao)
            if (g != null)
                g.SetActive(false);

        if (Tempestade.Instancia != null)
            Tempestade.Instancia.ativa = false;
        foreach (Behaviour b in desligarNoApagao)
            if (b != null)
                b.enabled = false;

        RenderSettings.ambientLight *= ambienteNoApagao;
        RenderSettings.ambientSkyColor *= ambienteNoApagao;
        RenderSettings.ambientEquatorColor *= ambienteNoApagao;
        RenderSettings.ambientGroundColor *= ambienteNoApagao;
        RenderSettings.reflectionIntensity *= reflexoNoApagao;
        foreach (ReflectionProbe sonda in FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
            sonda.intensity *= reflexoNoApagao;

        if (catraca != null)
            catraca.Desligar();
        if (guardaVolumes != null)
            guardaVolumes.itemEmprestimo = "";
        if (portaSaida != null)
            portaSaida.segundoItemNecessario = "";

        if (GameManager.Instancia != null && GameManager.Instancia.ui != null)
        {
            GameManager.Instancia.ui.MostrarAviso(avisoApagao);
            GameManager.Instancia.ui.MostrarObjetivo(objetivoApagao);
        }

        yield return new WaitForSeconds(0.6f);
        if (luzesEmergencia != null)
            luzesEmergencia.SetActive(true);

        // Zumbido e sons de fora vão sumindo; fica o silêncio com a chuva baixinha.
        float[] volumes = new float[abaixarNoApagao.Length];
        for (int i = 0; i < volumes.Length; i++)
            volumes[i] = abaixarNoApagao[i] != null ? abaixarNoApagao[i].volume : 0f;
        float inicioZumbido = zumbido != null ? zumbido.volume : 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 2f;
            if (zumbido != null)
                zumbido.volume = Mathf.Lerp(inicioZumbido, 0f, t);
            for (int i = 0; i < volumes.Length; i++)
                if (abaixarNoApagao[i] != null)
                    abaixarNoApagao[i].volume = Mathf.Lerp(volumes[i], volumes[i] * fracaoSomFora, t);
            yield return null;
        }
        if (zumbido != null)
            zumbido.Stop();
    }

    /// <summary>Liga ou desliga todas as luzes e o brilho das lâmpadas.</summary>
    private void Ligar(bool acesas)
    {
        foreach (Light l in luzes)
            if (l != null)
                l.enabled = acesas;

        if (lampadas == null)
            return;
        foreach (Renderer r in lampadas)
            if (r != null)
                r.SetPropertyBlock(acesas ? null : apagada);
    }
}
