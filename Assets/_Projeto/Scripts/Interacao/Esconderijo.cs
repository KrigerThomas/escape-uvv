using UnityEngine;

/// <summary>
/// Área invisível onde o jogador fica escondido (ex.: atrás dos armários do Nível 5).
/// Enquanto o jogador está dentro, a Professora não consegue vê-lo.
/// Precisa de um Collider marcado como Is Trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Esconderijo : MonoBehaviour
{
    /// <summary>Verdadeiro enquanto o jogador está dentro de algum esconderijo.</summary>
    public static bool JogadorEscondido { get; private set; }

    [Tooltip("Mensagem mostrada ao entrar no esconderijo. Deixe vazio para não mostrar nada.")]
    public string textoAoEntrar = "Você se escondeu atrás dos armários";

    /// <summary>Garante que o collider seja um trigger.</summary>
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>Começa a cena com o jogador visível.</summary>
    void Awake()
    {
        JogadorEscondido = false;
    }

    /// <summary>Jogador entrou: fica escondido.</summary>
    void OnTriggerEnter(Collider outro)
    {
        if (!outro.CompareTag("Player"))
            return;

        JogadorEscondido = true;

        if (!string.IsNullOrEmpty(textoAoEntrar) && GameManager.Instancia != null && GameManager.Instancia.ui != null)
            GameManager.Instancia.ui.MostrarAviso(textoAoEntrar);
    }

    /// <summary>Jogador saiu: volta a ficar visível.</summary>
    void OnTriggerExit(Collider outro)
    {
        if (outro.CompareTag("Player"))
            JogadorEscondido = false;
    }

    /// <summary>Evita que o jogador fique "escondido para sempre" ao trocar de cena.</summary>
    void OnDestroy()
    {
        JogadorEscondido = false;
    }

    /// <summary>Mostra a área do esconderijo na Scene View.</summary>
    void OnDrawGizmos()
    {
        BoxCollider caixa = GetComponent<BoxCollider>();
        if (caixa == null)
            return;

        Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(caixa.center, caixa.size);
    }
}
