using TMPro;
using UnityEngine;

/// <summary>
/// Lista de livros impressa no terminal de consulta (Nível 4).
/// Depois que o jogador tem o item "Lista de livros", a tecla L abre a lista
/// em qualquer lugar da biblioteca (L ou Esc fecham).
/// </summary>
public class ListaLivros : MonoBehaviour
{
    [Header("Volumes dos sons")]
    [Range(0f, 1f)] public float volumePapel = 0.6f;

    public string itemLista = "Lista de livros";
    public KeyCode tecla = KeyCode.L;

    [Tooltip("Painel do Canvas onde a lista aparece (o mesmo do terminal).")]
    public GameObject painel;

    public TMP_Text textoPainel;
    public AudioClip somPapel;

    private bool aberta;

    /// <summary>Abre e fecha a lista.</summary>
    void Update()
    {
        if (aberta)
        {
            if (Input.GetKeyDown(tecla) || Input.GetKeyDown(KeyCode.Escape))
                Fechar();
            return;
        }

        if (!Inventario.Tem(itemLista) || UIManager.MenuAberto || ArmarioEsconderijo.JogadorDentro)
            return;

        if (Input.GetKeyDown(tecla))
            Abrir();
    }

    /// <summary>Mostra a lista atualizada (livros já pegos aparecem riscados).</summary>
    public void Abrir()
    {
        if (textoPainel != null && SorteioLivros.Instancia != null)
            textoPainel.text = SorteioLivros.Instancia.TextoLista() + "\n<size=70%><color=#9aa>Aperte L para fechar</color></size>";

        UIManager.TocarSom2D(somPapel, volumePapel);
        UIManager.AbrirPainelComMouse(painel);
        aberta = true;
    }

    /// <summary>Fecha a lista.</summary>
    private void Fechar()
    {
        UIManager.TocarSom2D(somPapel, volumePapel);
        UIManager.FecharPainelComMouse(painel);
        aberta = false;
    }
}
