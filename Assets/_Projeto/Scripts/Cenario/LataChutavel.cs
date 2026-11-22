using UnityEngine;

/// <summary>
/// Latinha jogada no chão que o jogador chuta ao passar por cima.
/// Usa as duas formas de mexer um Rigidbody pela física vistas em aula:
/// AddForce (empurra a lata para frente e um pouco para cima) e
/// AddTorque (faz a lata sair girando).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class LataChutavel : MonoBehaviour
{
    [Header("Chute")]
    [Tooltip("Força do empurrão para frente quando o jogador encosta.")]
    public float forcaChute = 0.6f;

    [Tooltip("Quanto do empurrão vai para cima (0 = rente ao chão).")]
    [Range(0f, 1f)] public float forcaParaCima = 0.35f;

    [Tooltip("Força de rotação aplicada no chute. Quanto maior, mais a lata gira.")]
    public float torqueChute = 0.02f;

    [Tooltip("Segundos de espera até a lata poder ser chutada de novo.")]
    public float intervaloEntreChutes = 0.4f;

    [Header("Som")]
    public AudioClip somChute;
    [Range(0f, 1f)] public float volumeChute = 0.6f;

    private Rigidbody corpo;
    private float ultimoChute = -10f;

    /// <summary>Guarda o Rigidbody da lata.</summary>
    void Awake()
    {
        corpo = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Chamado pela física quando algo bate na lata. Se for o jogador, chuta.
    /// </summary>
    /// <param name="colisao">Dados da batida (quem bateu, ponto de contato etc.).</param>
    void OnCollisionEnter(Collision colisao)
    {
        if (!colisao.collider.CompareTag("Player"))
            return;

        Chutar(colisao.transform.position);
    }

    /// <summary>
    /// Empurra a lata para longe de quem chutou (AddForce) e faz ela girar (AddTorque).
    /// </summary>
    /// <param name="origem">Posição de quem chutou. A lata vai para o lado oposto.</param>
    public void Chutar(Vector3 origem)
    {
        if (Time.time - ultimoChute < intervaloEntreChutes)
            return;
        ultimoChute = Time.time;

        // direção do chute: de quem chutou até a lata, sem a parte vertical
        Vector3 direcao = transform.position - origem;
        direcao.y = 0f;
        if (direcao.sqrMagnitude < 0.0001f)
            direcao = transform.forward;
        direcao.Normalize();

        Vector3 impulso = (direcao + Vector3.up * forcaParaCima) * forcaChute;
        corpo.AddForce(impulso, ForceMode.Impulse);

        // eixo de giro deitado e perpendicular ao chute, com um pouco de sorte para cada chute ser diferente
        Vector3 eixo = Vector3.Cross(Vector3.up, direcao) + Random.insideUnitSphere * 0.5f;
        corpo.AddTorque(eixo.normalized * torqueChute, ForceMode.Impulse);

        if (somChute != null)
            AudioSource.PlayClipAtPoint(somChute, transform.position, volumeChute);
    }
}
