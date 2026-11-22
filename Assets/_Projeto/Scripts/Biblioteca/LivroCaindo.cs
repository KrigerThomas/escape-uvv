using UnityEngine;

/// <summary>
/// Livro que despenca da estante no Nível 4. Ganha um Rigidbody na hora,
/// é empurrado para fora da prateleira com AddForce e gira no ar com AddTorque.
/// Quando bate no chão (OnCollisionEnter) toca o som da queda uma vez.
/// O EventosBiblioteca coloca este script num livro da estante perto do jogador.
/// </summary>
public class LivroCaindo : MonoBehaviour
{
    [Tooltip("Força que tira o livro da prateleira.")]
    public float forcaEmpurrao = 1.2f;

    [Tooltip("Força do giro do livro enquanto cai.")]
    public float torqueGiro = 0.05f;

    [Tooltip("Som do livro batendo no chão.")]
    public AudioClip somQueda;

    [Range(0f, 1f)] public float volume = 0.8f;

    private Rigidbody corpo;
    private AudioSource fonte;
    private bool tocouSom;

    /// <summary>Prepara a física do livro e empurra ele para fora da estante.</summary>
    /// <param name="direcao">Para onde o livro sai (de dentro da estante para o corredor).</param>
    public void Derrubar(Vector3 direcao)
    {
        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();

        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0.8f;
        fonte.minDistance = 4f;
        fonte.maxDistance = 25f;

        corpo = gameObject.AddComponent<Rigidbody>();
        corpo.mass = 0.8f;
        corpo.interpolation = RigidbodyInterpolation.Interpolate;
        corpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        direcao.y = 0f;
        corpo.AddForce(direcao.normalized * forcaEmpurrao + Vector3.up * 0.3f, ForceMode.Impulse);
        // gira em volta do eixo de lado, como um livro que tomba da prateleira
        Vector3 eixo = Vector3.Cross(Vector3.up, direcao.normalized);
        corpo.AddTorque(eixo * torqueGiro + Random.insideUnitSphere * torqueGiro * 0.3f, ForceMode.Impulse);
    }

    /// <summary>Toca o som da queda na primeira batida forte.</summary>
    /// <param name="colisao">Dados da batida.</param>
    void OnCollisionEnter(Collision colisao)
    {
        if (tocouSom || corpo == null || colisao.relativeVelocity.magnitude < 1.2f)
            return;

        tocouSom = true;
        if (somQueda != null)
            fonte.PlayOneShot(somQueda, volume);
    }
}
