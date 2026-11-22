using UnityEngine;

/// <summary>
/// Opções do jogador (volume, sensibilidade do mouse e tela cheia).
/// Os valores ficam salvos no PlayerPrefs, então continuam iguais quando o jogo é aberto de novo.
/// É uma classe estática: qualquer script lê direto, sem precisar arrastar referência no Inspector.
/// </summary>
public static class Configuracoes
{
    private const string CHAVE_VOLUME = "opcoes_volume";
    private const string CHAVE_SENSIBILIDADE = "opcoes_sensibilidade";
    private const string CHAVE_TELA_CHEIA = "opcoes_tela_cheia";

    /// <summary>Volume geral do jogo, de 0 (mudo) a 1 (máximo).</summary>
    public static float VolumeGeral
    {
        get => PlayerPrefs.GetFloat(CHAVE_VOLUME, 1f);
        set { PlayerPrefs.SetFloat(CHAVE_VOLUME, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    /// <summary>Multiplicador da sensibilidade do mouse (1 = padrão).</summary>
    public static float Sensibilidade
    {
        get => PlayerPrefs.GetFloat(CHAVE_SENSIBILIDADE, 1f);
        set { PlayerPrefs.SetFloat(CHAVE_SENSIBILIDADE, Mathf.Clamp(value, 0.2f, 3f)); PlayerPrefs.Save(); }
    }

    /// <summary>Verdadeiro para jogar em tela cheia.</summary>
    public static bool TelaCheia
    {
        get => PlayerPrefs.GetInt(CHAVE_TELA_CHEIA, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(CHAVE_TELA_CHEIA, value ? 1 : 0);
            PlayerPrefs.Save();
            Screen.fullScreen = value;
        }
    }
}
