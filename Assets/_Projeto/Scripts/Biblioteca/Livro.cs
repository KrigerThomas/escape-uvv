using UnityEngine;

/// <summary>
/// Livro do acervo que o jogador precisa encontrar (Nível 4). É um ItemPegavel que
/// já vem configurado para contar no objetivo. Fica numa estante, um pouco puxado para fora,
/// com uma luz fraca para chamar atenção.
/// O nome no inventário fica "Livro: " + título (a catraca usa isso para o alarme antifurto).
/// </summary>
public class Livro : ItemPegavel
{
    [Header("Livro")]
    [Tooltip("Título que aparece na tela.")]
    public string titulo = "Cálculo Volume 1";

    /// <summary>Monta o nome e os textos a partir do título.</summary>
    void Awake()
    {
        nomeItem = "Livro: " + titulo;
        contaNoObjetivo = true;
        textoAcao = "pegar \"" + titulo + "\"";
    }

    /// <summary>Nos avisos aparece só o título ("Você encontrou: Cálculo").</summary>
    protected override string NomeNaTela()
    {
        return titulo;
    }
}
