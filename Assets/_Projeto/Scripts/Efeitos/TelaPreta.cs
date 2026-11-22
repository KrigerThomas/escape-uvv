using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Tela preta com texto, usada para:
///  - a abertura de cada nível: o nome do lugar no meio da tela, piscando como uma lâmpada
///    que demora a acender, e o número do nível pequeno embaixo;
///  - escurecer a tela quando algo acontece (a professora pegou o jogador, a saída do nível).
/// Fica num painel do Canvas com um CanvasGroup (que controla a transparência).
/// Enquanto a abertura está na tela o jogo fica em silêncio; o som do nível entra junto com a imagem.
/// Qualquer tecla pula a abertura.
/// </summary>
public class TelaPreta : MonoBehaviour
{
    /// <summary>Acesso rápido à tela preta da cena.</summary>
    public static TelaPreta Instancia { get; private set; }

    /// <summary>Verdadeiro enquanto a abertura ainda cobre a tela (o som do nível espera).</summary>
    public static bool AberturaNaTela { get; private set; }

    [Tooltip("CanvasGroup do painel preto (Alpha 1 = tudo preto).")]
    public CanvasGroup grupo;

    [Tooltip("Texto no meio da tela preta.")]
    public TMP_Text texto;

    [Tooltip("Texto pequeno embaixo da tela (número do nível).")]
    public TMP_Text textoRodape;

    [Header("Abertura do nível")]
    [Tooltip("Nome do lugar, no meio da tela. Vazio = sem abertura.")]
    public string textoLugar = "";

    [Tooltip("Linha pequena embaixo da tela. Ex.: Nível 1")]
    public string textoNivel = "";

    [Tooltip("Segundos que o nome do lugar fica na tela depois de acender.")]
    public float tempoAbertura = 2.6f;

    [Tooltip("Segundos piscando antes do nome firmar.")]
    public float tempoPiscando = 0.9f;

    [Tooltip("Frase curta quando o nível recomeça (a abertura completa só aparece na primeira vez).")]
    public string textoRecomeco = "De novo...";

    [Tooltip("Segundos da frase de recomeço.")]
    public float tempoRecomeco = 1.2f;

    [Tooltip("Segundos para clarear ou escurecer.")]
    public float tempoTransicao = 1.8f;

    /// <summary>Cenas em que a abertura completa já foi mostrada nesta sessão de jogo.</summary>
    private static readonly System.Collections.Generic.HashSet<string> aberturasVistas = new System.Collections.Generic.HashSet<string>();

    private bool pular;

    /// <summary>Registra a instância. Se houver abertura, o som do nível já começa esperando por ela.</summary>
    void Awake()
    {
        Instancia = this;
        AberturaNaTela = !string.IsNullOrEmpty(textoLugar);

        // no editor a tela fica guardada transparente (para não tampar a Scene); no jogo ela já nasce preta
        if (AberturaNaTela && grupo != null)
            grupo.alpha = 1f;
    }

    /// <summary>Roda a abertura (se houver) ou começa transparente.</summary>
    void Start()
    {
        if (textoRodape != null)
            textoRodape.text = "";

        if (!string.IsNullOrEmpty(textoLugar))
            StartCoroutine(Abertura());
        else
            grupo.alpha = 0f;
    }

    /// <summary>
    /// Abertura do nível: tela preta, nome do lugar acendendo, número do nível embaixo;
    /// depois a imagem do jogo clareia e o jogador é liberado.
    /// </summary>
    private IEnumerator Abertura()
    {
        PlayerController jogador = FindFirstObjectByType<PlayerController>();
        if (jogador != null)
            jogador.enabled = false;

        string cena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool primeiraVez = aberturasVistas.Add(cena);

        grupo.alpha = 1f;
        texto.text = "";
        pular = false;

        if (primeiraVez)
            yield return MostrarNomeDoLugar();
        else
        {
            texto.text = textoRecomeco;
            yield return Esperar(tempoRecomeco);
            texto.text = "";
        }

        if (jogador != null)
            jogador.enabled = true;

        AberturaNaTela = false;
        yield return Fade(1f, 0f);
    }

    /// <summary>
    /// O nome do lugar pisca algumas vezes, como uma lâmpada fluorescente acendendo,
    /// e as letras se aproximam devagar enquanto ele fica na tela.
    /// </summary>
    private IEnumerator MostrarNomeDoLugar()
    {
        yield return Esperar(0.6f);

        texto.text = textoLugar;
        if (textoRodape != null)
            textoRodape.text = textoNivel;

        float espacoInicial = 18f, espacoFinal = 6f;
        float duracao = tempoPiscando + tempoAbertura;
        float t = 0f;
        float proximaTroca = 0f;
        bool aceso = false;

        while (t < duracao && !pular)
        {
            t += Time.unscaledDeltaTime;

            // Piscando no começo, depois firme
            if (t < tempoPiscando)
            {
                if (t >= proximaTroca)
                {
                    aceso = !aceso;
                    proximaTroca = t + (aceso ? Random.Range(0.05f, 0.14f) : Random.Range(0.04f, 0.2f));
                }
                texto.alpha = aceso ? Random.Range(0.55f, 0.9f) : 0.05f;
            }
            else
            {
                texto.alpha = Mathf.MoveTowards(texto.alpha, 1f, Time.unscaledDeltaTime * 3f);
            }

            texto.characterSpacing = Mathf.Lerp(espacoInicial, espacoFinal, Mathf.SmoothStep(0f, 1f, t / duracao));
            if (textoRodape != null)
                textoRodape.alpha = Mathf.Clamp01((t - tempoPiscando * 0.5f) / 1f) * 0.8f;

            if (t > 0.4f && Input.anyKeyDown)
                pular = true;
            yield return null;
        }

        // Os textos somem antes de clarear
        float inicioTexto = texto.alpha;
        float inicioRodape = textoRodape != null ? textoRodape.alpha : 0f;
        float s = 0f;
        while (s < 0.7f)
        {
            s += Time.unscaledDeltaTime;
            texto.alpha = Mathf.Lerp(inicioTexto, 0f, s / 0.7f);
            if (textoRodape != null)
                textoRodape.alpha = Mathf.Lerp(inicioRodape, 0f, s / 0.7f);
            yield return null;
        }

        texto.text = "";
        texto.alpha = 1f;
        texto.characterSpacing = 0f;
        if (textoRodape != null)
        {
            textoRodape.text = "";
            textoRodape.alpha = 1f;
        }
    }

    /// <summary>Espera alguns segundos em tempo real; uma tecla apertada encurta a espera.</summary>
    /// <param name="segundos">Tempo de espera.</param>
    private IEnumerator Esperar(float segundos)
    {
        float t = 0f;
        while (t < segundos && !pular)
        {
            t += Time.unscaledDeltaTime;
            if (t > 0.3f && Input.anyKeyDown)
                pular = true;
            yield return null;
        }
    }

    /// <summary>Escurece a tela e mostra uma frase (chamado de outros scripts).</summary>
    /// <param name="frase">Texto que aparece no meio da tela preta.</param>
    public void Escurecer(string frase)
    {
        StopAllCoroutines();
        texto.alpha = 1f;
        texto.characterSpacing = 0f;
        texto.text = frase;
        StartCoroutine(Fade(grupo.alpha, 1f));
        StartCoroutine(AbaixarVolumeGeral());
    }

    /// <summary>Corta para a tela preta na hora e silencia tudo (fim do susto).</summary>
    /// <param name="frase">Texto que aparece no meio da tela preta.</param>
    public void EscurecerNaHora(string frase)
    {
        StopAllCoroutines();
        grupo.alpha = 1f;
        texto.alpha = 1f;
        texto.characterSpacing = 0f;
        texto.text = frase;
        AudioListener.volume = 0f;
    }

    /// <summary>Todo o som do jogo vai sumindo enquanto a tela escurece.</summary>
    private IEnumerator AbaixarVolumeGeral()
    {
        float inicio = AudioListener.volume;
        float duracao = tempoTransicao * 1.5f;
        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            AudioListener.volume = Mathf.Lerp(inicio, 0f, t / duracao);
            yield return null;
        }
        AudioListener.volume = 0f;
    }

    /// <summary>Muda a transparência do painel preto aos poucos.</summary>
    /// <param name="de">Transparência inicial (0 a 1).</param>
    /// <param name="para">Transparência final (0 a 1).</param>
    private IEnumerator Fade(float de, float para)
    {
        float t = 0f;
        while (t < tempoTransicao)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(de, para, Mathf.SmoothStep(0f, 1f, t / tempoTransicao));
            yield return null;
        }
        grupo.alpha = para;
    }

    /// <summary>Limpa a referência ao trocar de cena.</summary>
    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
        AberturaNaTela = false;
    }
}
