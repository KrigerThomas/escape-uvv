using UnityEngine;

/// <summary>
/// Área invisível na porta de saída. Se a porta estiver destrancada, carrega
/// o próximo nível; se não estiver, avisa quantos itens faltam.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SaidaNivel : MonoBehaviour
{
    /// <summary>Garante que o collider funcione como trigger.</summary>
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>Reage quando o jogador entra na área da saída.</summary>
    /// <param name="outro">Collider que entrou no trigger.</param>
    void OnTriggerEnter(Collider outro)
    {
        if (!outro.CompareTag("Player"))
            return;

        GameManager gm = GameManager.Instancia;
        if (gm == null)
            return;

        if (gm.PortaDestrancada)
            gm.CarregarProximaCena();
        else
            gm.AvisarPortaTrancada();
    }
}
