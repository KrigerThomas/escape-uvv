using UnityEngine;

/// <summary>
/// Segredo (easter egg) escondido no nível. Diferente do ItemColetavel, o objeto
/// não some quando o jogador encontra: ele só toca um brilho e um som, mostra uma
/// mensagem e soma 1 no contador de segredos do GameManager.
///
/// COMO USAR:
///  1. Coloque este script no objeto do segredo (o gato, um livro, um lanche...).
///  2. O objeto precisa de um Collider marcado como "Is Trigger" (o script marca sozinho).
///     Deixe o collider um pouco maior que o objeto para o jogador não precisar encostar exatamente nele.
///  3. Preencha o nome e a frase que aparecem na tela.
/// Cada Segredo se registra no GameManager ao começar a cena, então o total do contador
/// é atualizado sozinho quando alguém adiciona ou remove um segredo.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Segredo : MonoBehaviour
{
    [Header("Texto")]
    [Tooltip("Nome curto do segredo, mostrado no aviso. Ex.: Gato do campus.")]
    public string nomeSegredo = "Segredo";

    [Tooltip("Frase curta que aparece junto com o nome (opcional).")]
    public string frase = "";

    [Header("Efeitos")]
    [Tooltip("Partícula criada no lugar do segredo quando ele é encontrado.")]
    public GameObject efeitoDescoberta;

    [Tooltip("Som tocado ao encontrar o segredo.")]
    public AudioClip somDescoberta;

    [Range(0f, 1f)] public float volume = 0.8f;

    [Tooltip("Objeto de dica (ex.: brilho em volta do segredo) que é desligado depois de encontrado. Opcional.")]
    public GameObject dicaVisual;

    /// <summary>Verdadeiro depois que o jogador encontrou este segredo.</summary>
    public bool Encontrado { get; private set; }

    /// <summary>Garante que o collider seja um trigger e avisa o GameManager que este segredo existe.</summary>
    void Start()
    {
        GetComponent<Collider>().isTrigger = true;

        if (GameManager.Instancia != null)
            GameManager.Instancia.RegistrarSegredo();
    }

    /// <summary>Detecta o jogador (tag Player) entrando na área do segredo.</summary>
    /// <param name="outro">Collider que entrou no trigger.</param>
    void OnTriggerEnter(Collider outro)
    {
        if (Encontrado || !outro.CompareTag("Player"))
            return;

        Descobrir();
    }

    /// <summary>Toca o efeito e o som, desliga a dica visual e soma o segredo no GameManager.</summary>
    private void Descobrir()
    {
        Encontrado = true;

        if (efeitoDescoberta != null)
        {
            GameObject efeito = Instantiate(efeitoDescoberta, transform.position + Vector3.up * 0.3f, Quaternion.identity);
            Destroy(efeito, 3f);
        }

        if (somDescoberta != null)
            AudioSource.PlayClipAtPoint(somDescoberta, transform.position, volume);

        if (dicaVisual != null)
            dicaVisual.SetActive(false);

        string texto = string.IsNullOrEmpty(frase) ? nomeSegredo : nomeSegredo + ". " + frase;

        if (GameManager.Instancia != null)
            GameManager.Instancia.ColetarItem(texto);
    }
}
