using UnityEngine;

/// <summary>
/// Deixa um som de ambiente mais baixo quando o jogador entra numa área
/// (ex.: grilos e vagalumes altos no pátio e mais baixos dentro do labirinto).
/// O volume muda aos poucos (fade), sem cortes.
///
/// COMO USAR:
///  1. Coloque este script no mesmo objeto do AudioSource do som de fundo.
///  2. Ajuste "Centro da Área" e "Tamanho da Área" (a caixa verde na Scene View).
///  3. Ajuste os dois volumes e a velocidade do fade.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class VolumePorArea : MonoBehaviour
{
    [Header("Volumes")]
    [Range(0f, 1f)] public float volumeFora = 0.35f;
    [Range(0f, 1f)] public float volumeDentro = 0.12f;

    [Tooltip("Quanto o volume muda por segundo. Menor = fade mais lento.")]
    public float velocidadeFade = 0.15f;

    [Header("Área (no mundo)")]
    public Vector3 centroArea = Vector3.zero;
    public Vector3 tamanhoArea = new Vector3(42f, 10f, 42f);

    private AudioSource fonte;
    private Transform jogador;

    /// <summary>Acha o jogador e começa no volume certo, sem fade.</summary>
    void Start()
    {
        fonte = GetComponent<AudioSource>();
        GameObject objJogador = GameObject.FindWithTag("Player");
        if (objJogador != null)
            jogador = objJogador.transform;

        fonte.volume = VolumeAlvo();
    }

    /// <summary>Aproxima o volume do alvo um pouco a cada quadro.</summary>
    void Update()
    {
        fonte.volume = Mathf.MoveTowards(fonte.volume, VolumeAlvo(), velocidadeFade * Time.deltaTime);
    }

    /// <summary>Volume de dentro se o jogador está na área, senão o de fora.</summary>
    private float VolumeAlvo()
    {
        if (jogador == null)
            return volumeFora;

        Bounds area = new Bounds(centroArea, tamanhoArea);
        return area.Contains(jogador.position) ? volumeDentro : volumeFora;
    }

    /// <summary>Desenha a área na Scene View.</summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(centroArea, tamanhoArea);
    }
}
