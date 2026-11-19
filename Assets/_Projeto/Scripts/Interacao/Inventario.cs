using System.Collections.Generic;

/// <summary>
/// O que o jogador está carregando no nível atual (ex.: "Celular", "Chave do bloco").
/// É só uma lista de nomes. O GameManager limpa a lista sempre que uma cena começa.
/// </summary>
public static class Inventario
{
    private static readonly HashSet<string> itens = new HashSet<string>();

    /// <summary>Guarda um item pelo nome.</summary>
    public static void Adicionar(string item)
    {
        if (!string.IsNullOrEmpty(item))
            itens.Add(item);
    }

    /// <summary>Tira um item da lista (ex.: devolveu a chave do armário).</summary>
    public static void Remover(string item)
    {
        itens.Remove(item);
    }

    /// <summary>Verdadeiro se o jogador tem o item.</summary>
    public static bool Tem(string item)
    {
        return !string.IsNullOrEmpty(item) && itens.Contains(item);
    }

    /// <summary>Esvazia a lista (chamado no começo de cada nível).</summary>
    public static void Limpar()
    {
        itens.Clear();
    }
}
