using UnityEngine;

/// <summary>
/// Atalhos de teste mantidos para a apresentação:
/// R reinicia o nível e N pula para o próximo.
/// </summary>
public class AtalhosTeste : MonoBehaviour
{
    [Tooltip("Desmarque para desligar os atalhos na versão final.")]
    public bool ativo = true;

    /// <summary>Lê as teclas R e N a cada quadro.</summary>
    void Update()
    {
        if (!ativo || GameManager.Instancia == null)
            return;

        if (Input.GetKeyDown(KeyCode.R))
            GameManager.Instancia.ReiniciarNivel();
        else if (Input.GetKeyDown(KeyCode.N))
            GameManager.Instancia.CarregarProximaCena();
    }
}
