using UnityEngine;

/// <summary>
/// Atalhos usados pelos animais e plantas do pátio: mostrar um aviso no HUD,
/// achar o jogador e saber a altura do chão em um ponto.
/// Os avisos daqui NÃO contam como item: são só mensagens (easter eggs, dicas).
/// </summary>
public static class AvisoNaTela
{
    /// <summary>Mostra um aviso curto no meio da tela, se o GameManager e o HUD existirem na cena.</summary>
    /// <param name="texto">Mensagem do aviso.</param>
    public static void Mostrar(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return;

        if (GameManager.Instancia != null && GameManager.Instancia.ui != null)
            GameManager.Instancia.ui.MostrarAviso(texto);
    }

    /// <summary>Procura o objeto com a tag Player.</summary>
    /// <returns>Transform do jogador ou null se não houver.</returns>
    public static Transform AcharJogador()
    {
        GameObject jogador = GameObject.FindWithTag("Player");
        return jogador != null ? jogador.transform : null;
    }

    /// <summary>Altura do chão (Y do mundo) no ponto, usando o terreno ativo. Sem terreno, devolve o Y recebido.</summary>
    /// <param name="posicao">Ponto do mundo.</param>
    /// <returns>Y do chão.</returns>
    public static float AlturaDoChao(Vector3 posicao)
    {
        Terrain terreno = Terrain.activeTerrain;
        if (terreno == null)
            return posicao.y;

        return terreno.SampleHeight(posicao) + terreno.transform.position.y;
    }

    /// <summary>Distância entre dois pontos ignorando a altura.</summary>
    public static float DistanciaPlana(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
