using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interface do nível: mensagem de objetivo, contador de itens, avisos rápidos,
/// botão Reiniciar e painel de introdução (mostrado só na primeira vez).
/// </summary>
public class UIManager : MonoBehaviour
{
    /// <summary>Verdadeiro enquanto um painel que precisa do mouse está aberto.</summary>
    public static bool MenuAberto { get; private set; }

    /// <summary>Lembra se a introdução já foi vista nesta sessão de jogo.</summary>
    private static bool introJaVista;

    [Header("HUD")]
    public TMP_Text textoObjetivo;
    public TMP_Text textoContador;
    public TMP_Text textoAviso;
    public Button botaoReiniciar;

    [Header("Introdução")]
    [Tooltip("Painel com o texto de introdução e o botão Continuar. Deixe vazio em níveis sem introdução.")]
    public GameObject painelIntro;
    public Button botaoContinuar;

    [Tooltip("Música que toca só enquanto a introdução está aberta. Some aos poucos ao clicar em Continuar.")]
    public AudioSource musicaIntro;

    [Tooltip("Tempo, em segundos, para a música da introdução sumir.")]
    public float tempoFadeMusica = 1.5f;

    [Header("Sons")]
    [Tooltip("Som de clique tocado pelos botões.")]
    public AudioClip somClique;

    [Tooltip("Tempo que um aviso fica na tela, em segundos.")]
    public float duracaoAviso = 3f;

    private Coroutine rotinaAviso;

    /// <summary>Liga os botões e decide se a introdução aparece.</summary>
    void Start()
    {
        if (botaoReiniciar != null)
        {
            botaoReiniciar.onClick.AddListener(TocarClique);
            botaoReiniciar.onClick.AddListener(Reiniciar);
        }

        if (botaoContinuar != null)
        {
            botaoContinuar.onClick.AddListener(TocarClique);
            botaoContinuar.onClick.AddListener(FecharIntro);
        }

        if (textoAviso != null)
            textoAviso.text = "";

        if (painelIntro != null && !introJaVista)
            AbrirIntro();
        else
            FecharIntro();
    }

    /// <summary>Garante que o jogo não fique pausado se a cena for trocada com um painel aberto.</summary>
    void OnDestroy()
    {
        MenuAberto = false;
        Time.timeScale = 1f;
    }

    /// <summary>Troca o texto de objetivo no topo da tela.</summary>
    /// <param name="texto">Novo objetivo.</param>
    public void MostrarObjetivo(string texto)
    {
        if (textoObjetivo != null)
            textoObjetivo.text = texto;
    }

    /// <summary>Atualiza o contador, por exemplo "Itens: 2 / 5".</summary>
    /// <param name="rotulo">Palavra antes dos números.</param>
    /// <param name="atual">Quantidade coletada.</param>
    /// <param name="total">Quantidade necessária.</param>
    public void AtualizarContador(string rotulo, int atual, int total)
    {
        if (textoContador != null)
            textoContador.text = rotulo + ": " + atual + " / " + total;
    }

    /// <summary>Mostra um aviso curto no meio da tela, que some sozinho.</summary>
    /// <param name="texto">Mensagem do aviso.</param>
    public void MostrarAviso(string texto)
    {
        if (textoAviso == null)
            return;

        if (rotinaAviso != null)
            StopCoroutine(rotinaAviso);

        rotinaAviso = StartCoroutine(RotinaAviso(texto));
    }

    /// <summary>Exibe o aviso, espera e apaga.</summary>
    /// <param name="texto">Mensagem do aviso.</param>
    private IEnumerator RotinaAviso(string texto)
    {
        textoAviso.text = texto;
        yield return new WaitForSeconds(duracaoAviso);
        textoAviso.text = "";
    }

    /// <summary>Abre a introdução, pausa o jogo e solta o cursor do mouse.</summary>
    public void AbrirIntro()
    {
        painelIntro.SetActive(true);
        MenuAberto = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (musicaIntro != null && !musicaIntro.isPlaying)
            musicaIntro.Play();
    }

    /// <summary>Fecha a introdução, despausa o jogo e prende o cursor de novo.</summary>
    public void FecharIntro()
    {
        if (painelIntro != null)
            painelIntro.SetActive(false);

        introJaVista = true;
        MenuAberto = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (musicaIntro != null && musicaIntro.isPlaying)
            StartCoroutine(SumirMusica());
    }

    /// <summary>Abaixa o volume da música da introdução até zero e para a música.</summary>
    private IEnumerator SumirMusica()
    {
        float volumeInicial = musicaIntro.volume;
        float t = 0f;

        while (t < tempoFadeMusica)
        {
            t += Time.unscaledDeltaTime;
            musicaIntro.volume = Mathf.Lerp(volumeInicial, 0f, t / tempoFadeMusica);
            yield return null;
        }

        musicaIntro.Stop();
        musicaIntro.volume = volumeInicial;
    }

    /// <summary>Toca o som de clique dos botões (funciona mesmo com o jogo pausado).</summary>
    private void TocarClique()
    {
        if (somClique != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(somClique, Camera.main.transform.position, 0.7f);
    }

    /// <summary>Ação do botão Reiniciar.</summary>
    private void Reiniciar()
    {
        if (GameManager.Instancia != null)
            GameManager.Instancia.ReiniciarNivel();
    }
}
