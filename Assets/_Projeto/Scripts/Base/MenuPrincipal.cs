using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tela inicial do jogo (cena Menu). O fundo é a biblioteca da UVV à noite, com chuva,
/// e o calouro parado do lado de fora. Os botões são Jogar, Opções e Sair.
/// A tela de Opções tem volume, sensibilidade do mouse, tela cheia e a lista de comandos.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    [Header("Painéis")]
    [Tooltip("Grupo com o título e os botões principais.")]
    public CanvasGroup painelPrincipal;
    [Tooltip("Painel de opções (começa escondido).")]
    public CanvasGroup painelOpcoes;
    [Tooltip("Painel preto que cobre a tela no começo e ao sair do menu.")]
    public CanvasGroup telaPreta;

    [Header("Botões")]
    public Button botaoJogar;
    public Button botaoOpcoes;
    public Button botaoSair;
    public Button botaoVoltar;

    [Header("Opções")]
    public Slider sliderVolume;
    public Slider sliderSensibilidade;
    public Toggle toggleTelaCheia;

    [Header("Cena e som")]
    [Tooltip("Cena carregada ao apertar Jogar.")]
    public string primeiraCena = "Nivel1";
    [Tooltip("Música do menu (some aos poucos ao apertar Jogar).")]
    public AudioSource musica;

    [Tooltip("Tempo que a música leva para subir quando o menu abre, em segundos.")]
    public float tempoEntradaMusica = 4f;
    public AudioClip somClique;
    [Range(0f, 1f)] public float volumeClique = 0.7f;

    [Header("Câmera")]
    [Tooltip("Câmera do fundo, que se move bem devagar para a cena parecer viva.")]
    public Transform cameraFundo;
    [Tooltip("Quanto a câmera balança para os lados, em metros.")]
    public float balanco = 0.25f;

    private Vector3 posicaoCamera;
    private Quaternion rotacaoCamera;
    private bool saindo;

    /// <summary>Solta o mouse, lê as opções salvas, liga os botões e clareia a tela.</summary>
    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioListener.volume = Configuracoes.VolumeGeral;

        if (musica != null)
            StartCoroutine(EntrarMusica(musica.volume));

        if (cameraFundo != null)
        {
            posicaoCamera = cameraFundo.position;
            rotacaoCamera = cameraFundo.rotation;
        }

        botaoJogar.onClick.AddListener(Jogar);
        botaoOpcoes.onClick.AddListener(AbrirOpcoes);
        botaoSair.onClick.AddListener(Sair);
        botaoVoltar.onClick.AddListener(FecharOpcoes);

        sliderVolume.value = Configuracoes.VolumeGeral;
        sliderSensibilidade.value = Configuracoes.Sensibilidade;
        toggleTelaCheia.isOn = Configuracoes.TelaCheia;
        sliderVolume.onValueChanged.AddListener(MudarVolume);
        sliderSensibilidade.onValueChanged.AddListener(v => Configuracoes.Sensibilidade = v);
        toggleTelaCheia.onValueChanged.AddListener(v => Configuracoes.TelaCheia = v);

        MostrarPainel(painelOpcoes, false);
        MostrarPainel(painelPrincipal, true);
        StartCoroutine(Fade(telaPreta, 1f, 0f, 2.5f));
    }

    /// <summary>Move a câmera do fundo devagar, num vaivém suave.</summary>
    void Update()
    {
        if (cameraFundo == null)
            return;

        float t = Time.time;
        Vector3 lado = cameraFundo.right * Mathf.Sin(t * 0.12f) * balanco;
        Vector3 cima = Vector3.up * Mathf.Sin(t * 0.09f) * balanco * 0.3f;
        cameraFundo.position = posicaoCamera + lado + cima;
        cameraFundo.rotation = rotacaoCamera * Quaternion.Euler(0f, Mathf.Sin(t * 0.07f) * 0.8f, 0f);

        if (Input.GetKeyDown(KeyCode.Escape) && painelOpcoes.interactable)
            FecharOpcoes();
    }

    /// <summary>Escurece a tela, abaixa a música e carrega o primeiro nível.</summary>
    public void Jogar()
    {
        if (saindo)
            return;
        saindo = true;
        Clique();
        StartCoroutine(SairPara(primeiraCena));
    }

    /// <summary>Esconde os botões principais e mostra as opções.</summary>
    public void AbrirOpcoes()
    {
        Clique();
        MostrarPainel(painelPrincipal, false);
        MostrarPainel(painelOpcoes, true);
    }

    /// <summary>Volta das opções para os botões principais.</summary>
    public void FecharOpcoes()
    {
        Clique();
        MostrarPainel(painelOpcoes, false);
        MostrarPainel(painelPrincipal, true);
    }

    /// <summary>Fecha o jogo (no editor, só para o modo Play).</summary>
    public void Sair()
    {
        Clique();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>Salva o volume e já aplica no jogo.</summary>
    /// <param name="valor">Volume de 0 a 1.</param>
    private void MudarVolume(float valor)
    {
        Configuracoes.VolumeGeral = valor;
        AudioListener.volume = valor;
    }

    /// <summary>Liga ou desliga um painel (visível e clicável ou escondido).</summary>
    /// <param name="painel">Painel a mudar.</param>
    /// <param name="visivel">Verdadeiro para mostrar.</param>
    private void MostrarPainel(CanvasGroup painel, bool visivel)
    {
        painel.alpha = visivel ? 1f : 0f;
        painel.interactable = visivel;
        painel.blocksRaycasts = visivel;
    }

    /// <summary>Toca o som de clique dos botões.</summary>
    private void Clique()
    {
        UIManager.TocarSom2D(somClique, volumeClique);
    }

    /// <summary>Escurece a tela e abaixa a música antes de trocar de cena.</summary>
    /// <param name="cena">Nome da cena a carregar.</param>
    private IEnumerator SairPara(string cena)
    {
        telaPreta.blocksRaycasts = true;
        float volumeMusica = musica != null ? musica.volume : 0f;
        float t = 0f;
        while (t < 1.6f)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 1.6f;
            telaPreta.alpha = k;
            if (musica != null)
                musica.volume = Mathf.Lerp(volumeMusica, 0f, k);
            AudioListener.volume = Mathf.Lerp(Configuracoes.VolumeGeral, 0f, k);
            yield return null;
        }
        SceneManager.LoadScene(cena);
    }

    /// <summary>Muda a transparência de um painel aos poucos.</summary>
    /// <param name="grupo">Painel.</param>
    /// <param name="de">Transparência inicial.</param>
    /// <param name="para">Transparência final.</param>
    /// <param name="duracao">Segundos da transição.</param>
    private IEnumerator Fade(CanvasGroup grupo, float de, float para, float duracao)
    {
        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(de, para, Mathf.SmoothStep(0f, 1f, t / duracao));
            yield return null;
        }
        grupo.alpha = para;
        grupo.blocksRaycasts = para > 0.5f;
    }

    /// <summary>A música do menu começa em silêncio e sobe devagar.</summary>
    /// <param name="volumeFinal">Volume que a música tem no Inspector.</param>
    private System.Collections.IEnumerator EntrarMusica(float volumeFinal)
    {
        musica.volume = 0f;
        float t = 0f;
        while (t < tempoEntradaMusica)
        {
            t += Time.unscaledDeltaTime;
            musica.volume = Mathf.Lerp(0f, volumeFinal, Mathf.SmoothStep(0f, 1f, t / tempoEntradaMusica));
            yield return null;
        }
    }
}
