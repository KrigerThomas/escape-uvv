using UnityEngine;

/// <summary>
/// Cadeira do laboratório que sai do lugar quando o jogador esbarra nela.
/// Usa a física do Rigidbody: AddForce empurra a cadeira para longe do jogador
/// e AddTorque gira a cadeira um pouco em volta do eixo vertical, como uma cadeira
/// de verdade que escorrega torta quando alguém passa apressado.
/// A cadeira não tomba: as rotações em X e Z ficam travadas no Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CadeiraEmpurravel : MonoBehaviour
{
    [Tooltip("Força do empurrão quando o jogador esbarra.")]
    public float forcaEmpurrao = 9f;

    [Tooltip("Força de rotação (torque) em volta do eixo vertical. O lado do giro é sorteado.")]
    public float torqueGiro = 10f;

    [Tooltip("Segundos de espera até a cadeira poder ser empurrada de novo.")]
    public float intervalo = 0.5f;

    [Header("Som")]
    public AudioClip somArrastar;
    [Range(0f, 1f)] public float volumeArrastar = 0.5f;

    private Rigidbody corpo;
    private float ultimoEmpurrao = -10f;

    /// <summary>Guarda o Rigidbody e trava a cadeira para ela não tombar.</summary>
    void Awake()
    {
        corpo = GetComponent<Rigidbody>();
        corpo.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    /// <summary>
    /// Chamado pela física quando algo encosta na cadeira. Se for o jogador, empurra.
    /// </summary>
    /// <param name="colisao">Dados da batida (quem bateu, pontos de contato).</param>
    void OnCollisionEnter(Collision colisao)
    {
        if (colisao.collider.CompareTag("Player"))
            Empurrar(colisao.transform.position);
    }

    /// <summary>
    /// Empurra a cadeira para o lado oposto de quem esbarrou (AddForce)
    /// e faz ela girar um pouco no chão (AddTorque).
    /// </summary>
    /// <param name="origem">Posição de quem esbarrou.</param>
    public void Empurrar(Vector3 origem)
    {
        if (Time.time - ultimoEmpurrao < intervalo)
            return;
        ultimoEmpurrao = Time.time;

        Vector3 direcao = transform.position - origem;
        direcao.y = 0f;
        if (direcao.sqrMagnitude < 0.0001f)
            direcao = transform.forward;

        corpo.AddForce(direcao.normalized * forcaEmpurrao, ForceMode.Impulse);

        float lado = Random.value < 0.5f ? -1f : 1f;
        corpo.AddTorque(Vector3.up * torqueGiro * lado, ForceMode.Impulse);

        if (somArrastar != null)
            AudioSource.PlayClipAtPoint(somArrastar, transform.position, volumeArrastar);
    }
}
