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

        if (somAbrir != null)
            AudioSource.PlayClipAtPoint(somAbrir, transform.position);
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
