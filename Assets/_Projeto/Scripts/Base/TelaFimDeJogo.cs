using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tela final, mostrada depois que o calouro escapa da sala do Nível 5.
/// Solta o cursor do mouse e liga os botões "Jogar de novo" e "Sair".
/// </summary>
public class TelaFimDeJogo : MonoBehaviour
{
    [Tooltip("Botão que volta para o Nível 1.")]
    public Button botaoJogarDeNovo;

    [Tooltip("Botão que fecha o jogo (no Editor, só para o Play).")]
    public Button botaoSair;

    [Tooltip("Som de clique dos botões.")]
    public AudioClip somClique;

    [Tooltip("Nome da cena carregada ao jogar de novo.")]
    public string primeiraCena = "Nivel1";

    /// <summary>Mostra o cursor e liga os botões.</summary>
    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (botaoJogarDeNovo != null)
            botaoJogarDeNovo.onClick.AddListener(JogarDeNovo);

        if (botaoSair != null)
            botaoSair.onClick.AddListener(Sair);
    }

    /// <summary>Volta para o primeiro nível.</summary>
    public void JogarDeNovo()
    {
        UIManager.TocarSom2D(somClique, 0.7f);
        SceneManager.LoadScene(primeiraCena);
    }

    /// <summary>Fecha o jogo. Dentro do Editor, apenas sai do modo Play.</summary>
    public void Sair()
    {
        UIManager.TocarSom2D(somClique, 0.7f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
