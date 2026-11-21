using UnityEngine;

/// <summary>
/// Porta que fica fechada até receber a ordem Abrir(). Depois desliza com
/// Transform.Translate até percorrer a distância configurada.
/// </summary>
public class PortaDeslizante : MonoBehaviour
{
    [Tooltip("Direção, no mundo, para onde a porta desliza.")]
    public Vector3 direcaoAbertura = Vector3.forward;

    [Tooltip("Distância que a porta percorre, em metros.")]
    public float distancia = 2f;

    [Tooltip("Tempo para abrir por completo, em segundos.")]
    public float duracao = 1.5f;

    [Tooltip("Som tocado quando a porta começa a abrir.")]
    public AudioClip somAbrir;

    [Tooltip("Volume do som de abrir.")]
    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("Marque para o som tocar perto do jogador (fica mais alto e claro mesmo com a porta longe).")]
    public bool somPertoDoJogador = false;

    [Tooltip("Marque para a porta levar o mesmo tempo do som para abrir (fica sincronizado).")]
    public bool acompanharSom = true;

    [Tooltip("Segundos a mais depois do som (porta mais lenta).")]
    public float tempoExtra = 0f;

    private bool abrindo;
    private float percorrido;

    /// <summary>Verdadeiro depois que a porta recebeu a ordem de abrir.</summary>
    public bool Aberta => abrindo;

    /// <summary>Começa a abrir a porta (chamado pelo GameManager).</summary>
    public void Abrir()
    {
        if (abrindo)
            return;

        abrindo = true;
        if (acompanharSom && somAbrir != null)
            duracao = somAbrir.length + tempoExtra;

        if (somAbrir != null)
        {
            Vector3 ponto = somPertoDoJogador && Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(somAbrir, ponto, volume);
        }
    }

    /// <summary>Move a porta um pouco a cada quadro até chegar ao fim do trilho.</summary>
    void Update()
    {
        if (!abrindo || percorrido >= distancia)
            return;

        float passo = Mathf.Min(distancia / duracao * Time.deltaTime, distancia - percorrido);
        transform.Translate(direcaoAbertura.normalized * passo, Space.World);
        percorrido += passo;
    }
}
