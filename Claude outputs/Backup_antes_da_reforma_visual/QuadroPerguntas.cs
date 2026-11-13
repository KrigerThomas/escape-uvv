using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Uma pergunta do Quadro do Abrantes. Edite as perguntas direto no Inspector,
/// no objeto "Quadro_Abrantes" (lista "Perguntas").
/// </summary>
[System.Serializable]
public class Pergunta
{
    [TextArea(2, 5)] public string enunciado = "Escreva a pergunta aqui";

    [Tooltip("As três alternativas. Elas aparecem embaralhadas na tela.")]
    public string[] alternativas = new string[3] { "Alternativa A", "Alternativa B", "Alternativa C" };

    [Tooltip("Qual alternativa está certa: 0 = primeira, 1 = segunda, 2 = terceira.")]
    [Range(0, 2)] public int indiceCorreta = 0;

    [Tooltip("Frase curta mostrada quando o jogador acerta (opcional).")]
    [TextArea(1, 3)] public string explicacao = "";
}

/// <summary>
/// Quadro do Abrantes (Nível 3). Quando o jogador chega perto e aperta E, abre um painel
/// com uma pergunta sorteada e três botões. Cada acerto conta 1 no GameManager
/// (rotulo "Acertos"); ao chegar no total, o GameManager abre a porta corta-fogo.
/// Errar não tira ponto: o quadro só troca de pergunta.
/// </summary>
[RequireComponent(typeof(Collider))]
public class QuadroPerguntas : MonoBehaviour
{
    [Header("Perguntas")]
    [Tooltip("Banco de perguntas. O grupo pode adicionar quantas quiser.")]
    public List<Pergunta> perguntas = new List<Pergunta>();

    [Tooltip("Tecla para ler o quadro quando o jogador está perto.")]
    public KeyCode teclaInteragir = KeyCode.E;

    [Header("Interface (filhos do Canvas_HUD)")]
    public GameObject painelQuadro;
    public TMP_Text textoPergunta;
    public TMP_Text textoProgresso;
    public Button[] botoesAlternativas = new Button[3];
    public TMP_Text textoResultado;
    public Button botaoFechar;

    [Tooltip("Caixa inteira da dica (fundo + texto) que aparece quando o jogador está perto do quadro.")]
    public GameObject avisoInteracao;

    [Tooltip("Texto \"Aperte E...\" dentro da caixa da dica.")]
    public TMP_Text textoInteracao;

    [Header("Cores dos botões")]
    public Color corNormal = new Color(0.96f, 0.96f, 0.94f);
    public Color corCerta = new Color(0.45f, 0.85f, 0.45f);
    public Color corErrada = new Color(0.95f, 0.45f, 0.4f);

    [Header("Sons")]
    public AudioClip somAcerto;
    public AudioClip somErro;
    [Range(0f, 1f)] public float volume = 0.8f;

    [Tooltip("Tempo (em segundos) que o resultado fica na tela antes da próxima pergunta.")]
    public float tempoResultado = 2.5f;

    private bool jogadorPerto;
    private bool painelAberto;
    private bool esperandoResultado;
    private readonly List<int> fila = new List<int>();
    private int perguntaAtual = -1;
    private int[] ordemAlternativas = new int[3];

    /// <summary>Esconde o painel, liga os botões e embaralha a fila de perguntas.</summary>
    void Start()
    {
        GetComponent<Collider>().isTrigger = true;

        if (painelQuadro != null)
            painelQuadro.SetActive(false);
        if (avisoInteracao != null)
            avisoInteracao.SetActive(false);

        for (int i = 0; i < botoesAlternativas.Length; i++)
        {
            int indice = i;
            if (botoesAlternativas[i] != null)
                botoesAlternativas[i].onClick.AddListener(() => Responder(indice));
        }

        if (botaoFechar != null)
            botaoFechar.onClick.AddListener(Fechar);

        EmbaralharFila();
    }

    /// <summary>Abre o quadro com E e fecha com Esc.</summary>
    void Update()
    {
        if (painelAberto)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !esperandoResultado)
                Fechar();
            return;
        }

        if (jogadorPerto && Input.GetKeyDown(teclaInteragir) && !DesafioConcluido())
            Abrir();
    }

    /// <summary>Mostra a dica "Aperte E" quando o jogador entra na área do quadro.</summary>
    void OnTriggerEnter(Collider outro)
    {
        if (!outro.CompareTag("Player"))
            return;

        jogadorPerto = true;
        AtualizarDica();
    }

    /// <summary>Esconde a dica quando o jogador se afasta.</summary>
    void OnTriggerExit(Collider outro)
    {
        if (!outro.CompareTag("Player"))
            return;

        jogadorPerto = false;
        AtualizarDica();
    }

    /// <summary>Verdadeiro quando o jogador já acertou tudo (a porta abriu).</summary>
    private bool DesafioConcluido()
    {
        return GameManager.Instancia != null && GameManager.Instancia.PortaDestrancada;
    }

    /// <summary>Liga ou desliga o texto "Aperte E para ler o quadro".</summary>
    private void AtualizarDica()
    {
        if (avisoInteracao == null)
            return;

        bool mostrar = jogadorPerto && !painelAberto && !DesafioConcluido();
        avisoInteracao.SetActive(mostrar);
        if (mostrar && textoInteracao != null)
            textoInteracao.text = "Aperte <b>" + teclaInteragir + "</b> para ler o Quadro do Abrantes";
    }

    /// <summary>Coloca todas as perguntas numa fila em ordem aleatória.</summary>
    private void EmbaralharFila()
    {
        fila.Clear();
        for (int i = 0; i < perguntas.Count; i++)
            fila.Add(i);

        for (int i = fila.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (fila[i], fila[j]) = (fila[j], fila[i]);
        }
    }

    /// <summary>Abre o painel (pausa o jogo e solta o mouse) e mostra uma pergunta.</summary>
    public void Abrir()
    {
        if (perguntas.Count == 0 || painelQuadro == null)
            return;

        painelAberto = true;
        AtualizarDica();
        UIManager.AbrirPainelComMouse(painelQuadro);
        MostrarProximaPergunta();
    }

    /// <summary>Fecha o painel e devolve o controle ao jogador.</summary>
    public void Fechar()
    {
        painelAberto = false;
        esperandoResultado = false;
        UIManager.FecharPainelComMouse(painelQuadro);
        AtualizarDica();
    }

    /// <summary>Tira a próxima pergunta da fila e escreve no painel com as alternativas embaralhadas.</summary>
    private void MostrarProximaPergunta()
    {
        if (fila.Count == 0)
            EmbaralharFila();

        perguntaAtual = fila[0];
        fila.RemoveAt(0);
        Pergunta p = perguntas[perguntaAtual];

        textoPergunta.text = p.enunciado;
        if (textoResultado != null)
            textoResultado.text = "";

        ordemAlternativas = new[] { 0, 1, 2 };
        for (int i = 2; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (ordemAlternativas[i], ordemAlternativas[j]) = (ordemAlternativas[j], ordemAlternativas[i]);
        }

        string letras = "ABC";
        for (int i = 0; i < botoesAlternativas.Length; i++)
        {
            Button b = botoesAlternativas[i];
            b.interactable = true;
            b.image.color = corNormal;
            TMP_Text t = b.GetComponentInChildren<TMP_Text>();
            t.text = letras[i] + ")  " + p.alternativas[ordemAlternativas[i]];
        }

        AtualizarProgresso();
    }

    /// <summary>Escreve "Acertos: X de Y" no canto do painel.</summary>
    private void AtualizarProgresso()
    {
        if (textoProgresso == null || GameManager.Instancia == null)
            return;

        GameManager gm = GameManager.Instancia;
        textoProgresso.text = "Acertos: " + gm.ItensColetados + " de " + gm.totalItens;
    }

    /// <summary>Chamado pelos botões. Confere a resposta, toca o som e pinta o botão.</summary>
    /// <param name="botao">Qual botão foi clicado (0, 1 ou 2).</param>
    public void Responder(int botao)
    {
        if (esperandoResultado || perguntaAtual < 0)
            return;

        Pergunta p = perguntas[perguntaAtual];
        bool certa = ordemAlternativas[botao] == p.indiceCorreta;

        foreach (Button b in botoesAlternativas)
            b.interactable = false;
        botoesAlternativas[botao].image.color = certa ? corCerta : corErrada;

        AudioClip som = certa ? somAcerto : somErro;
        UIManager.TocarSom2D(som, volume);

        if (certa)
        {
            textoResultado.text = "<color=#1f7a1f>Certa resposta!</color> " + p.explicacao;
            if (GameManager.Instancia != null)
                GameManager.Instancia.ColetarItem("Resposta certa!");
        }
        else
        {
            textoResultado.text = "<color=#b3261e>Errou!</color> O Abrantes apaga o quadro e escreve outra pergunta...";
            fila.Add(perguntaAtual); // a pergunta errada volta para o fim da fila
        }

        AtualizarProgresso();
        StartCoroutine(DepoisDoResultado());
    }

    /// <summary>Espera um pouco (em tempo real, porque o jogo está pausado) e segue o desafio.</summary>
    private IEnumerator DepoisDoResultado()
    {
        esperandoResultado = true;
        // textos longos (com explicação) ficam mais tempo na tela
        string semTags = System.Text.RegularExpressions.Regex.Replace(textoResultado != null ? textoResultado.text : "", "<.*?>", "");
        yield return new WaitForSecondsRealtime(Mathf.Max(tempoResultado, 1f + semTags.Length * 0.06f));
        esperandoResultado = false;

        if (DesafioConcluido())
            Fechar();
        else
            MostrarProximaPergunta();
    }
}
