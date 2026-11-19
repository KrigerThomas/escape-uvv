using UnityEngine;

/// <summary>
/// Totem de autoatendimento da biblioteca (Nível 4): registra o empréstimo dos livros.
/// Só funciona quando o jogador já pegou todos os livros (contador do GameManager completo).
/// Depois disso a catraca deixa sair sem disparar o alarme antifurto.
/// </summary>
public class Autoatendimento : Interagivel
{
    public string itemEmprestimo = "Empréstimo registrado";

    [Tooltip("Objetivo depois de registrar.")]
    [TextArea] public string objetivoDepois = "Empréstimo feito! Passe pela catraca e pegue sua mochila no guarda-volumes.";

    public AudioClip somConfirmar;

    /// <summary>Texto padrão da dica.</summary>
    void Start()
    {
        if (string.IsNullOrEmpty(textoAcao) || textoAcao == "interagir")
            textoAcao = "registrar o empréstimo";
    }

    /// <summary>Depois de registrado, a dica some.</summary>
    public override bool PodeInteragir()
    {
        return !Inventario.Tem(itemEmprestimo);
    }

    /// <summary>Registra o empréstimo ou avisa quantos livros faltam.</summary>
    public override void Interagir()
    {
        GameManager gm = GameManager.Instancia;
        if (gm != null && gm.ItensFaltando > 0)
        {
            Avisar("Ainda faltam " + gm.ItensFaltando + (gm.ItensFaltando == 1 ? " livro." : " livros."));
            return;
        }

        Inventario.Adicionar(itemEmprestimo);
        UIManager.TocarSom2D(somConfirmar, 0.8f);
        Avisar("Empréstimo registrado: 3 livros, devolução em 7 dias úteis.");
        if (UI != null)
            UI.MostrarObjetivo(objetivoDepois);
    }
}
