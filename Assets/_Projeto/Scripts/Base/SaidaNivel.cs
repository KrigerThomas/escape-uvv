using System.Collections;
using UnityEngine;

/// <summary>
/// Área invisível na saída do nível. Se a saída estiver liberada, escurece a tela
/// (se a cena tiver TelaPreta) e carrega o próximo nível; se não estiver, avisa quantos itens faltam.
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

    [Tooltip("Frase na tela preta antes de trocar de nível (só nas cenas com TelaPreta).")]
    public string fraseSaida = "";

    [Tooltip("Segundos de tela preta antes de carregar o próximo nível.")]
    public float tempoAntesDeTrocar = 1.8f;

    private bool saindo;

    /// <summary>Verdadeiro depois que o jogador atravessou a saída e a cena está trocando.</summary>
    public static bool NivelTerminando { get; private set; }

    /// <summary>Cada cena começa com a saída fechada.</summary>
    void Awake()
    {
        NivelTerminando = false;
    }

    /// <summary>Garante que o collider funcione como trigger.</summary>
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>Reage quando o jogador entra na área da saída.</summary>
    /// <param name="outro">Collider que entrou no trigger.</param>
    void OnTriggerEnter(Collider outro)
    {
        if (saindo || !outro.CompareTag("Player"))
            return;

        GameManager gm = GameManager.Instancia;
        if (gm == null)
            return;

        if (!gm.PortaDestrancada)
            gm.AvisarPortaTrancada();
        else if (!apenasAviso)
            StartCoroutine(Sair(gm));
    }

    /// <summary>Escurece a tela (quando existe) e troca de nível.</summary>
    private IEnumerator Sair(GameManager gm)
    {
        saindo = true;
        NivelTerminando = true;
        if (TelaPreta.Instancia != null)
        {
            TelaPreta.Instancia.Escurecer(fraseSaida);
            yield return new WaitForSeconds(tempoAntesDeTrocar);
        }
        gm.CarregarProximaCena();
    }
}
