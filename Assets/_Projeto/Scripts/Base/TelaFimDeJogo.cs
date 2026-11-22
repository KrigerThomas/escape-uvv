using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Encerramento do jogo, mostrado depois que o calouro sai do bloco no Nível 5.
/// A chuva passou: o calouro vai embora andando pela frente da biblioteca enquanto a câmera
/// se afasta devagar. Aparece "Você escapou", sobem os créditos e no fim surgem os botões
/// "Jogar de novo" e "Menu principal". Qualquer tecla adianta os créditos.
/// </summary>
public class TelaFimDeJogo : MonoBehaviour
{
    [Header("Volumes dos sons")]
    [Range(0f, 1f)] public float volumeClique = 0.7f;

    [Header("Interface")]
    [Tooltip("Painel preto do começo (clareia aos poucos).")]
    public CanvasGroup telaPreta;
    [Tooltip("Grupo com o título \"Você escapou\".")]
    public CanvasGroup grupoTitulo;
    [Tooltip("Bloco de créditos que sobe pela tela.")]
    public RectTransform creditos;
    [Tooltip("Grupo com os botões do fim.")]
    public CanvasGroup grupoBotoes;
    public Button botaoJogarDeNovo;
    public Button botaoMenu;

    [Header("Cena")]
    [Tooltip("Calouro que vai embora andando.")]
    public Animator calouro;
    [Tooltip("Velocidade do calouro andando, em metros por segundo.")]
    public float velocidadeCalouro = 1.6f;
    [Tooltip("Câmera que se afasta devagar.")]
    public Transform cameraFinal;
    [Tooltip("Metros por segundo que a câmera sobe e recua.")]
    public float velocidadeCamera = 0.25f;

    [Header("Tempos")]
    [Tooltip("Segundos com o título na tela.")]
    public float tempoTitulo = 4f;
    [Tooltip("Segundos para os créditos subirem inteiros.")]
    public float tempoCreditos = 26f;

    [Header("Cenas e som")]
    [Tooltip("Cena carregada no Jogar de novo.")]
    public string primeiraCena = "Nivel1";
    [Tooltip("Cena da tela inicial.")]
    public string cenaMenu = "Menu";
    public AudioClip somClique;

    private bool adiantar;

    /// <summary>Solta o mouse, liga os botões e começa o encerramento.</summary>
    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.volume = Configuracoes.VolumeGeral;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        botaoJogarDeNovo.onClick.AddListener(JogarDeNovo);
        botaoMenu.onClick.AddListener(IrParaMenu);
        Mostrar(grupoBotoes, 0f);
        Mostrar(grupoTitulo, 0f);

        StartCoroutine(Encerramento());
    }

    /// <summary>Move o calouro para frente e a câmera para trás, devagar; uma tecla adianta os créditos.</summary>
    void Update()
    {
        if (calouro != null)
        {
            calouro.SetFloat("Velocidade", 3f);
            calouro.SetBool("NoChao", true);
            calouro.transform.position += calouro.transform.forward * velocidadeCalouro * Time.deltaTime;
        }

        if (cameraFinal != null)
            cameraFinal.position += (-cameraFinal.forward * 0.6f + Vector3.up * 0.4f) * velocidadeCamera * Time.deltaTime;

        if (Input.anyKeyDown && !Input.GetMouseButtonDown(0))
            adiantar = true;
    }

    /// <summary>Sequência do fim: clareia, título, créditos subindo e botões.</summary>
    private IEnumerator Encerramento()
    {
        yield return Fade(telaPreta, 1f, 0f, 3f);

        yield return Fade(grupoTitulo, 0f, 1f, 1.5f);
        yield return Esperar(tempoTitulo);
        yield return Fade(grupoTitulo, 1f, 0f, 1.5f);

        // a imagem escurece um pouco para os créditos ficarem legíveis
        StartCoroutine(Fade(telaPreta, 0f, 0.65f, 2f));

        // créditos: começam abaixo da tela e sobem até sair por cima
        RectTransform tela = (RectTransform)creditos.parent;
        float inicio = -tela.rect.height * 0.5f - 40f;
        float fim = tela.rect.height * 0.5f + creditos.rect.height + 40f;
        float t = 0f;
        adiantar = false;
        while (t < tempoCreditos)
        {
            t += Time.deltaTime * (adiantar ? 6f : 1f);
            creditos.anchoredPosition = new Vector2(creditos.anchoredPosition.x, Mathf.Lerp(inicio, fim, t / tempoCreditos));
            yield return null;
        }

        grupoBotoes.blocksRaycasts = true;
        grupoBotoes.interactable = true;
        yield return Fade(grupoBotoes, 0f, 1f, 1f);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(botaoJogarDeNovo.gameObject);
    }

    /// <summary>Espera alguns segundos; uma tecla encurta a espera.</summary>
    /// <param name="segundos">Tempo de espera.</param>
    private IEnumerator Esperar(float segundos)
    {
        adiantar = false;
        float t = 0f;
        while (t < segundos && !adiantar)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>Muda a transparência de um grupo aos poucos.</summary>
    /// <param name="grupo">Grupo a mudar.</param>
    /// <param name="de">Transparência inicial.</param>
    /// <param name="para">Transparência final.</param>
    /// <param name="duracao">Segundos da transição.</param>
    private IEnumerator Fade(CanvasGroup grupo, float de, float para, float duracao)
    {
        float t = 0f;
        while (t < duracao)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(de, para, Mathf.SmoothStep(0f, 1f, t / duracao));
            yield return null;
        }
        grupo.alpha = para;
    }

    /// <summary>Define a transparência de um grupo na hora e se ele recebe cliques.</summary>
    /// <param name="grupo">Grupo a mudar.</param>
    /// <param name="alfa">Transparência (0 a 1).</param>
    private void Mostrar(CanvasGroup grupo, float alfa)
    {
        grupo.alpha = alfa;
        grupo.interactable = alfa > 0.5f;
        grupo.blocksRaycasts = alfa > 0.5f;
    }

    /// <summary>Volta para o primeiro nível.</summary>
    public void JogarDeNovo()
    {
        UIManager.TocarSom2D(somClique, volumeClique);
        SceneManager.LoadScene(primeiraCena);
    }

    /// <summary>Volta para a tela inicial.</summary>
    public void IrParaMenu()
    {
        UIManager.TocarSom2D(somClique, volumeClique);
        SceneManager.LoadScene(cenaMenu);
    }
}
