using TMPro;
using UnityEngine;

/// <summary>
/// Terminal de consulta ao acervo (Nível 4). Apertando E abre um painel com a lista
/// de livros que o jogador precisa achar e onde cada um está (corredor e andar).
/// E ou Esc fecham o painel. O texto é editado direto no Inspector.
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

    public AudioClip somTecla;

    private bool aberto;
    private bool jaConsultou;

    /// <summary>Texto padrão da dica.</summary>
    void Start()
    {
        if (string.IsNullOrEmpty(textoAcao) || textoAcao == "interagir")
            textoAcao = "consultar o acervo";
        if (painel != null)
            painel.SetActive(false);
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
        if (textoPainel != null)
            textoPainel.text = resultado + "\n\n<size=70%><color=#9aa>Aperte E para fechar</color></size>";

        UIManager.AbrirPainelComMouse(painel);
        aberto = true;
        EsconderDica();
    }

    /// <summary>Fecha o painel e atualiza o objetivo na primeira vez.</summary>
    private void Fechar()
    {
        UIManager.FecharPainelComMouse(painel);
        aberto = false;

        if (!jaConsultou && UI != null && !string.IsNullOrEmpty(objetivoDepois))
            UI.MostrarObjetivo(objetivoDepois);
        jaConsultou = true;
        AtualizarDica();
    }
}
