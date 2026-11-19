using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Tela preta com texto, usada para:
///  - a abertura do nível (começa preta com uma frase e vai clareando);
///  - escurecer a tela quando algo acontece (ex.: a professora pegou o jogador).
/// Fica num painel do Canvas com um CanvasGroup (que controla a transparência).
/// </summary>
public class TelaPreta : MonoBehaviour
{
    /// <summary>Acesso rápido à tela preta da cena.</summary>
    public static TelaPreta Instancia { get; private set; }

    [Tooltip("CanvasGroup do painel preto (Alpha 1 = tudo preto).")]
    public CanvasGroup grupo;

    [Tooltip("Texto no meio da tela preta.")]
    public TMP_Text texto;

    [Header("Abertura do nível")]
    [Tooltip("Frase mostrada no começo. Vazio = sem abertura.")]
    [TextArea(2, 4)] public string textoAbertura = "";

    [Tooltip("Segundos com a tela preta antes de clarear.")]
    public float tempoAbertura = 4f;

    [Tooltip("Frase curta quando o nível recomeça (a abertura completa só aparece na primeira vez).")]
    public string textoRecomeco = "De novo...";

    [Tooltip("Segundos da frase de recomeço.")]
    public float tempoRecomeco = 1.2f;

    /// <summary>Cenas em que a abertura completa já foi mostrada nesta sessão de jogo.</summary>
    private static readonly System.Collections.Generic.HashSet<string> aberturasVistas = new System.Collections.Generic.HashSet<string>();

    [Tooltip("Segundos para clarear ou escurecer.")]
    public float tempoTransicao = 1.5f;

    /// <summary>Registra a instância.</summary>
    void Awake()
    {
        Instancia = this;
    }

    /// <summary>Roda a abertura (se houver) ou começa transparente.</summary>
    void Start()
    {
        if (!string.IsNullOrEmpty(textoAbertura))
            StartCoroutine(Abertura());
        else
            grupo.alpha = 0f;
    }

    /// <summary>Tela preta com a frase, jogador parado; depois clareia e libera o jogador.</summary>
    private IEnumerator Abertura()
    {
        PlayerController jogador = FindFirstObjectByType<PlayerController>();
        if (jogador != null)
            jogador.enabled = false;

        string cena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool primeiraVez = aberturasVistas.Add(cena);

        grupo.alpha = 1f;
        texto.text = primeiraVez ? textoAbertura : textoRecomeco;
        yield return new WaitForSeconds(primeiraVez ? tempoAbertura : tempoRecomeco);

        if (jogador != null)
            jogador.enabled = true;

        yield return Fade(1f, 0f);
        texto.text = "";
    }

    /// <summary>Escurece a tela e mostra uma frase (chamado de outros scripts).</summary>
    public void Escurecer(string frase)
    {
        StopAllCoroutines();
        texto.text = frase;
        StartCoroutine(Fade(grupo.alpha, 1f));
    }

    /// <summary>Muda a transparência aos poucos.</summary>
    private IEnumerator Fade(float de, float para)
    {
        float t = 0f;
        while (t < tempoTransicao)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(de, para, t / tempoTransicao);
            yield return null;
        }
        grupo.alpha = para;
    }

    /// <summary>Limpa a referência ao trocar de cena.</summary>
    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }
}
