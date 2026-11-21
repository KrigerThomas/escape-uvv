using TMPro;
using UnityEngine;

/// <summary>
/// Terminal de consulta ao acervo (Nível 4). Apertando E abre um painel com a lista
/// de livros que o jogador precisa achar e onde cada um está (o lugar vem do SorteioLivros).
/// Ao fechar pela primeira vez, a lista é impressa: o jogador ganha o item "Lista de livros"
/// e pode abrir de novo em qualquer lugar com L.
/// </summary>
public class TerminalConsulta : Interagivel
{
    [Tooltip("Painel do Canvas que mostra o resultado da consulta.")]
    public GameObject painel;

    [Tooltip("Texto dentro do painel.")]
    public TMP_Text textoPainel;

    [Tooltip("Resultado da consulta mostrado no painel.")]
    [TextArea(6, 14)] public string resultado = "";

    [Tooltip("Objetivo depois da primeira consulta.")]
    [TextArea] public string objetivoDepois = "Encontre os 3 livros no acervo.";

    [Tooltip("Item que o jogador ganha ao imprimir a lista.")]
    public string itemLista = "Lista de livros";

    [TextArea] public string textoImpresso = "Lista impressa, aperte L para ver de novo";

    public AudioClip somTecla;
    public AudioClip somImpressora;

    [Tooltip("Marque se o texto do painel usa a fonte do DOS (converte os acentos).")]
    public bool fonteDOS = true;

    [Header("Computador travando")]
    [Tooltip("Tela do monitor com o programa de consulta.")]
    public GameObject telaNormal;

    [Tooltip("Tela azul que aparece no monitor depois da consulta (o PC \"dá pau\").")]
    public GameObject telaTravada;

    [Tooltip("Som de erro quando o computador trava.")]
    public AudioClip somTravou;

    private bool travou;

    private bool aberto;
    private bool jaConsultou;

    /// <summary>Texto padrão da dica.</summary>
    void Start()
    {
        if (telaTravada != null)
            telaTravada.SetActive(false);
        if (string.IsNullOrEmpty(textoAcao) || textoAcao == "interagir")
            textoAcao = "consultar o acervo";
        if (painel != null)
            painel.SetActive(false);
    }

    /// <summary>Com o computador travado não há mais o que fazer aqui.</summary>
    public override bool PodeInteragir()
    {
        return !travou;
    }

    /// <summary>Com o painel aberto, E ou Esc fecham.</summary>
    protected override void Update()
    {
        if (aberto)
        {
            if (Input.GetKeyDown(tecla) || Input.GetKeyDown(KeyCode.Escape))
                Fechar();
            return;
        }

        base.Update();
    }

    /// <summary>Abre o painel com o resultado da consulta.</summary>
    public override void Interagir()
    {
        UIManager.TocarSom2D(somTecla, 0.6f);
        if (SorteioLivros.Instancia != null)
            resultado = SorteioLivros.Instancia.TextoLista();
        if (textoPainel != null)
            textoPainel.text = fonteDOS ? TextoDOS.Converter(resultado) : resultado;

        UIManager.AbrirPainelComMouse(painel);
        aberto = true;
        EsconderDica();
    }

    /// <summary>Fecha o painel e atualiza o objetivo na primeira vez.</summary>
    private void Fechar()
    {
        UIManager.FecharPainelComMouse(painel);
        aberto = false;

        // Depois da primeira consulta o computador trava (tela azul).
        if (!travou && telaTravada != null)
        {
            travou = true;
            if (telaNormal != null)
                telaNormal.SetActive(false);
            telaTravada.SetActive(true);
            UIManager.TocarSom2D(somTravou, 0.5f);
        }

        if (!jaConsultou)
        {
            Inventario.Adicionar(itemLista);
            UIManager.TocarSom2D(somImpressora, 0.7f);
            Avisar(textoImpresso);
            if (UI != null && !string.IsNullOrEmpty(objetivoDepois))
                UI.MostrarObjetivo(objetivoDepois);
        }
        jaConsultou = true;
        AtualizarDica();
    }
}
