using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Regras do nível: conta os itens coletados, destranca a porta quando o contador
/// chega ao total e carrega a próxima cena. Existe um GameManager por cena.
/// No Nível 2 a saída já começa aberta e o contador mostra os segredos opcionais
/// (cada Segredo da cena se registra sozinho, então o total é calculado na hora).
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>Acesso rápido ao GameManager da cena atual.</summary>
    public static GameManager Instancia { get; private set; }

    [Header("Objetivo")]
    [Tooltip("Quantos itens o jogador precisa pegar para destrancar a porta. Em níveis com segredos, deixe 0: cada Segredo soma 1 sozinho.")]
    public int totalItens = 5;

    [Tooltip("Palavra usada no contador da tela. Ex.: Itens, Livros, Segredos.")]
    public string rotuloContador = "Itens";

    [Tooltip("Marque em níveis em que a saída não depende de coletar nada (ex.: o labirinto do Nível 2).")]
    public bool saidaAbertaDesdeOInicio = false;

    [Tooltip("Texto que aparece antes do nome do item no aviso da tela.")]
    public string prefixoAviso = "Você encontrou: ";

    [Header("Referências")]
    [Tooltip("Porta que desliza quando o contador chega ao total.")]
    public PortaDeslizante portaSaida;

    [Tooltip("Interface do nível (textos, contador e botões).")]
    public UIManager ui;

    [Header("Sons")]
    [Tooltip("Som tocado quando o jogador tenta sair com a porta trancada.")]
    public AudioClip somPortaTrancada;

    [Header("Troca de cena")]
    [Tooltip("Nome da cena carregada ao sair pela porta (precisa estar no Build Settings).")]
    public string proximaCena = "Nivel2";

    [Header("Textos")]
    [TextArea] public string textoObjetivo = "A porta está trancada. Encontre os 5 itens perdidos do laboratório.";
    [TextArea] public string textoPortaLiberada = "Porta destrancada! Vá até a saída.";
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
    }

    /// <summary>
    /// Mostra o objetivo e o contador zerado no início do nível.
    /// Se a saída começa aberta, já marca a porta como destrancada.
    /// </summary>
    void Start()
    {
        if (saidaAbertaDesdeOInicio)
        {
            PortaDestrancada = true;
            if (portaSaida != null)
                portaSaida.Abrir();
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
        if (ItensColetados >= totalItens)
            return;

        ItensColetados++;

        if (ui != null)
        {
            ui.AtualizarContador(rotuloContador, ItensColetados, totalItens);
            ui.MostrarAviso(prefixoAviso + nomeItem);
        }

        if (ItensColetados < totalItens)
            return;

        if (!PortaDestrancada)
            DestrancarPorta();
        else if (ui != null && !string.IsNullOrEmpty(textoTudoEncontrado))
            ui.MostrarObjetivo(textoTudoEncontrado);
    }

    /// <summary>Abre a porta de saída e troca a mensagem de objetivo.</summary>
    private void DestrancarPorta()
    {
        PortaDestrancada = true;

        if (portaSaida != null)
            portaSaida.Abrir();

        if (ui != null)
            ui.MostrarObjetivo(textoPortaLiberada);
    }

    /// <summary>Avisa quantos itens faltam quando o jogador tenta sair antes da hora.</summary>
    public void AvisarPortaTrancada()
    {
        if (somPortaTrancada != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(somPortaTrancada, Camera.main.transform.position, 0.8f);

        if (ui == null)
            return;

        string palavra = ItensFaltando == 1 ? "item" : "itens";
        ui.MostrarAviso("A porta está trancada. Faltam " + ItensFaltando + " " + palavra + ".");
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
