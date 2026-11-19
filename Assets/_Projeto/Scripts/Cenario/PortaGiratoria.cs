using UnityEngine;

/// <summary>
/// Porta que abre girando em volta da dobradiça (como a porta corta-fogo do corredor).
/// Coloque este script no objeto "Dobradica" (o pivô fica na lateral da porta)
/// e deixe a folha da porta como filha dele. Fica fechada até receber Abrir().
/// </summary>
public class PortaGiratoria : MonoBehaviour
{
    [Tooltip("Quantos graus a porta gira para abrir. Use valor negativo para abrir para o outro lado.")]
    public float anguloAbertura = 100f;

    [Tooltip("Tempo para abrir por completo, em segundos.")]
    public float duracao = 1.2f;

    [Tooltip("Som tocado quando a porta começa a abrir.")]
    public AudioClip somAbrir;

    [Range(0f, 1f)] public float volume = 0.9f;

    private bool abrindo;
    private float tempo;
    private Quaternion rotacaoFechada;
    private Quaternion rotacaoAberta;

    /// <summary>Verdadeiro depois que a porta recebeu a ordem de abrir.</summary>
    public bool Aberta => abrindo;

    /// <summary>Guarda a rotação fechada e calcula a rotação aberta.</summary>
    void Awake()
    {
        rotacaoFechada = transform.localRotation;
        rotacaoAberta = rotacaoFechada * Quaternion.Euler(0f, anguloAbertura, 0f);
    }

    /// <summary>Começa a abrir a porta (chamado pelo GameManager).</summary>
    public void Abrir()
    {
        if (abrindo)
            return;

        abrindo = true;

        if (somAbrir != null)
            AudioSource.PlayClipAtPoint(somAbrir, transform.position, volume);
    }

    /// <summary>Gira a porta um pouco a cada quadro, desacelerando no fim (SmoothStep).</summary>
    void Update()
    {
        if (!abrindo || tempo >= duracao)
            return;

        tempo += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, tempo / duracao);
        transform.localRotation = Quaternion.Slerp(rotacaoFechada, rotacaoAberta, t);
    }
}
