using System.Text;

/// <summary>
/// A fonte PerfectDOSVGA437 (computador antigo do Nível 4) guarda as letras com acento
/// nas posições da tabela antiga do DOS (página de código 437). Este conversor troca cada
/// letra pela posição certa para ela aparecer na fonte. Letras que o DOS não tinha
/// (ã, õ, Á...) viram a letra sem acento, como acontecia nos computadores da época.
/// </summary>
public static class TextoDOS
{
    // Caracteres 128 a 255 da página 437, na ordem.
    private const string Tabela437 =
        "ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■ ";

    private const string SemAcentoDe = "ÁÍÓÚÀÂÊÔÃÕãõ";
    private const string SemAcentoPara = "AIOUAAEOAOao";

    /// <summary>Devolve o texto pronto para a fonte do DOS (tags como &lt;b&gt; continuam funcionando).</summary>
    public static string Converter(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return texto;

        StringBuilder saida = new StringBuilder(texto.Length);
        foreach (char c in texto)
        {
            if (c < 128)
            {
                saida.Append(c);
                continue;
            }

            // O "á" do DOS fica na posição que a Unity trata como espaço sem quebra: vira "a".
            if (c == 'á')
            {
                saida.Append('a');
                continue;
            }

            int posicao = Tabela437.IndexOf(c);
            if (posicao >= 0)
            {
                saida.Append((char)(128 + posicao));
                continue;
            }

            int troca = SemAcentoDe.IndexOf(c);
            saida.Append(troca >= 0 ? SemAcentoPara[troca] : c);
        }
        return saida.ToString();
    }
}
