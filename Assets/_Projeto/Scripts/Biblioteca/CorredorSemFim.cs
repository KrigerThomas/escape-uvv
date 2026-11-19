using UnityEngine;

/// <summary>
/// O "corredor que não acaba" do Nível 4 (efeito de espaço liminar / backrooms).
///
/// Truque: quando o jogador cruza esta área invisível andando para o fundo do corredor,
/// ele é levado para trás, alguns metros, sem perceber (as estantes são todas iguais,
/// então a imagem não muda). Parece que o corredor nunca termina.
/// Depois de algumas voltas o truque desliga, as luzes do fundo acendem e o livro aparece.
///
/// Importante: o "Deslocamento" precisa ser um múltiplo da largura das estantes
/// para que o cenário depois do salto seja idêntico ao de antes.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CorredorSemFim : MonoBehaviour
{
    [Tooltip("Quanto o jogador é levado de volta (direção e distância, no mundo).")]
    public Vector3 deslocamento = new Vector3(8.32f, 0f, 0f);

    [Tooltip("Quantas vezes o corredor se repete antes de \"terminar\".")]
    public int voltas = 3;

    [Tooltip("Objetos ligados quando o truque acaba (ex.: luzes do fundo, o livro).")]
    public GameObject[] ligarNoFim;

    [Tooltip("Objetos desligados quando o truque acaba (ex.: a névoa que esconde o fundo).")]
    public GameObject[] desligarNoFim;

    [Tooltip("Frases mostradas a cada volta (uma por volta).")]
    public string[] frases = { "...", "Essa placa de novo?", "Esse corredor não acaba..." };

    public string fraseFinal = "As luzes do fundo acenderam.";
    public AudioClip somVolta;
    public AudioClip somFim;

    private int contador;

    /// <summary>Garante que seja um trigger e que o fim comece escondido.</summary>
    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
        foreach (GameObject g in ligarNoFim)
            if (g != null)
                g.SetActive(false);
    }

    /// <summary>Cada vez que o jogador cruza, volta alguns metros, até completar as voltas.</summary>
    void OnTriggerEnter(Collider outro)
    {
        if (!outro.CompareTag("Player") || contador >= voltas)
            return;

        Rigidbody rb = outro.attachedRigidbody;
        if (rb == null)
            return;

        // Só leva de volta quem está indo para o fundo (andando contra o deslocamento).
        if (Vector3.Dot(rb.linearVelocity, deslocamento) > 0f)
            return;

        rb.position += deslocamento;
        rb.transform.position += deslocamento;
        if (Camera.main != null)
            Camera.main.transform.position += deslocamento;
        Physics.SyncTransforms();

        if (contador < frases.Length)
            Avisar(frases[contador]);
        UIManager.TocarSom2D(somVolta, 0.5f);

        contador++;
        if (contador >= voltas)
            Terminar();
    }

    /// <summary>Desliga o truque e mostra o fim do corredor.</summary>
    private void Terminar()
    {
        foreach (GameObject g in desligarNoFim)
            if (g != null)
                g.SetActive(false);

        foreach (GameObject g in ligarNoFim)
            if (g != null)
                g.SetActive(true);

        UIManager.TocarSom2D(somFim, 0.7f);
        Avisar(fraseFinal);
    }

    /// <summary>Mostra um aviso na tela.</summary>
    private void Avisar(string texto)
    {
        if (GameManager.Instancia != null && GameManager.Instancia.ui != null && !string.IsNullOrEmpty(texto))
            GameManager.Instancia.ui.MostrarAviso(texto);
    }
}
