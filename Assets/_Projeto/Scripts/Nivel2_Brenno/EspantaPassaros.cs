using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Faz os pássaros do pacote Living Birds (tag lb_bird) voarem para longe quando
/// este objeto chega perto. Vai no jogador e também nos gatos e no esquilo,
/// então os pássaros fogem de qualquer um deles.
/// </summary>
public class EspantaPassaros : MonoBehaviour
{
    [Tooltip("Pássaros mais perto que isso (em metros) levantam voo.")]
    public float raio = 2.5f;

    [Tooltip("De quanto em quanto tempo procurar pássaros por perto.")]
    public float intervaloBusca = 0.2f;

    [Tooltip("Tempo mínimo antes de espantar o mesmo pássaro de novo (evita repetir o som de asas).")]
    public float esperaMesmoPassaro = 4f;

    private readonly Collider[] encontrados = new Collider[32];
    private readonly Dictionary<Collider, float> ultimoSusto = new Dictionary<Collider, float>();
    private float timerBusca;

    /// <summary>Procura pássaros por perto em intervalos e manda cada um fugir.</summary>
    void Update()
    {
        timerBusca -= Time.deltaTime;
        if (timerBusca > 0f)
            return;
        timerBusca = intervaloBusca;

        int quantidade = Physics.OverlapSphereNonAlloc(transform.position, raio, encontrados, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < quantidade; i++)
        {
            Collider passaro = encontrados[i];
            if (passaro == null || !passaro.CompareTag("lb_bird"))
                continue;

            if (ultimoSusto.TryGetValue(passaro, out float quando) && Time.time - quando < esperaMesmoPassaro)
                continue;

            ultimoSusto[passaro] = Time.time;
            passaro.SendMessage("Flee", SendMessageOptions.DontRequireReceiver);
        }
    }

    /// <summary>Mostra o raio de susto na Scene View.</summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, raio);
    }
}
