using System.Collections;
using UnityEngine;

/// <summary>
/// Catraca com reconhecimento facial da biblioteca (Nível 4), igual à da UVV.
/// Funciona sozinha quando o jogador chega perto (não precisa apertar nada):
///
/// Entrando (vindo do saguão):
///  - com mochila: tela vermelha, bipe de erro, a passagem continua fechada;
///  - sem mochila: tela azul "lendo o rosto", depois verde, bipe e a passagem abre.
/// Saindo (vindo do acervo):
///  - com livros sem empréstimo registrado: alarme antifurto, luz vermelha piscando e catraca travada;
///  - com empréstimo registrado (ou sem livros): abre normalmente.
///
/// O lado é decidido pela posição do jogador: atrás da catraca = saguão, na frente = acervo
/// (a "frente" é a seta azul do objeto, o eixo Z).
/// </summary>
[RequireComponent(typeof(Collider))]
public class CatracaFacial : MonoBehaviour
{
    [Header("Peças")]
    [Tooltip("Objeto com as portinholas de vidro e o collider que bloqueia a passagem. Some quando a catraca libera.")]
    public GameObject bloqueio;

    [Tooltip("Braço da portinhola (pivô na dobradiça). Gira para abrir quando a catraca libera. Opcional.")]
    public Transform braco;

    [Tooltip("Quantos graus o braço gira para abrir (negativo = para o outro lado).")]
    public float anguloBraco = -90f;

    [Tooltip("Renderers das telas (o material delas troca de cor).")]
    public Renderer[] telas;

    [Tooltip("Luz que pisca vermelha no alarme (opcional).")]
    public Light luzAlarme;

    [Header("Regras")]
    [Tooltip("Item que impede a entrada.")]
    public string itemProibidoNaEntrada = "Mochila";

    [Tooltip("Item que prova que o empréstimo foi registrado.")]
    public string itemEmprestimo = "Empréstimo registrado";

    [Tooltip("Começo do nome dos livros no inventário (ex.: \"Livro: Cálculo\").")]
    public string prefixoLivros = "Livro";

    [Tooltip("Segundos que a passagem fica aberta.")]
    public float tempoAberta = 3f;

    [Header("Cores das telas")]
    public Color corEspera = new Color(0.2f, 0.35f, 0.9f);
    public Color corLiberado = new Color(0.1f, 0.9f, 0.3f);
    public Color corNegado = new Color(1f, 0.1f, 0.1f);

    [Header("Sons")]
    public AudioClip somLiberado;
    public AudioClip somNegado;
    public AudioClip somAlarme;

    [Header("Textos")]
    public string textoMochila = "Não é permitido entrar com mochila, bolsa ou pasta. Use o guarda-volumes.";
    public string textoAlarme = "ALARME ANTIFURTO! Os livros não foram emprestados. Use o autoatendimento.";

    private bool ocupada;
    private MaterialPropertyBlock bloco;
    private Quaternion bracoFechado;
    private Coroutine giro;

    /// <summary>Começa fechada e com a tela azul.</summary>
    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
        bloco = new MaterialPropertyBlock();
        if (braco != null)
            bracoFechado = braco.localRotation;
        Fechar();
        PintarTelas(corEspera);
        if (luzAlarme != null)
            luzAlarme.enabled = false;
    }

    /// <summary>O jogador chegou perto: decide o que acontece conforme o lado e o que ele carrega.</summary>
    void OnTriggerEnter(Collider outro)
    {
        if (ocupada || !outro.CompareTag("Player"))
            return;

        bool vindoDoSaguao = transform.InverseTransformPoint(outro.transform.position).z < 0f;
        StartCoroutine(vindoDoSaguao ? Entrada() : Saida());
    }

    /// <summary>Entrada: confere a mochila e lê o rosto.</summary>
    private IEnumerator Entrada()
    {
        ocupada = true;

        if (Inventario.Tem(itemProibidoNaEntrada))
        {
            yield return Negar(textoMochila, somNegado);
        }
        else
        {
            PintarTelas(corEspera);
            Avisar("Reconhecimento facial...");
            yield return new WaitForSeconds(0.8f);
            yield return Liberar("Acesso liberado. Bem-vindo à Biblioteca Central.");
        }

        ocupada = false;
    }

    /// <summary>Saída: dispara o alarme se houver livro sem empréstimo.</summary>
    private IEnumerator Saida()
    {
        ocupada = true;

        if (TemLivro() && !Inventario.Tem(itemEmprestimo))
        {
            UIManager.TocarSom2D(somAlarme, 0.9f);
            Avisar(textoAlarme);
            for (int i = 0; i < 8; i++)
            {
                PintarTelas(i % 2 == 0 ? corNegado : Color.black);
                if (luzAlarme != null)
                    luzAlarme.enabled = i % 2 == 0;
                yield return new WaitForSeconds(0.25f);
            }
            if (luzAlarme != null)
                luzAlarme.enabled = false;
            PintarTelas(corEspera);
        }
        else
        {
            yield return Liberar("Volte sempre!");
        }

        ocupada = false;
    }

    /// <summary>Tela verde, bipe e passagem aberta por alguns segundos.</summary>
    private IEnumerator Liberar(string texto)
    {
        PintarTelas(corLiberado);
        UIManager.TocarSom2D(somLiberado, 0.8f);
        Avisar(texto);
        if (bloqueio != null)
            bloqueio.SetActive(false);
        GirarBraco(true);

        yield return new WaitForSeconds(tempoAberta);

        Fechar();
        PintarTelas(corEspera);
    }

    /// <summary>Tela vermelha e bipe de erro.</summary>
    private IEnumerator Negar(string texto, AudioClip som)
    {
        PintarTelas(corNegado);
        UIManager.TocarSom2D(som, 0.8f);
        Avisar(texto);
        yield return new WaitForSeconds(1.5f);
        PintarTelas(corEspera);
    }

    /// <summary>Verdadeiro se o jogador carrega algum livro do acervo.</summary>
    private bool TemLivro()
    {
        Livro[] livros = FindObjectsByType<Livro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Livro l in livros)
            if (Inventario.Tem(l.nomeItem))
                return true;
        return false;
    }

    /// <summary>Liga o bloqueio da passagem.</summary>
    private void Fechar()
    {
        if (bloqueio != null)
            bloqueio.SetActive(true);
        GirarBraco(false);
    }

    /// <summary>Abre ou fecha o braço da portinhola, girando aos poucos.</summary>
    private void GirarBraco(bool abrir)
    {
        if (braco == null || !isActiveAndEnabled)
            return;
        if (giro != null)
            StopCoroutine(giro);
        Quaternion alvo = abrir ? bracoFechado * Quaternion.Euler(0f, anguloBraco, 0f) : bracoFechado;
        giro = StartCoroutine(Girar(alvo));
    }

    private IEnumerator Girar(Quaternion alvo)
    {
        Quaternion inicio = braco.localRotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.45f;
            braco.localRotation = Quaternion.Slerp(inicio, alvo, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
    }

    /// <summary>Troca a cor (e o brilho) das telas.</summary>
    private void PintarTelas(Color cor)
    {
        if (telas == null)
            return;

        foreach (Renderer r in telas)
        {
            if (r == null)
                continue;
            r.GetPropertyBlock(bloco);
            bloco.SetColor("_BaseColor", cor);
            bloco.SetColor("_EmissionColor", cor * 2f);
            r.SetPropertyBlock(bloco);
        }
    }

    /// <summary>Mostra um aviso na tela do jogador.</summary>
    private void Avisar(string texto)
    {
        if (GameManager.Instancia != null && GameManager.Instancia.ui != null)
            GameManager.Instancia.ui.MostrarAviso(texto);
    }
}
