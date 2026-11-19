using UnityEngine;

/// <summary>
/// Faíscas de curto-circuito no multiplug: de tempos em tempos solta um punhado
/// de partículas, toca o som da faísca e pisca uma luz laranja.
/// </summary>
public class EfeitoFaisca : MonoBehaviour
{
    [Tooltip("Sistema de partículas das faíscas.")]
    public ParticleSystem particulas;

    [Tooltip("Luz que pisca junto com a faísca (opcional).")]
    public Light luz;

    [Tooltip("Som tocado a cada faísca (opcional).")]
    public AudioSource som;

    [Tooltip("Menor tempo entre duas faíscas, em segundos.")]
    public float intervaloMinimo = 1.5f;

    [Tooltip("Maior tempo entre duas faíscas, em segundos.")]
    public float intervaloMaximo = 4f;

    [Tooltip("Quantas partículas cada faísca solta.")]
    public int quantidade = 15;

    private float proximaFaisca;
    private float brilhoMaximo;

    /// <summary>Guarda a intensidade da luz, apaga a luz e agenda a primeira faísca.</summary>
    void Start()
    {
        if (luz != null)
        {
            brilhoMaximo = luz.intensity;
            luz.intensity = 0f;
        }

        AgendarProxima();
    }

    /// <summary>Solta a faísca na hora certa e apaga a luz aos poucos.</summary>
    void Update()
    {
        if (Time.time >= proximaFaisca)
        {
            SoltarFaisca();
            AgendarProxima();
        }

        if (luz != null)
            luz.intensity = Mathf.MoveTowards(luz.intensity, 0f, brilhoMaximo * 6f * Time.deltaTime);
    }

    /// <summary>Emite as partículas, acende a luz e toca o som.</summary>
    private void SoltarFaisca()
    {
        if (particulas != null)
            particulas.Emit(quantidade);

        if (luz != null)
            luz.intensity = brilhoMaximo;

        if (som != null && som.clip != null)
        {
            som.pitch = Random.Range(0.9f, 1.15f);
            som.PlayOneShot(som.clip);
        }
    }

    /// <summary>Sorteia quando acontece a próxima faísca.</summary>
    private void AgendarProxima()
    {
        proximaFaisca = Time.time + Random.Range(intervaloMinimo, intervaloMaximo);
    }
}
