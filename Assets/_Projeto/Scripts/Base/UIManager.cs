using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Interface do nível (HUD). A ideia é ficar o mais limpa possível: nada fica fixo na tela.
/// - Objetivo e contador aparecem no canto superior esquerdo quando mudam e somem sozinhos
///   (segure Tab para ver de novo).
/// - A ação possível ("E  Guardar mochila") aparece embaixo, no centro, só perto de algo interativo.
/// - Avisos aparecem como legenda, um pouco acima da ação.
/// - Mensagens dos colegas chegam como notificação de celular no canto superior direito.
/// - Esc abre a pausa (Continuar, Reiniciar nível, Sair). O botão de reiniciar pedido no enunciado fica aqui.
/// Todas as cores vêm da classe Paleta.
/// </summary>
public class UIManager : MonoBehaviour
{
    /// <summary>Verdadeiro enquanto um painel que precisa do mouse está aberto (pausa, quadro, lista...).</summary>
    public static bool MenuAberto { get; private set; }

    /// <summary>Verdadeiro enquanto o menu de pausa está aberto.</summary>
    public static bool Pausado { get; private set; }

    /// <summary>Lembra se a introdução já foi vista nesta sessão de jogo.</summary>
    private static bool introJaVista;

    /// <summary>Quadro em que um painel foi fechado (o mesmo Esc não pode fechar o painel e abrir a pausa).</summary>
    private static int quadroPainelFechado = -1;

    [Header("Objetivo (canto superior esquerdo)")]
    [Tooltip("Grupo com o nome do local, o objetivo e o contador. Aparece e some sozinho.")]
    public CanvasGroup grupoObjetivo;
    [Tooltip("Linha pequena com o nome do lugar (ex.: Biblioteca UVV).")]
    public TMP_Text textoLocal;
    [Tooltip("Nome do lugar deste nível. Vazio = a linha não aparece.")]
    public string nomeDoLocal = "";
    public TMP_Text textoObjetivo;
    public TMP_Text textoContador;
    [Tooltip("Quantos segundos o objetivo fica na tela depois de mudar.")]
    public float tempoObjetivoNaTela = 7f;
    [Tooltip("Segurando esta tecla, o objetivo aparece de novo.")]
    public KeyCode teclaObjetivo = KeyCode.Tab;

    [Header("Ação (embaixo, no centro)")]
    [Tooltip("Grupo com a tecla e o nome da ação. Os scripts de interação ligam e desligam.")]
    public CanvasGroup grupoAcao;
    public GameObject caixaTecla;
    public TMP_Text textoTecla;
    public TMP_Text textoAcao;

    [Header("Legado (não usar em coisas novas)")]
    [Tooltip("Caixa antiga de dica. Se estiver preenchida, é escondida no início.")]
    public GameObject caixaDica;
    public TMP_Text textoDica;

    [Header("Legenda / avisos")]
    public TMP_Text textoAviso;
    public CanvasGroup grupoAviso;
    [Tooltip("Tempo MÍNIMO que um aviso fica na tela, em segundos.")]
    public float duracaoAviso = 3f;
    [Tooltip("Tempo extra por letra: avisos longos ficam mais tempo na tela para dar tempo de ler.")]
    public float segundosPorLetra = 0.07f;

    [Header("Mensagem no celular (canto superior direito)")]
    public CanvasGroup grupoMensagem;
    public RectTransform caixaMensagem;
    public Image iconeRemetente;
    public TMP_Text textoIniciais;
    public TMP_Text textoRemetente;
    public TMP_Text textoMensagem;
    [Tooltip("Som curto tocado quando chega uma mensagem.")]
    public AudioClip somMensagem;

    [Header("Pausa (Esc)")]
    public GameObject painelPausa;
    public Button botaoContinuarPausa;
    public Button botaoReiniciar;
    public Button botaoSair;
    [Tooltip("Placa azul da pausa: número do nível.")]
    public TMP_Text textoPausaNivel;
    [Tooltip("Placa azul da pausa: nome do lugar.")]
    public TMP_Text textoPausaLocal;
    public TMP_Text textoPausaObjetivo;

    [Header("Introdução")]
    [Tooltip("Painel de abertura do jogo (só no Nível 1). Deixe vazio em níveis sem introdução.")]
    public GameObject painelIntro;
    public Button botaoContinuar;
    [Tooltip("Música que toca só enquanto a introdução está aberta.")]
    public AudioSource musicaIntro;
    [Tooltip("Tempo, em segundos, para a música da introdução sumir.")]
    public float tempoFadeMusica = 1.5f;

    [Header("Sons")]
    [Tooltip("Som de clique tocado pelos botões.")]
    public AudioClip somClique;

    // Estado interno
    private float objetivoAte;          // até quando (tempo real) o objetivo fica visível
    private float avisoAte;             // até quando a legenda fica visível
    private bool acaoVisivel;
    private string objetivoAtual = "";
    private string contadorAtual = "";
    private int ultimoValorContador = -1;

    private class Mensagem { public string remetente, texto; }
    private readonly Queue<Mensagem> filaMensagens = new Queue<Mensagem>();
    private int etapaMensagem;          // 0 parada, 1 entrando, 2 na tela, 3 saindo
    private float tempoEtapa;
    private float duracaoMensagem;
    private Vector2 posicaoMensagem;

    private const float tempoFade = 0.25f;
    private int quadrosDesdeInicio;    // conta os primeiros quadros (o carregamento da cena não conta como tempo de leitura)

    /// <summary>Liga os botões, esconde tudo e decide se a introdução aparece.</summary>
    void Start()
    {
        Ligar(botaoContinuar, FecharIntro);
        Ligar(botaoContinuarPausa, FecharPausa);
        Ligar(botaoReiniciar, Reiniciar);
        Ligar(botaoSair, Sair);

        if (caixaDica != null)
            caixaDica.SetActive(false);
        if (painelPausa != null)
            painelPausa.SetActive(false);
        if (textoAviso != null)
            textoAviso.text = "";
        if (textoLocal != null)
        {
            textoLocal.text = nomeDoLocal;
            textoLocal.gameObject.SetActive(!string.IsNullOrEmpty(nomeDoLocal));
        }
        if (caixaMensagem != null)
            posicaoMensagem = caixaMensagem.anchoredPosition;

        DefinirAlfa(grupoObjetivo, 0f);
        DefinirAlfa(grupoAcao, 0f);
        DefinirAlfa(grupoAviso, 0f);
        DefinirAlfa(grupoMensagem, 0f);

        if (painelIntro != null && !introJaVista)
            AbrirIntro();
        else
            FecharIntro();
    }

    /// <summary>Volta tudo ao normal quando a cena é trocada ou reiniciada.</summary>
    void OnDestroy()
    {
        MenuAberto = false;
        Pausado = false;
        AudioListener.pause = false;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// A cada quadro: confere o Esc (pausa), o Tab (objetivo) e faz os elementos
    /// aparecerem e sumirem suavemente. Usa tempo real, então funciona com o jogo pausado.
    /// </summary>
    void Update()
    {
        // Nos primeiros quadros depois de carregar a cena o relógio dá um salto; o objetivo só começa a contar depois.
        if (quadrosDesdeInicio < 3)
        {
            quadrosDesdeInicio++;
            if (!string.IsNullOrEmpty(objetivoAtual))
                objetivoAte = Time.unscaledTime + tempoObjetivoNaTela;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Pausado)
                FecharPausa();
            else if (PodePausar())
                AbrirPausa();
        }

        bool telaEscura = TelaPreta.Instancia != null && TelaPreta.Instancia.grupo != null && TelaPreta.Instancia.grupo.alpha > 0.5f;
        if (telaEscura)
            objetivoAte = Mathf.Max(objetivoAte, Time.unscaledTime + tempoObjetivoNaTela);

        bool livre = !MenuAberto && !telaEscura;
        bool verObjetivo = livre && !string.IsNullOrEmpty(objetivoAtual)
            && (Time.unscaledTime < objetivoAte || Input.GetKey(teclaObjetivo));

        Aproximar(grupoObjetivo, verObjetivo ? 1f : 0f);
        Aproximar(grupoAcao, livre && acaoVisivel ? 1f : 0f);
        Aproximar(grupoAviso, Time.unscaledTime < avisoAte ? 1f : 0f);

        AtualizarMensagens(telaEscura);
    }

    // ---------------- Objetivo e contador ----------------

    /// <summary>
    /// Troca o objetivo e mostra por alguns segundos.
    /// </summary>
    /// <param name="texto">Frase do objetivo, curta e direta (ex.: "Guarde a mochila no guarda-volumes").</param>
    public void MostrarObjetivo(string texto)
    {
        objetivoAtual = texto ?? "";
        if (textoObjetivo != null)
            textoObjetivo.text = objetivoAtual;
        objetivoAte = Time.unscaledTime + tempoObjetivoNaTela;
    }

    /// <summary>
    /// Atualiza o contador (ex.: "Livros 1 de 3"). Quando o número muda, o objetivo aparece de novo.
    /// </summary>
    /// <param name="rotulo">Nome do que está sendo contado. Vazio = sem contador.</param>
    /// <param name="atual">Quantos já foram encontrados.</param>
    /// <param name="total">Quantos existem. Zero ou menos = sem contador.</param>
    public void AtualizarContador(string rotulo, int atual, int total)
    {
        if (textoContador == null)
            return;

        if (string.IsNullOrEmpty(rotulo) || total <= 0)
        {
            contadorAtual = "";
            textoContador.text = "";
            return;
        }

        string cinza = ColorUtility.ToHtmlStringRGB(Paleta.Cinza);
        string destaque = ColorUtility.ToHtmlStringRGB(atual >= total ? Paleta.Verde : Paleta.Papel);
        contadorAtual = rotulo + " " + atual + " de " + total;
        textoContador.text = "<color=#" + cinza + ">" + rotulo + "</color>  <color=#" + destaque + ">" + atual + "</color><color=#" + cinza + "> de " + total + "</color>";

        if (atual != ultimoValorContador && ultimoValorContador >= 0)
            objetivoAte = Time.unscaledTime + tempoObjetivoNaTela;
        ultimoValorContador = atual;
    }

    // ---------------- Ação (tecla + verbo) ----------------

    /// <summary>
    /// Mostra a ação possível perto de um objeto, no formato [E] Guardar mochila.
    /// </summary>
    /// <param name="tecla">Nome da tecla (ex.: "E"). Vazio = mostra só o texto.</param>
    /// <param name="acao">O que acontece ao apertar (ex.: "guardar a mochila").</param>
    public void MostrarAcao(string tecla, string acao)
    {
        if (textoTecla != null)
            textoTecla.text = tecla ?? "";
        if (caixaTecla != null)
            caixaTecla.SetActive(!string.IsNullOrEmpty(tecla));
        if (textoAcao != null)
            textoAcao.text = PrimeiraMaiuscula(acao);
        acaoVisivel = true;
    }

    /// <summary>
    /// Versão antiga, mantida para scripts que mandam a frase pronta ("Aperte <b>E</b> para ...").
    /// A frase é separada em tecla e ação e mostrada no formato novo.
    /// </summary>
    /// <param name="texto">Frase da dica.</param>
    public void MostrarDica(string texto)
    {
        Match m = Regex.Match(texto ?? "", @"Aperte\s*<b>(.+?)</b>\s*para\s*(.+)", RegexOptions.IgnoreCase);
        if (m.Success)
            MostrarAcao(m.Groups[1].Value, m.Groups[2].Value);
        else
            MostrarAcao("", texto);
    }

    /// <summary>Esconde a ação (o jogador se afastou do objeto).</summary>
    public void EsconderDica()
    {
        acaoVisivel = false;
    }

    /// <summary>
    /// Nome curto de uma tecla para mostrar na tela.
    /// </summary>
    /// <param name="tecla">Tecla do Unity.</param>
    /// <returns>Texto da tecla (ex.: "E", "Espaço", "Clique").</returns>
    public static string NomeDaTecla(KeyCode tecla)
    {
        switch (tecla)
        {
            case KeyCode.Mouse0: return "Clique";
            case KeyCode.Space: return "Espaço";
            case KeyCode.Tab: return "Tab";
            case KeyCode.Return: return "Enter";
            case KeyCode.LeftShift: return "Shift";
            default:
                string nome = tecla.ToString();
                return nome.StartsWith("Alpha") ? nome.Substring(5) : nome;
        }
    }

    // ---------------- Legenda / avisos ----------------

    /// <summary>
    /// Mostra um aviso como legenda. O tempo na tela depende do tamanho do texto.
    /// </summary>
    /// <param name="texto">Frase do aviso.</param>
    public void MostrarAviso(string texto)
    {
        if (textoAviso == null || string.IsNullOrEmpty(texto))
            return;
        textoAviso.text = texto;
        avisoAte = Time.unscaledTime + TempoDeLeitura(texto);
    }

    /// <summary>
    /// Calcula quanto tempo um texto precisa ficar na tela para dar tempo de ler.
    /// </summary>
    /// <param name="texto">Texto (pode ter tags como &lt;b&gt;, que não contam).</param>
    /// <returns>Segundos: o mínimo de duracaoAviso ou mais, para textos longos.</returns>
    public float TempoDeLeitura(string texto)
    {
        string semTags = Regex.Replace(texto ?? "", "<.*?>", "");
        return Mathf.Max(duracaoAviso, 1f + semTags.Length * segundosPorLetra);
    }

    // ---------------- Mensagens no celular ----------------

    /// <summary>
    /// Coloca uma mensagem na fila. Ela chega como notificação no canto da tela.
    /// </summary>
    /// <param name="remetente">Nome de quem mandou (ex.: "Brenno").</param>
    /// <param name="texto">Texto da mensagem, do jeito que a pessoa escreveria no grupo.</param>
    public void MostrarMensagem(string remetente, string texto)
    {
        if (grupoMensagem == null || string.IsNullOrEmpty(texto))
            return;
        filaMensagens.Enqueue(new Mensagem { remetente = remetente, texto = texto });
    }

    /// <summary>
    /// Controla a notificação: entra deslizando, fica o tempo de leitura e sai.
    /// </summary>
    /// <param name="telaEscura">Se a tela preta está cobrindo o jogo (aí a mensagem espera).</param>
    private void AtualizarMensagens(bool telaEscura)
    {
        if (grupoMensagem == null || caixaMensagem == null || Pausado)
            return;

        tempoEtapa += Time.unscaledDeltaTime;
        float deslocamento = 40f;

        switch (etapaMensagem)
        {
            case 0:
                if (filaMensagens.Count == 0 || telaEscura || MenuAberto)
                    return;
                Mensagem m = filaMensagens.Dequeue();
                PreencherMensagem(m);
                TocarSom2D(somMensagem, 0.5f);
                duracaoMensagem = TempoDeLeitura(m.texto) + 1.5f;
                etapaMensagem = 1;
                tempoEtapa = 0f;
                break;

            case 1:
                float t = Mathf.Clamp01(tempoEtapa / 0.3f);
                float suave = 1f - Mathf.Pow(1f - t, 3f);
                grupoMensagem.alpha = suave;
                caixaMensagem.anchoredPosition = posicaoMensagem + Vector2.right * deslocamento * (1f - suave);
                if (t >= 1f) { etapaMensagem = 2; tempoEtapa = 0f; }
                break;

            case 2:
                if (tempoEtapa >= duracaoMensagem) { etapaMensagem = 3; tempoEtapa = 0f; }
                break;

            case 3:
                float s = Mathf.Clamp01(tempoEtapa / 0.25f);
                grupoMensagem.alpha = 1f - s;
                caixaMensagem.anchoredPosition = posicaoMensagem + Vector2.right * deslocamento * s;
                if (s >= 1f) { etapaMensagem = 0; tempoEtapa = 0f; }
                break;
        }
    }

    /// <summary>
    /// Escreve o nome, as iniciais e o texto da mensagem e escolhe a cor do ícone pelo nome.
    /// </summary>
    /// <param name="m">Mensagem que vai aparecer.</param>
    private void PreencherMensagem(Mensagem m)
    {
        if (textoRemetente != null) textoRemetente.text = m.remetente;
        if (textoMensagem != null) textoMensagem.text = m.texto;
        if (textoIniciais != null) textoIniciais.text = string.IsNullOrEmpty(m.remetente) ? "?" : m.remetente.Substring(0, 1).ToUpper();
        if (iconeRemetente != null)
        {
            Color[] cores = { Paleta.AzulPlaca, Paleta.Hex("2F5D50"), Paleta.Hex("6B3F5C"), Paleta.Hex("7A4B22") };
            int indice = Mathf.Abs((m.remetente ?? "").GetHashCode()) % cores.Length;
            iconeRemetente.color = cores[indice];
        }
    }

    // ---------------- Pausa ----------------

    /// <summary>
    /// Confere se dá para abrir a pausa agora (sem outro painel aberto e sem tela preta).
    /// </summary>
    /// <returns>Verdadeiro se a pausa pode abrir.</returns>
    private bool PodePausar()
    {
        if (painelPausa == null || MenuAberto || Time.frameCount == quadroPainelFechado)
            return false;
        if (TelaPreta.Instancia != null && TelaPreta.Instancia.grupo != null && TelaPreta.Instancia.grupo.alpha > 0.5f)
            return false;
        return true;
    }

    /// <summary>Abre o menu de pausa: para o tempo, pausa os sons do jogo e mostra o objetivo atual.</summary>
    public void AbrirPausa()
    {
        Pausado = true;
        if (textoPausaLocal != null)
            textoPausaLocal.text = nomeDoLocal;
        if (textoPausaNivel != null)
            textoPausaNivel.text = "Nível " + (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1);
        if (textoPausaObjetivo != null)
        {
            string texto = string.IsNullOrEmpty(objetivoAtual) ? "" : objetivoAtual;
            if (!string.IsNullOrEmpty(contadorAtual))
                texto += "\n<color=#" + ColorUtility.ToHtmlStringRGB(Paleta.Cinza) + ">" + contadorAtual + "</color>";
            textoPausaObjetivo.text = texto;
        }
        AbrirPainelComMouse(painelPausa);
        AudioListener.pause = true;
        if (botaoContinuarPausa != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(botaoContinuarPausa.gameObject);
    }

    /// <summary>Fecha o menu de pausa e volta ao jogo.</summary>
    public void FecharPausa()
    {
        Pausado = false;
        AudioListener.pause = false;
        FecharPainelComMouse(painelPausa);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    // ---------------- Introdução ----------------

    /// <summary>Abre o painel de introdução: para o tempo e solta o mouse.</summary>
    public void AbrirIntro()
    {
        painelIntro.SetActive(true);
        MenuAberto = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (musicaIntro != null && !musicaIntro.isPlaying)
            musicaIntro.Play();

        if (botaoContinuar != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(botaoContinuar.gameObject);
    }

    /// <summary>Fecha a introdução, volta o tempo e prende o mouse para jogar.</summary>
    public void FecharIntro()
    {
        if (painelIntro != null)
            painelIntro.SetActive(false);

        introJaVista = true;
        MenuAberto = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        objetivoAte = Time.unscaledTime + tempoObjetivoNaTela;

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

    // ---------------- Painéis com mouse (usados por outros scripts) ----------------

    /// <summary>
    /// Abre um painel que precisa do mouse (quadro, lista de livros, pausa): para o tempo e solta o cursor.
    /// </summary>
    /// <param name="painel">Objeto do painel a ser ligado.</param>
    public static void AbrirPainelComMouse(GameObject painel)
    {
        if (painel != null)
            painel.SetActive(true);

        MenuAberto = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Fecha um painel aberto com AbrirPainelComMouse: volta o tempo e prende o cursor.
    /// </summary>
    /// <param name="painel">Objeto do painel a ser desligado.</param>
    public static void FecharPainelComMouse(GameObject painel)
    {
        if (painel != null)
            painel.SetActive(false);

        MenuAberto = false;
        quadroPainelFechado = Time.frameCount;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ---------------- Sons e utilidades ----------------

    /// <summary>Fonte de som 2D compartilhada pela interface (toca mesmo com o jogo pausado).</summary>
    private static AudioSource fonteSons2D;

    /// <summary>
    /// Toca um som de interface que não depende de posição.
    /// </summary>
    /// <param name="som">Som a tocar. Vazio = não faz nada.</param>
    /// <param name="volume">Volume de 0 a 1.</param>
    public static void TocarSom2D(AudioClip som, float volume = 1f)
    {
        if (som == null)
            return;

        if (fonteSons2D == null)
        {
            GameObject g = new GameObject("Sons_Interface");
            fonteSons2D = g.AddComponent<AudioSource>();
            fonteSons2D.playOnAwake = false;
            fonteSons2D.spatialBlend = 0f;
            fonteSons2D.ignoreListenerPause = true;
            fonteSons2D.priority = 0;
        }
        fonteSons2D.PlayOneShot(som, volume);
    }

    /// <summary>
    /// Liga um botão a uma função, com o som de clique.
    /// </summary>
    /// <param name="botao">Botão da interface (pode ser vazio).</param>
    /// <param name="acao">Função chamada no clique.</param>
    private void Ligar(Button botao, UnityEngine.Events.UnityAction acao)
    {
        if (botao == null)
            return;
        botao.onClick.AddListener(TocarClique);
        botao.onClick.AddListener(acao);
    }

    /// <summary>Toca o som de clique dos botões.</summary>
    private void TocarClique()
    {
        TocarSom2D(somClique, 0.7f);
    }

    /// <summary>Reinicia o nível atual (botão da pausa).</summary>
    private void Reiniciar()
    {
        Pausado = false;
        AudioListener.pause = false;
        if (GameManager.Instancia != null)
            GameManager.Instancia.ReiniciarNivel();
    }

    /// <summary>Fecha o jogo (no editor, para o modo Play).</summary>
    private void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Leva a transparência de um grupo em direção ao alvo, suavemente, em tempo real.
    /// </summary>
    /// <param name="grupo">Grupo da interface (pode ser vazio).</param>
    /// <param name="alvo">0 = invisível, 1 = visível.</param>
    private static void Aproximar(CanvasGroup grupo, float alvo)
    {
        if (grupo == null)
            return;
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, alvo, Time.unscaledDeltaTime / tempoFade);
    }

    /// <summary>
    /// Define a transparência de um grupo na hora.
    /// </summary>
    /// <param name="grupo">Grupo da interface (pode ser vazio).</param>
    /// <param name="alfa">0 = invisível, 1 = visível.</param>
    private static void DefinirAlfa(CanvasGroup grupo, float alfa)
    {
        if (grupo != null)
            grupo.alpha = alfa;
    }

    /// <summary>
    /// Deixa a primeira letra maiúscula (o resto fica igual).
    /// </summary>
    /// <param name="texto">Texto original.</param>
    /// <returns>O texto com a primeira letra maiúscula.</returns>
    private static string PrimeiraMaiuscula(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return "";
        return char.ToUpper(texto[0]) + texto.Substring(1);
    }
}
