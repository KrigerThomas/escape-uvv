using UnityEngine;

/// <summary>
/// Armário do guarda-volumes da biblioteca (Nível 4).
/// Na entrada: o jogador guarda a mochila (E) e recebe a chave do armário.
/// Na saída: devolve a chave e pega a mochila de volta (só depois do empréstimo registrado).
/// Na UVV é assim: não se entra no acervo com mochila, e a chave não pode sair da biblioteca.
/// </summary>
public class GuardaVolumes : Interagivel
{
    [Header("Itens")]
    public string itemMochila = "Mochila";
    public string itemChave = "Chave do armário";
    public string itemEmprestimo = "Empréstimo registrado";

    [Header("Textos")]
    public string textoGuardou = "Mochila guardada. A chave do armário não pode sair da biblioteca.";
    public string textoAindaNao = "Sua mochila está aqui. Termine na biblioteca antes de pegá-la.";
    public string textoPegou = "Mochila de volta e chave devolvida. Agora é só sair!";

    [Tooltip("Objetivo depois de guardar a mochila.")]
    [TextArea] public string objetivoDepoisDeGuardar = "Passe pela catraca e consulte o terminal do acervo.";

    [Tooltip("Objetivo depois de pegar a mochila de volta.")]
    [TextArea] public string objetivoDepoisDePegar = "Saia da biblioteca pela porta de vidro do saguão.";

    public AudioClip somArmario;

    [Tooltip("Marque para o jogador começar o nível carregando a mochila.")]
    public bool comecaComMochila = true;

    /// <summary>Coloca a mochila no inventário no começo do nível.</summary>
    void Start()
    {
        if (comecaComMochila)
            Inventario.Adicionar(itemMochila);
    }

    /// <summary>A dica muda conforme a etapa.</summary>
    protected override string TextoDica()
    {
        if (Inventario.Tem(itemMochila))
            return "guardar a mochila no armário";
        if (Inventario.Tem(itemChave))
            return "pegar a mochila de volta";
        return textoAcao;
    }

    /// <summary>Some a dica quando não há mais nada a fazer aqui.</summary>
    public override bool PodeInteragir()
    {
        return Inventario.Tem(itemMochila) || Inventario.Tem(itemChave);
    }

    /// <summary>Guarda ou devolve a mochila.</summary>
    public override void Interagir()
    {
        UIManager.TocarSom2D(somArmario, 0.7f);

        if (Inventario.Tem(itemMochila))
        {
            Inventario.Remover(itemMochila);
            Inventario.Adicionar(itemChave);
            Avisar(textoGuardou);
            if (UI != null)
                UI.MostrarObjetivo(objetivoDepoisDeGuardar);
            return;
        }

        if (!Inventario.Tem(itemEmprestimo))
        {
            Avisar(textoAindaNao);
            return;
        }

        Inventario.Remover(itemChave);
        Inventario.Adicionar(itemMochila);
        Avisar(textoPegou);
        if (UI != null)
            UI.MostrarObjetivo(objetivoDepoisDePegar);
    }
}
