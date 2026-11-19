using UnityEngine;

/// <summary>
/// Item que fica parado no cenário (em cima de uma mesa, numa estante) e é pego com E.
/// Diferente do ItemColetavel do Nível 1, ele não gira nem flutua: fica onde foi colocado.
/// Ao pegar, o nome vai para o Inventario e o objeto some.
/// </summary>
public class ItemPegavel : Interagivel
{
    [Header("Item")]
    [Tooltip("Nome do item no inventário. Outros scripts procuram por este nome (ex.: \"Chave do bloco\").")]
    public string nomeItem = "Item";

    [Tooltip("Marque para somar 1 no contador do GameManager (ex.: livros do Nível 4).")]
    public bool contaNoObjetivo = false;

    [Tooltip("Mensagem ao pegar. Vazio = \"Você pegou: <nome>\".")]
    public string mensagem = "";

    [Tooltip("Novo texto de objetivo depois de pegar (vazio = não muda).")]
    [TextArea] public string novoObjetivo = "";

    [Header("Som")]
    public AudioClip somPegar;
    [Range(0f, 1f)] public float volume = 0.8f;

    /// <summary>Se o texto da ação não foi trocado no Inspector, monta "pegar o item".</summary>
    void Start()
    {
        if (string.IsNullOrEmpty(textoAcao) || textoAcao == "interagir")
            textoAcao = "pegar: " + nomeItem;
    }

    /// <summary>Guarda o item, avisa, atualiza o objetivo e tira o objeto do cenário.</summary>
    public override void Interagir()
    {
        Inventario.Adicionar(nomeItem);
        UIManager.TocarSom2D(somPegar, volume);

        if (contaNoObjetivo && GameManager.Instancia != null)
            GameManager.Instancia.ColetarItem(nomeItem);

        if (!string.IsNullOrEmpty(mensagem))
            Avisar(mensagem);
        else if (!contaNoObjetivo)
            Avisar("Você pegou: " + nomeItem);

        if (!string.IsNullOrEmpty(novoObjetivo) && UI != null)
            UI.MostrarObjetivo(novoObjetivo);

        gameObject.SetActive(false);
    }
}
