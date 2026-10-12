using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Regras do nível: conta os itens coletados, destranca a porta quando o contador
/// chega ao total e carrega a próxima cena. Existe um GameManager por cena.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>Acesso rápido ao GameManager da cena atual.</summary>
    public static GameManager Instancia { get; private set; }

    [Header("Objetivo")]
    [Tooltip("Quantos itens o jogador precisa pegar para destrancar a porta.")]
    public int totalItens = 5;

    [Tooltip("Palavra usada no contador da tela. Ex.: Itens, Livros.")]
    public string rotuloContador = "Itens";

    [Header("Referências")]
    [Tooltip("Porta que desliza quando o contador chega ao total.")]
    public PortaDeslizante portaSaida;

    [Tooltip("Interface do nível (textos, contador e botões).")]
    public UIManager ui;

    [Header("Troca de cena")]
    [Tooltip("Nome da cena carregada ao sair pela porta (precisa estar no Build Settings).")]
    public string proximaCena = "Nivel2";

    [Header("Textos")]
    [TextArea] public string textoObjetivo = "A porta está trancada. Encontre os 5 itens perdidos do laboratório.";
    [TextArea] public string textoPortaLiberada = "Porta destrancada! Vá até a saída.";

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

    /// <summary>Mostra o objetivo e o contador zerado no início do nível.</summary>
    void Start()
    {
        if (ui != null)
        {
            ui.MostrarObjetivo(textoObjetivo);
            ui.AtualizarContador(rotuloContador, ItensColetados, totalItens);
        }
    }

    /// <summary>Limpa a referência estática quando a cena é descarregada.</summary>
    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }

    /// <summary>
    /// Chamado por um ItemColetavel quando o jogador encosta nele.
    /// Soma 1 no contador e, se chegou ao total, destranca a porta.
    /// </summary>
    /// <param name="nomeItem">Nome do item, mostrado no aviso da tela.</param>
    public void ColetarItem(string nomeItem)
    {
        if (PortaDestrancada)
            return;

        ItensColetados++;

        if (ui != null)
        {
            ui.AtualizarContador(rotuloContador, ItensColetados, totalItens);
            ui.MostrarAviso("Você encontrou: " + nomeItem);
        }

        if (ItensColetados >= totalItens)
            DestrancarPorta();
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
