using UnityEngine;

/// <summary>
/// Porta que só abre se o jogador tiver um item no Inventario (ex.: "Chave do bloco").
/// Sem o item, mostra que está trancada. Com o item, avisa o GameManager para destrancar
/// (o GameManager abre a PortaGiratoria/PortaDeslizante e troca o objetivo).
/// </summary>
public class PortaComChave : Interagivel
{
    [Header("Volumes dos sons")]
    [Range(0f, 1f)] public float volumeDestrancar = 0.9f;

    [Header("Chave")]
    [Tooltip("Nome do item que abre esta porta (igual ao \"Nome Item\" do ItemPegavel).")]
    public string chaveNecessaria = "Chave do bloco";

    [Tooltip("Aviso quando o jogador tenta abrir sem a chave.")]
    public string textoTrancada = "Trancada, a chave do bloco ficou com a professora";

    [Tooltip("Segundo item exigido (opcional). Ex.: no Nível 4, a porta pede o empréstimo E a mochila.")]
    public string segundoItemNecessario = "";

    [Tooltip("Aviso quando falta só o segundo item.")]
    public string textoFaltaSegundoItem = "";

    [Header("Sons")]
    public AudioClip somTrancada;
    public AudioClip somDestrancar;

    [Tooltip("Volume do som de porta trancada.")]
    [Range(0f, 1f)] public float volumeTrancada = 0.6f;

    private bool aberta;

    /// <summary>Texto padrão da dica.</summary>
    void Start()
    {
        if (string.IsNullOrEmpty(textoAcao) || textoAcao == "interagir")
            textoAcao = "abrir a porta";
    }

    /// <summary>Depois de aberta, a dica some.</summary>
    public override bool PodeInteragir()
    {
        return !aberta;
    }

    /// <summary>Testa a chave e abre (ou avisa que está trancada).</summary>
    public override void Interagir()
    {
        if (!Inventario.Tem(chaveNecessaria))
        {
            UIManager.TocarSom2D(somTrancada, volumeTrancada);
            Avisar(textoTrancada);
            return;
        }

        if (!string.IsNullOrEmpty(segundoItemNecessario) && !Inventario.Tem(segundoItemNecessario))
        {
            UIManager.TocarSom2D(somTrancada, volumeTrancada);
            Avisar(textoFaltaSegundoItem);
            return;
        }

        aberta = true;
        UIManager.TocarSom2D(somDestrancar, volumeDestrancar);

        if (GameManager.Instancia != null)
            GameManager.Instancia.DestrancarPorta();
    }
}
