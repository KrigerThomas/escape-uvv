using UnityEngine;

/// <summary>
/// Sons de ambiente que aparecem de vez em quando em volta do jogador
/// (ex.: coruja, sapo, cachorro latindo longe). A cada intervalo sorteado,
/// escolhe um som do vetor e toca em um ponto aleatório ao redor do jogador,
/// para dar a sensação de que o parque está vivo.
///
/// COMO USAR: coloque o script em um objeto vazio e arraste os áudios para o vetor "Sons".
/// O som contínuo de fundo (grilos) fica em outro AudioSource, com Loop marcado.
/// </summary>
public class SomAmbienteAleatorio : MonoBehaviour
{
    [Tooltip("Vetor de sons possíveis. A cada vez, um deles é sorteado.")]
    public AudioClip[] sons;

    [Tooltip("Menor tempo entre dois sons, em segundos.")]
    public float intervaloMinimo = 8f;

    [Tooltip("Maior tempo entre dois sons, em segundos.")]
    public float intervaloMaximo = 20f;

    [Tooltip("Distância mínima e máxima do jogador onde o som aparece.")]
    public Vector2 distancia = new Vector2(8f, 20f);

    [Range(0f, 1f)] public float volume = 0.6f;

    private Transform jogador;
    private float proximoSom;

    /// <summary>Procura o jogador pela tag e agenda o primeiro som.</summary>
    void Start()
    {
        GameObject objJogador = GameObject.FindWithTag("Player");
        if (objJogador != null)
            jogador = objJogador.transform;

        AgendarProximo();
    }

    /// <summary>Quando chega a hora, toca um som sorteado e agenda o seguinte.</summary>
    void Update()
    {
        if (Time.time < proximoSom)
            return;

        TocarSomAleatorio();
        AgendarProximo();
    }

    /// <summary>
    /// Sorteia a posição no vetor de sons e um ponto em volta do jogador,
    /// e toca o som 3D ali.
    /// </summary>
    private void TocarSomAleatorio()
    {
        if (sons == null || sons.Length == 0 || jogador == null)
            return;

        AudioClip som = sons[Random.Range(0, sons.Length)];
        if (som == null)
            return;

        Vector2 direcao = Random.insideUnitCircle.normalized * Random.Range(distancia.x, distancia.y);
        Vector3 ponto = jogador.position + new Vector3(direcao.x, 2f, direcao.y);
        AudioSource.PlayClipAtPoint(som, ponto, volume);
    }

    /// <summary>Sorteia quando o próximo som vai tocar.</summary>
    private void AgendarProximo()
    {
        proximoSom = Time.time + Random.Range(intervaloMinimo, intervaloMaximo);
    }
}
