using UnityEngine;

/// <summary>
/// Cadeira de escritório que gira em volta do próprio eixo quando o jogador
/// encosta nela, usando Rigidbody.AddTorque. A cadeira não sai do lugar.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CadeiraGiratoria : MonoBehaviour
{
    [Tooltip("Força do giro aplicada ao encostar.")]
    public float forcaGiro = 4f;

    [Tooltip("Quanto o giro perde de velocidade com o tempo.")]
    public float freio = 1.2f;

    private Rigidbody rb;

    /// <summary>Configura o Rigidbody para só girar no eixo Y.</summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePosition |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
        rb.angularDamping = freio;
    }

    /// <summary>Quando o jogador bate na cadeira, aplica um torque para um dos lados.</summary>
    /// <param name="colisao">Dados da colisão.</param>
    void OnCollisionEnter(Collision colisao)
    {
        if (!colisao.collider.CompareTag("Player"))
            return;

        float sentido = Random.value < 0.5f ? -1f : 1f;
        rb.AddTorque(Vector3.up * forcaGiro * sentido, ForceMode.Impulse);
    }
}
