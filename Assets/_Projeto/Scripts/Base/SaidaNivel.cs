using UnityEngine;

/// <summary>
/// Área invisível na saída do nível. Se a saída estiver liberada, carrega
/// o próximo nível; se não estiver, avisa quantos itens faltam.
///
/// No Nível 2 são usadas duas áreas:
///  - "Aviso_Portao", na frente do portão, com "Apenas Aviso" marcado: só mostra
///    quantos itens faltam (e não faz nada depois que o portão abre);
///  - "Saida_Nivel", logo depois do portão: troca de cena quando o jogador atravessa.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SaidaNivel : MonoBehaviour
{
    [Tooltip("Marque para esta área só avisar que a saída está trancada (sem trocar de cena).")]
    public bool apenasAviso = false;

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

        if (!gm.PortaDestrancada)
            gm.AvisarPortaTrancada();
        else if (!apenasAviso)
            gm.CarregarProximaCena();
    }
}
