using System.Collections;
using UnityEngine;

/// <summary>
/// Mensagens que os colegas mandam no celular do calouro durante o nível.
/// Cada mensagem chega depois de alguns segundos de jogo (o tempo para quando o jogo pausa).
/// Para outro script mandar uma mensagem na hora certa, use GameManager.Instancia.ui.MostrarMensagem.
/// </summary>
public class MensagensDoNivel : MonoBehaviour
{
    /// <summary>Uma mensagem: quem mandou, o texto e quando chega.</summary>
    [System.Serializable]
    public class Mensagem
    {
        [Tooltip("Nome de quem manda (alguém do grupo).")]
        public string remetente = "Brenno";

        [Tooltip("Texto, do jeito que a pessoa escreveria no grupo da turma.")]
        [TextArea(1, 3)] public string texto = "";

        [Tooltip("Segundos depois da mensagem anterior (a primeira conta do começo do nível).")]
        public float esperar = 4f;
    }

    [Tooltip("Mensagens em ordem de chegada.")]
    public Mensagem[] mensagens = new Mensagem[0];

    [Tooltip("Se preenchido, as mensagens só começam depois que o jogador pegar este item (ex.: o celular no Nível 5).")]
    public string esperarItem = "";

    /// <summary>Começa a mandar as mensagens.</summary>
    void Start()
    {
        StartCoroutine(Mandar());
    }

    /// <summary>
    /// Espera o tempo de cada mensagem e entrega para a interface.
    /// Usa tempo de jogo: com o jogo pausado, a contagem para.
    /// </summary>
    private IEnumerator Mandar()
    {
        while (!string.IsNullOrEmpty(esperarItem) && !Inventario.Tem(esperarItem))
            yield return null;

        foreach (Mensagem m in mensagens)
        {
            yield return new WaitForSeconds(m.esperar);
            if (GameManager.Instancia != null && GameManager.Instancia.ui != null)
                GameManager.Instancia.ui.MostrarMensagem(m.remetente, m.texto);
        }
    }
}
