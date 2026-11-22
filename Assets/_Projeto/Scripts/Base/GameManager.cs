using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Regras do nível: conta os itens coletados, destranca a porta quando o contador
/// chega ao total e carrega a próxima cena. Existe um GameManager por cena.
/// No Nível 2 os itens usam o script Segredo: cada um se registra sozinho no início
/// da cena, então o total do contador é calculado na hora (deixe totalItens = 0).
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Volumes dos sons")]
    [Range(0f, 1f)] public float volumePortaTrancada = 0.8f;

    /// <summary>Acesso rápido ao GameManager da cena atual.</summary>
    public static GameManager Instancia { get; private set; }

    [Header("Objetivo")]
    [Tooltip("Quantos itens o jogador precisa pegar para destrancar a porta. Em níveis com segredos, deixe 0: cada Segredo soma 1 sozinho.")]
    public int totalItens = 5;

    [Tooltip("Palavra usada no contador da tela. Ex.: Itens, Livros, Segredos.")]
    public string rotuloContador = "Itens";

    [Tooltip("Marque em níveis em que a saída não depende de coletar nada (ex.: o labirinto do Nível 2).")]
    public bool saidaAbertaDesdeOInicio = false;

    [Tooltip("Desmarque quando juntar os itens NÃO deve abrir a saída sozinho (ex.: Nível 4, onde ainda é preciso registrar o empréstimo e pegar a mochila). Nesse caso aparece o Texto Tudo Encontrado.")]
    public bool destrancarAoCompletar = true;

    [Tooltip("Texto que aparece antes do nome do item no aviso da tela.")]
    public string prefixoAviso = "Você achou ";

    [Header("Referências")]
    [Tooltip("Porta que desliza quando o contador chega ao total.")]
    public PortaDeslizante portaSaida;

    [Tooltip("Porta que abre girando (ex.: porta corta-fogo do Nível 3). Pode ficar vazio.")]
    public PortaGiratoria portaGiratoria;

    [Tooltip("Interface do nível (textos, contador e botões).")]
    public UIManager ui;

    [Header("Sons")]
    [Tooltip("Som tocado quando o jogador tenta sair com a porta trancada.")]
    public AudioClip somPortaTrancada;

    [Header("Troca de cena")]
    [Tooltip("Nome da cena carregada ao sair pela porta (precisa estar no Build Settings).")]
    public string proximaCena = "Nivel2";

    [Header("Textos")]
    [TextArea] public string textoObjetivo = "A porta está trancada, encontre os 5 itens perdidos do laboratório";
    [TextArea] public string textoPortaLiberada = "A porta destrancou, pode sair";
    [Tooltip("Começo do aviso quando o jogador tenta sair antes da hora. Depois vem \"Faltam X itens.\"")]
    public string textoPortaTrancada = "A porta está trancada";
    [Tooltip("Palavra do aviso \"Faltam 1 ...\" (singular). Ex.: item, acerto.")]
    public string palavraSingular = "item";
    [Tooltip("Palavra do aviso \"Faltam 2 ...\" (plural). Ex.: itens, acertos.")]
    public string palavraPlural = "itens";
    [Tooltip("Usado só quando a saída já começa aberta: aparece quando o jogador acha tudo.")]
    [TextArea] public string textoTudoEncontrado = "";

    /// <summary>Quantos itens o jogador já pegou.</summary>
    public int ItensColetados { get; private set; }

    /// <summary>Verdadeiro depois que todos os itens foram coletados.</summary>
    public bool PortaDestrancada { get; private set; }

    /// <summary>Quantos itens ainda faltam.</summary>
    public int ItensFaltando => Mathf.Max(0, totalItens - ItensColetados);

    /// <summary>Registra esta instância e garante que o tempo do jogo esteja normal.</summary>
    void Awake()
    {
        Instancia = this;
        Time.timeScale = 1f;
        Inventario.Limpar();
        AudioListener.volume = 0f; // o som entra aos poucos (ver SomEntrando)
    }

    [Tooltip("Segundos para o som do nível subir do silêncio até o volume normal.")]
    public float tempoFadeInSom = 4f;

    /// <summary>
    /// No começo do nível o som sobe aos poucos, em vez de começar alto de uma vez.
    /// Enquanto a abertura (tela preta com o nome do lugar) está na tela, fica em silêncio;
    /// o som entra junto com a imagem do nível.
    /// </summary>
    private IEnumerator SomEntrando()
    {
        AudioListener.volume = 0f;
        yield return null; // deixa a TelaPreta começar

        float limite = Time.unscaledTime + 20f;
        while (TelaPreta.AberturaNaTela && Time.unscaledTime < limite)
            yield return null;

        float t = 0f;
        while (t < tempoFadeInSom)
        {
            t += Time.unscaledDeltaTime;
            AudioListener.volume = Mathf.SmoothStep(0f, Configuracoes.VolumeGeral, t / tempoFadeInSom);
            yield return null;
        }
        AudioListener.volume = Configuracoes.VolumeGeral;
    }

    /// <summary>
    /// Mostra o objetivo e o contador zerado no início do nível.
    /// Se a saída começa aberta, já marca a porta como destrancada.
    /// </summary>
    void Start()
    {
        StartCoroutine(SomEntrando());

        if (saidaAbertaDesdeOInicio)
        {
            PortaDestrancada = true;
            if (portaSaida != null)
                portaSaida.Abrir();
            if (portaGiratoria != null)
                portaGiratoria.Abrir();
        }

        if (ui != null)
        {
            ui.MostrarObjetivo(textoObjetivo);
            ui.AtualizarContador(rotuloContador, ItensColetados, totalItens);
        }
    }

    /// <summary>
    /// Chamado por cada Segredo no início da cena: soma 1 no total do contador.
    /// Assim ninguém precisa contar os segredos à mão quando adicionar ou remover um.
    /// </summary>
    public void RegistrarSegredo()
    {
        totalItens++;

        if (ui != null)
            ui.AtualizarContador(rotuloContador, ItensColetados, totalItens);
    }

    /// <summary>Limpa a referência estática quando a cena é descarregada.</summary>
    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }

    /// <summary>
    /// Chamado por um ItemColetavel (ou por um Segredo) quando o jogador encosta nele.
    /// Soma 1 no contador e, se chegou ao total, destranca a porta
    /// (ou, se a porta já estava aberta, avisa que o jogador achou tudo).
    /// </summary>
    /// <param name="nomeItem">Nome do item, mostrado no aviso da tela.</param>
    public void ColetarItem(string nomeItem)
    {
        ColetarItem(nomeItem, true);
    }

    /// <summary>
    /// Igual ao ColetarItem acima, mas escolhe como o aviso aparece.
    /// </summary>
    /// <param name="texto">Nome do item ou frase pronta.</param>
    /// <param name="usarPrefixo">Verdadeiro: mostra "Você encontrou " + texto. Falso: mostra só o texto (frase pronta). Texto vazio: não mostra aviso.</param>
    public void ColetarItem(string texto, bool usarPrefixo)
    {
        if (ItensColetados >= totalItens)
            return;

        ItensColetados++;

        if (ui != null)
        {
            ui.AtualizarContador(rotuloContador, ItensColetados, totalItens);
            if (!string.IsNullOrEmpty(texto))
                ui.MostrarAviso(usarPrefixo ? prefixoAviso + texto : texto);
        }

        if (ItensColetados < totalItens)
            return;

        if (!PortaDestrancada && destrancarAoCompletar)
            DestrancarPorta();
        else if (ui != null && !string.IsNullOrEmpty(textoTudoEncontrado))
            ui.MostrarObjetivo(textoTudoEncontrado);
    }

    /// <summary>Abre a porta de saída e troca a mensagem de objetivo. Também é chamado pela PortaComChave.</summary>
    public void DestrancarPorta()
    {
        PortaDestrancada = true;

        if (portaSaida != null)
            portaSaida.Abrir();

        if (portaGiratoria != null)
            portaGiratoria.Abrir();

        if (ui != null)
            ui.MostrarObjetivo(textoPortaLiberada);
    }

    /// <summary>Avisa quantos itens faltam quando o jogador tenta sair antes da hora.</summary>
    public void AvisarPortaTrancada()
    {
        UIManager.TocarSom2D(somPortaTrancada, volumePortaTrancada);

        if (ui == null)
            return;

        string palavra = ItensFaltando == 1 ? palavraSingular : palavraPlural;
        ui.MostrarAviso(textoPortaTrancada.TrimEnd('.', ' ') + ", faltam " + ItensFaltando + " " + palavra);
    }

    /// <summary>Carrega a cena configurada em proximaCena (ou volta para a primeira, se não existir).</summary>
    public void CarregarProximaCena()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(proximaCena) && Application.CanStreamedLevelBeLoaded(proximaCena))
            SceneManager.LoadScene(proximaCena);
        else
            SceneManager.LoadScene(0);
    }

    /// <summary>Recarrega o nível atual. Os itens são sorteados de novo.</summary>
    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
