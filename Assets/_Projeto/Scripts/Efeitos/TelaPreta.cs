using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Tela preta com texto, usada para:
///  - a abertura de cada nível (tela preta com o nome do lugar e um pensamento do calouro);
///  - escurecer a tela quando algo acontece (ex.: a professora pegou o jogador, a saída do nível).
/// Fica num painel do Canvas com um CanvasGroup (que controla a transparência).
///
/// Como escrever a abertura (campo Texto Abertura):
///  - uma linha em branco separa os blocos: cada bloco aparece alguns segundos depois do anterior;
///  - uma linha só com --- troca de tela (o texto some e começa a próxima tela).
/// Qualquer tecla pula a abertura.
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
    [Tooltip("Frase mostrada no começo. Vazio = sem abertura. Linha em branco = próximo bloco. Linha com --- = próxima tela.")]
    [TextArea(4, 12)] public string textoAbertura = "";

    [Tooltip("Segundos que a última frase de cada tela fica parada antes de seguir.")]
    public float tempoAbertura = 2.5f;

    [Tooltip("Segundos entre um bloco de texto e o próximo.")]
    public float tempoPorBloco = 1.6f;

    [Tooltip("Segundos para cada bloco de texto aparecer.")]
    public float tempoFadeBloco = 0.8f;

    [Tooltip("Frase curta quando o nível recomeça (a abertura completa só aparece na primeira vez).")]
    public string textoRecomeco = "De novo";

    [Tooltip("Segundos da frase de recomeço.")]
    public float tempoRecomeco = 1.2f;

    [Tooltip("Segundos para clarear ou escurecer.")]
    public float tempoTransicao = 1.5f;

    /// <summary>Cenas em que a abertura completa já foi mostrada nesta sessão de jogo.</summary>
    private static readonly HashSet<string> aberturasVistas = new HashSet<string>();

    private bool pularAbertura;
    private int[] blocoDoCaractere;
    private Color32[][] coresOriginais;

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

    /// <summary>
    /// Abertura do nível: espera o menu inicial fechar (só existe no Nível 1), segura o jogador,
    /// mostra as telas de texto e depois clareia.
    /// </summary>
    private IEnumerator Abertura()
    {
        PlayerController jogador = FindFirstObjectByType<PlayerController>();
        UIManager ui = FindFirstObjectByType<UIManager>();

        // No Nível 1 o menu aparece primeiro: a tela preta só entra depois do "Começar"
        if (ui != null && ui.MenuInicialNaTela)
        {
            grupo.alpha = 0f;
            texto.text = "";
            while (ui.MenuInicialNaTela)
                yield return null;
        }

        if (jogador != null)
            jogador.enabled = false;

        string cena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool primeiraVez = aberturasVistas.Add(cena);

        grupo.alpha = 1f;
        pularAbertura = false;

        if (primeiraVez)
        {
            string[] telas = textoAbertura.Replace("\r", "").Split(new[] { "\n---\n", "\n---", "---\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string tela in telas)
            {
                yield return MostrarTela(tela.Trim('\n'));
                if (pularAbertura)
                    break;
            }
        }
        else
        {
            texto.text = textoRecomeco;
            yield return new WaitForSecondsRealtime(tempoRecomeco);
        }

        if (jogador != null)
            jogador.enabled = true;

        yield return Fade(1f, 0f);
        texto.text = "";
    }

    /// <summary>
    /// Mostra uma tela da abertura: os blocos aparecem um de cada vez, a tela fica parada
    /// um pouco e o texto some. Uma tecla apertada pula o resto da abertura.
    /// </summary>
    /// <param name="tela">Texto da tela (blocos separados por linha em branco).</param>
    private IEnumerator MostrarTela(string tela)
    {
        PrepararBlocos(tela);
        int total = blocoDoCaractere.Length == 0 ? 1 : blocoDoCaractere[blocoDoCaractere.Length - 1] + 1;
        float[] alfas = new float[total];

        float duracao = (total - 1) * tempoPorBloco + tempoFadeBloco + tempoAbertura;
        float t = 0f;
        while (t < duracao && !pularAbertura)
        {
            t += Time.unscaledDeltaTime;
            for (int b = 0; b < total; b++)
                alfas[b] = Mathf.Clamp01((t - b * tempoPorBloco) / tempoFadeBloco);
            AplicarAlfas(alfas);

            if (t > 0.5f && Input.anyKeyDown)
                pularAbertura = true;
            yield return null;
        }

        // o texto inteiro some antes da próxima tela (ou antes de clarear)
        float inicio = 1f;
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(inicio, 0f, t / 0.5f);
            for (int b = 0; b < total; b++)
                alfas[b] = Mathf.Min(alfas[b], a);
            AplicarAlfas(alfas);
            yield return null;
        }
        texto.text = "";
    }

    /// <summary>
    /// Escreve a tela inteira no texto e descobre a qual bloco pertence cada letra
    /// (um bloco termina numa linha em branco). Guarda as cores originais das letras.
    /// </summary>
    /// <param name="tela">Texto da tela.</param>
    private void PrepararBlocos(string tela)
    {
        texto.text = tela;
        texto.ForceMeshUpdate();
        TMP_TextInfo info = texto.textInfo;

        blocoDoCaractere = new int[info.characterCount];
        int bloco = 0;
        char anterior = '\0';
        for (int i = 0; i < info.characterCount; i++)
        {
            char c = info.characterInfo[i].character;
            if (c == '\n' && anterior == '\n')
                bloco++;
            blocoDoCaractere[i] = bloco;
            anterior = c;
        }

        coresOriginais = new Color32[info.meshInfo.Length][];
        for (int m = 0; m < info.meshInfo.Length; m++)
            coresOriginais[m] = (Color32[])info.meshInfo[m].colors32.Clone();
    }

    /// <summary>Aplica a transparência de cada bloco nas letras dele.</summary>
    /// <param name="alfas">Transparência de cada bloco (0 = invisível, 1 = normal).</param>
    private void AplicarAlfas(float[] alfas)
    {
        TMP_TextInfo info = texto.textInfo;
        for (int i = 0; i < info.characterCount && i < blocoDoCaractere.Length; i++)
        {
            TMP_CharacterInfo ci = info.characterInfo[i];
            if (!ci.isVisible)
                continue;

            int m = ci.materialReferenceIndex;
            int v = ci.vertexIndex;
            Color32[] cores = info.meshInfo[m].colors32;
            if (m >= coresOriginais.Length || v + 3 >= cores.Length || v + 3 >= coresOriginais[m].Length)
                continue;

            float a = alfas[Mathf.Min(blocoDoCaractere[i], alfas.Length - 1)];
            for (int k = 0; k < 4; k++)
                cores[v + k].a = (byte)(coresOriginais[m][v + k].a * a);
        }
        texto.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    /// <summary>Escurece a tela e mostra uma frase (chamado de outros scripts).</summary>
    /// <param name="frase">Texto que aparece no meio da tela preta.</param>
    public void Escurecer(string frase)
    {
        StopAllCoroutines();
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
