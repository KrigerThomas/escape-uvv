using UnityEngine;

/// <summary>
/// Som dos passos do jogador. Toca um passo a cada tantos metros andados no chão,
/// sorteando um dos sons da lista e variando um pouco o tom (para não ficar repetitivo).
/// Correndo, os passos ficam mais rápidos naturalmente (anda mais metros por segundo).
/// Sem sons na lista, o script não faz nada.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PassosJogador : MonoBehaviour
{
    [Tooltip("Sons de passo (vários, para variar).")]
    public AudioClip[] passos;

    [Tooltip("Metros andados entre um passo e outro.")]
    public float distanciaPasso = 0.8f;

    [Range(0f, 1f)] public float volume = 0.35f;

    [Tooltip("Variação do tom de um passo para outro.")]
    public float variacaoTom = 0.08f;

    private Rigidbody rb;
    private AudioSource fonte;
    private float andado;

    /// <summary>Cria a fonte de som dos passos.</summary>
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
    }

    /// <summary>Soma a distância andada no chão e toca um passo quando completa a distância.</summary>
    void Update()
    {
        if (passos == null || passos.Length == 0 || rb.isKinematic || Time.timeScale == 0f)
            return;

        Vector3 velocidade = rb.linearVelocity;
        velocidade.y = 0f;
        float rapidez = velocidade.magnitude;

        if (rapidez < 0.4f || !NoChao())
        {
            andado = distanciaPasso * 0.6f; // o primeiro passo sai logo que começa a andar
            return;
        }

        andado += rapidez * Time.deltaTime;
        if (andado >= distanciaPasso)
        {
            andado = 0f;
            fonte.pitch = 1f + Random.Range(-variacaoTom, variacaoTom);
            fonte.PlayOneShot(passos[Random.Range(0, passos.Length)], volume);
        }
    }

    /// <summary>Confere se há chão logo abaixo dos pés.</summary>
    private bool NoChao()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.45f, ~0, QueryTriggerInteraction.Ignore);
    }
}
