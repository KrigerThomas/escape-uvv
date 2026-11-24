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

    [Tooltip("Imagem mostrada de perto no meio da tela quando o segredo é encontrado (ex.: a carteirinha). Opcional.")]
    public Texture imagemDetalhe;

    /// <summary>Verdadeiro depois que o jogador encontrou este segredo.</summary>
    public bool Encontrado { get; private set; }

    /// <summary>Garante que o collider seja um trigger e avisa o GameManager que este segredo existe.</summary>
    void Start()
    {
        GetComponent<Collider>().isTrigger = true;

        if (GameManager.Instancia != null)
            GameManager.Instancia.RegistrarSegredo();
    }

    /// <summary>
    /// Enquanto o jogador está na área do segredo, confere se ele consegue ver o segredo
    /// (sem sebe ou parede no meio). Só assim o segredo conta como encontrado.
    /// </summary>
    /// <param name="outro">Collider que está dentro do trigger.</param>
    void OnTriggerStay(Collider outro)
    {
        if (Encontrado || !outro.CompareTag("Player"))
            return;

        if (JogadorVe(outro.transform))
            Descobrir();
    }

    /// <summary>Verdadeiro se nada sólido estiver entre os olhos do jogador e o segredo.</summary>
    /// <param name="jogador">Transform do jogador.</param>
    private bool JogadorVe(Transform jogador)
    {
        Vector3 olhos = jogador.position + Vector3.up * 1.5f;
        Vector3 alvo = transform.position + Vector3.up * 0.3f;
        Vector3 direcao = alvo - olhos;
        RaycastHit[] batidas = Physics.RaycastAll(olhos, direcao.normalized, direcao.magnitude, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit batida in batidas)
        {
            if (batida.transform.IsChildOf(transform) || batida.transform.IsChildOf(jogador))
                continue;
            // o banco ou a mesa em que o item está apoiado não conta
            if (Vector3.Distance(batida.point, alvo) < 0.8f)
                continue;
            return false;
        }
        return true;
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

        if (imagemDetalhe != null)
            DetalheItem.Mostrar(imagemDetalhe);

        // Com frase: mostra só a frase (ela já fala do achado). Sem frase: "Você encontrou " + nome.
        if (GameManager.Instancia != null)
        {
            if (string.IsNullOrEmpty(frase))
                GameManager.Instancia.ColetarItem(nomeSegredo, true);
            else
                GameManager.Instancia.ColetarItem(frase, false);
        }
    }
}
