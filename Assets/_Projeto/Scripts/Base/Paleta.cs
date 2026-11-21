using UnityEngine;

/// <summary>
/// Cores da identidade visual do Escape UVV. Toda a interface lê as cores daqui,
/// para o jogo inteiro ter a mesma cara. Para mudar uma cor no jogo todo, mude só aqui.
/// A fonte é a Barlow (licença OFL), desenhada a partir de placas de sinalização.
/// </summary>
public static class Paleta
{
    /// <summary>Fundo escuro dos painéis e sombras da interface (azul quase preto, como o campus à noite).</summary>
    public static readonly Color Tinta = Hex("0E1116");

    /// <summary>Texto principal: branco levemente quente, como papel sob luz fria.</summary>
    public static readonly Color Papel = Hex("EDEAE3");

    /// <summary>Texto secundário (rótulos, horários, itens não selecionados).</summary>
    public static readonly Color Cinza = Hex("9AA3AE");

    /// <summary>Azul das placas de sinalização do campus (cartão do nível).</summary>
    public static readonly Color AzulPlaca = Hex("1F3A68");

    /// <summary>Destaque: item selecionado no menu e itens encontrados. Usar pouco.</summary>
    public static readonly Color Ambar = Hex("F2B33D");

    /// <summary>Perigo (professora, erro no quadro).</summary>
    public static readonly Color Vermelho = Hex("E5484D");

    /// <summary>Acerto e saída liberada.</summary>
    public static readonly Color Verde = Hex("3FBF7F");

    /// <summary>
    /// Converte um código hexadecimal (ex.: "EDEAE3") em Color.
    /// </summary>
    /// <param name="hex">Código da cor com 6 dígitos, sem o #.</param>
    /// <returns>A cor correspondente, totalmente opaca.</returns>
    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color cor);
        return cor;
    }

    /// <summary>
    /// Devolve a mesma cor com outra transparência.
    /// </summary>
    /// <param name="cor">Cor de origem.</param>
    /// <param name="alfa">Transparência de 0 (invisível) a 1 (opaca).</param>
    /// <returns>Cópia da cor com o alfa trocado.</returns>
    public static Color ComAlfa(Color cor, float alfa)
    {
        cor.a = alfa;
        return cor;
    }
}
