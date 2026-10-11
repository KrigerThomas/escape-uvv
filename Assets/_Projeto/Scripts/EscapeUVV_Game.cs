using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#region PORTA DE ENTRADA
/// <summary>
/// Controla a porta deslizante de entrada do InovaLab.
/// Quando o jogador se aproxima do gatilho (trigger), a porta desliza para o lado abrindo a passagem.
/// Quando se afasta, a porta fecha automaticamente.
/// </summary>
public class PortaEntrada : MonoBehaviour
{
    public float distanciaAbertura = 2.4f;
    public float velocidade = 3.5f;

    private Vector3 posicaoFechada;
    private Vector3 posicaoAberta;
    private bool jogadorPerto = false;

    void Start()
    {
        posicaoFechada = transform.position;
        posicaoAberta = posicaoFechada + transform.forward * distanciaAbertura;
    }

    void Update()
    {
        Vector3 destino = jogadorPerto ? posicaoAberta : posicaoFechada;
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidade * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorPerto = true;
            if (GameManager.Instancia != null)
                GameManager.Instancia.IniciarCronometro();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            jogadorPerto = false;
    }
}
#endregion

#region GERENCIADOR DO JOGO (GAME MANAGER)
/// <summary>
/// Gerencia o ciclo completo do Nível 1: cronômetro, código da porta, pistas e vitória/derrota.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instancia { get; private set; }

    [Header("Cronômetro")]
    public float tempoRestante = 300f; // 5 minutos
    public bool cronometroAtivo = false;

    [Header("Código Secreto da Porta")]
    public int[] codigoPorta = new int[4];
    public bool[] digitoDescoberto = new bool[4];

    [Header("Progresso")]
    public int evidenciasColetadas = 0;
    public bool temLanterna = false;
    public bool jogoVencido = false;
    public bool jogoPerdido = false;

    [Header("Interface")]
    public string mensagemInteracao = "";
    public string mensagemAlerta = "";
    private float tempoAlerta = 0f;

    void Awake()
    {
        if (Instancia == null)
            Instancia = this;
        else
            Destroy(gameObject);

        GerarCodigo();
    }

    void Update()
    {
        if (cronometroAtivo && !jogoVencido && !jogoPerdido)
        {
            tempoRestante -= Time.deltaTime;
            if (tempoRestante <= 0f)
            {
                tempoRestante = 0f;
                Derrota("O tempo esgotou! Os gatos correm perigo!");
            }
        }

        if (tempoAlerta > 0f)
        {
            tempoAlerta -= Time.deltaTime;
            if (tempoAlerta <= 0f)
                mensagemAlerta = "";
        }
    }

    private void GerarCodigo()
    {
        for (int i = 0; i < 4; i++)
        {
            codigoPorta[i] = Random.Range(1, 10);
            digitoDescoberto[i] = false;
        }
    }

    public void IniciarCronometro()
    {
        cronometroAtivo = true;
    }

    public void MostrarAlerta(string texto, float duracao = 4f)
    {
        mensagemAlerta = texto;
        tempoAlerta = duracao;
    }

    public void ColetarEvidencia(string nomeEvidencia)
    {
        if (evidenciasColetadas < 3)
        {
            digitoDescoberto[evidenciasColetadas] = true;
            int digito = codigoPorta[evidenciasColetadas];
            evidenciasColetadas++;
            MostrarAlerta($"Evidência encontrada ({evidenciasColetadas}/3): {nomeEvidencia}!\nDígito {evidenciasColetadas} do código: [{digito}]");
        }
    }

    public void AcertarPerguntaQuadro()
    {
        digitoDescoberto[3] = true;
        MostrarAlerta($"Resposta correta no quadro!\nÚltimo dígito (4º) do código: [{codigoPorta[3]}]");
    }

    public bool ValidarCodigo(int d1, int d2, int d3, int d4)
    {
        if (d1 == codigoPorta[0] && d2 == codigoPorta[1] && d3 == codigoPorta[2] && d4 == codigoPorta[3])
        {
            Vitoria();
            return true;
        }
        MostrarAlerta("Código incorreto! Confira as pistas e a resposta do quadro.");
        return false;
    }

    public void Vitoria()
    {
        jogoVencido = true;
        cronometroAtivo = false;
    }

    public void Derrota(string motivo)
    {
        jogoPerdido = true;
        cronometroAtivo = false;
        mensagemAlerta = motivo;
    }

    public void ReiniciarFase()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
#endregion

#region LANTERNA UV
/// <summary>
/// Controla a Lanterna UV: coleta com E, liga/desliga com F, consome bateria.
/// </summary>
public class LanternaUV : MonoBehaviour
{
    public float bateriaMaxima = 25f;
    public float bateriaAtual = 25f;
    public float taxaConsumo = 1f;
    public float taxaRecarga = 0.5f;

    public Light luzUV;
    public bool ligada = false;

    private bool coletada = false;
    private Transform jogador;

    void Start()
    {
        if (luzUV == null) luzUV = GetComponentInChildren<Light>();
        if (luzUV != null)
        {
            luzUV.enabled = false;
            luzUV.color = new Color(0.65f, 0.2f, 1f);
        }

        GameObject jog = GameObject.FindGameObjectWithTag("Player");
        if (jog != null) jogador = jog.transform;
    }

    void Update()
    {
        if (!coletada)
        {
            ChecarColeta();
            return;
        }

        if (jogador != null)
        {
            transform.position = jogador.position + Vector3.up * 0.8f + jogador.forward * 0.25f;
            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ligada = !ligada;
            if (bateriaAtual <= 0f) ligada = false;
            if (luzUV != null) luzUV.enabled = ligada;
        }

        if (ligada)
        {
            bateriaAtual -= taxaConsumo * Time.deltaTime;
            if (bateriaAtual <= 0f)
            {
                bateriaAtual = 0f;
                ligada = false;
                if (luzUV != null) luzUV.enabled = false;
            }
        }
        else if (bateriaAtual < bateriaMaxima)
        {
            bateriaAtual += taxaRecarga * Time.deltaTime;
        }
    }

    private void ChecarColeta()
    {
        if (jogador == null) return;
        if (Vector3.Distance(transform.position, jogador.position) < 2.0f)
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.mensagemInteracao = "Pressione [E] para pegar a Lanterna UV";

            if (Input.GetKeyDown(KeyCode.E))
            {
                coletada = true;
                if (GameManager.Instancia != null)
                {
                    GameManager.Instancia.temLanterna = true;
                    GameManager.Instancia.mensagemInteracao = "";
                    GameManager.Instancia.MostrarAlerta("Lanterna UV coletada! Aperte [F] para ligar a luz roxa.");
                }
            }
        }
        else if (GameManager.Instancia != null && GameManager.Instancia.mensagemInteracao.Contains("Lanterna"))
        {
            GameManager.Instancia.mensagemInteracao = "";
        }
    }
}
#endregion

#region EVIDÊNCIAS
/// <summary>
/// Evidência do crime visível apenas sob a luz da lanterna UV.
/// </summary>
public class EvidenciaItem : MonoBehaviour
{
    public string nomeEvidencia = "Pedaço de Pano Laranja";
    public float velocidadeGiro = 45f;
    private Renderer rend;
    private bool coletada = false;
    private Transform jogador;
    private LanternaUV lanterna;

    void Start()
    {
        rend = GetComponent<Renderer>();
        GameObject jog = GameObject.FindGameObjectWithTag("Player");
        if (jog != null) jogador = jog.transform;
    }

    void Update()
    {
        if (coletada) return;
        transform.Rotate(Vector3.up, velocidadeGiro * Time.deltaTime, Space.World);

        if (lanterna == null) lanterna = Object.FindFirstObjectByType<LanternaUV>();

        bool sobLuzUV = lanterna != null && lanterna.ligada && Vector3.Distance(transform.position, lanterna.transform.position) < 8f;
        if (rend != null) rend.enabled = sobLuzUV;

        if (jogador != null && sobLuzUV && Vector3.Distance(transform.position, jogador.position) < 2.5f)
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.mensagemInteracao = $"Pressione [E] para coletar: {nomeEvidencia}";

            if (Input.GetKeyDown(KeyCode.E))
            {
                coletada = true;
                if (GameManager.Instancia != null)
                {
                    GameManager.Instancia.mensagemInteracao = "";
                    GameManager.Instancia.ColetarEvidencia(nomeEvidencia);
                }
                gameObject.SetActive(false);
            }
        }
        else if (GameManager.Instancia != null && GameManager.Instancia.mensagemInteracao.Contains(nomeEvidencia))
        {
            GameManager.Instancia.mensagemInteracao = "";
        }
    }
}
#endregion

#region QUADRO DE PERGUNTAS
/// <summary>
/// Quadro branco com pergunta de tecnologia para liberar o 4º dígito.
/// </summary>
public class InteracaoQuadro : MonoBehaviour
{
    private bool jogadorPerto = false;
    private bool mostrarJanela = false;
    private bool acertou = false;

    void Update()
    {
        GameObject jog = GameObject.FindGameObjectWithTag("Player");
        if (jog != null)
            jogadorPerto = Vector3.Distance(transform.position, jog.transform.position) < 3.2f;

        if (jogadorPerto && !acertou)
        {
            if (!mostrarJanela && GameManager.Instancia != null)
                GameManager.Instancia.mensagemInteracao = "Pressione [E] para ler o Quadro de Segurança";

            if (Input.GetKeyDown(KeyCode.E))
            {
                mostrarJanela = !mostrarJanela;
                Cursor.lockState = mostrarJanela ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = mostrarJanela;
            }
        }
        else if (mostrarJanela && !jogadorPerto)
        {
            mostrarJanela = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnGUI()
    {
        if (!mostrarJanela || acertou) return;

        int w = 450;
        int h = 260;
        int x = (Screen.width - w) / 2;
        int y = (Screen.height - h) / 2;

        GUI.Box(new Rect(x, y, w, h), "PERGUNTA DE SEGURANÇA - INOVALAB");
        GUI.Label(new Rect(x + 20, y + 40, w - 40, 60), 
            "Quantos bits formam exatamente 1 Byte no sistema de computação?\n(A resposta correta é o 4º dígito do código da porta!)");

        if (GUI.Button(new Rect(x + 30, y + 110, 110, 45), "4 Bits"))
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.MostrarAlerta("Byte diz: 'Esse palpite nem compila! Tente de novo.'");
        }

        if (GUI.Button(new Rect(x + 160, y + 110, 110, 45), "6 Bits"))
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.MostrarAlerta("Byte diz: 'Quase! Releia com atenção.'");
        }

        if (GUI.Button(new Rect(x + 290, y + 110, 110, 45), "8 Bits"))
        {
            acertou = true;
            mostrarJanela = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (GameManager.Instancia != null)
                GameManager.Instancia.AcertarPerguntaQuadro();
        }

        if (GUI.Button(new Rect(x + 160, y + 190, 110, 35), "Fechar [E]"))
        {
            mostrarJanela = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
#endregion

#region TECLADO DA PORTA DE SAÍDA
/// <summary>
/// Teclado numérico da porta de saída para digitar os 4 dígitos e vencer o jogo.
/// </summary>
public class TecladoPortaSaida : MonoBehaviour
{
    private bool jogadorPerto = false;
    private bool mostrarTeclado = false;
    private string entradaCodigo = "";
    public GameObject portaSaidaObjeto;

    void Update()
    {
        GameObject jog = GameObject.FindGameObjectWithTag("Player");
        if (jog != null)
            jogadorPerto = Vector3.Distance(transform.position, jog.transform.position) < 2.5f;

        if (jogadorPerto)
        {
            if (!mostrarTeclado && GameManager.Instancia != null && !GameManager.Instancia.jogoVencido)
                GameManager.Instancia.mensagemInteracao = "Pressione [E] para acessar o Teclado da Porta";

            if (Input.GetKeyDown(KeyCode.E))
            {
                mostrarTeclado = !mostrarTeclado;
                Cursor.lockState = mostrarTeclado ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = mostrarTeclado;
            }
        }
        else if (mostrarTeclado)
        {
            mostrarTeclado = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnGUI()
    {
        if (!mostrarTeclado) return;

        int w = 320;
        int h = 380;
        int x = (Screen.width - w) / 2;
        int y = (Screen.height - h) / 2;

        GUI.Box(new Rect(x, y, w, h), "FECHADURA ELETRÔNICA - SAÍDA");
        GUI.Box(new Rect(x + 40, y + 40, w - 80, 40), $"CÓDIGO: {entradaCodigo}");

        int btnW = 60;
        int btnH = 45;
        for (int i = 1; i <= 9; i++)
        {
            int col = (i - 1) % 3;
            int row = (i - 1) / 3;
            int bx = x + 50 + col * 75;
            int by = y + 100 + row * 55;

            if (GUI.Button(new Rect(bx, by, btnW, btnH), i.ToString()))
            {
                if (entradaCodigo.Length < 4) entradaCodigo += i.ToString();
            }
        }

        if (GUI.Button(new Rect(x + 50, y + 270, 60, btnH), "C")) entradaCodigo = "";
        if (GUI.Button(new Rect(x + 125, y + 270, 60, btnH), "0"))
        {
            if (entradaCodigo.Length < 4) entradaCodigo += "0";
        }

        if (GUI.Button(new Rect(x + 200, y + 270, 60, btnH), "OK"))
        {
            if (entradaCodigo.Length == 4)
            {
                int d1 = entradaCodigo[0] - '0';
                int d2 = entradaCodigo[1] - '0';
                int d3 = entradaCodigo[2] - '0';
                int d4 = entradaCodigo[3] - '0';

                if (GameManager.Instancia != null && GameManager.Instancia.ValidarCodigo(d1, d2, d3, d4))
                {
                    mostrarTeclado = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    if (portaSaidaObjeto != null) portaSaidaObjeto.SetActive(false);
                }
                else
                {
                    entradaCodigo = "";
                }
            }
        }

        if (GUI.Button(new Rect(x + 110, y + 330, 100, 30), "Fechar [E]"))
        {
            mostrarTeclado = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
#endregion

#region INTERFACE DE USUÁRIO (UI MANAGER)
/// <summary>
/// Exibe a HUD na tela: timer, evidências, código, bateria e tela de vitória com botão de reiniciar.
/// </summary>
public class UIManager : MonoBehaviour
{
    private LanternaUV lanterna;

    void Start()
    {
        lanterna = Object.FindFirstObjectByType<LanternaUV>();
    }

    void OnGUI()
    {
        GameManager gm = GameManager.Instancia;
        if (gm == null) return;

        // Cronômetro
        int minutos = Mathf.FloorToInt(gm.tempoRestante / 60f);
        int segundos = Mathf.FloorToInt(gm.tempoRestante % 60f);
        string corTempo = gm.tempoRestante < 30f ? "<color=red>" : "<color=white>";
        string textoTempo = $"{corTempo}TEMPO: {minutos:00}:{segundos:00}</color>";

        GUIStyle estiloTempo = new GUIStyle(GUI.skin.box);
        estiloTempo.fontSize = 18;
        estiloTempo.richText = true;
        GUI.Box(new Rect(Screen.width / 2 - 90, 15, 180, 40), textoTempo, estiloTempo);

        // Painel de Pistas
        string d1 = gm.digitoDescoberto[0] ? gm.codigoPorta[0].ToString() : "?";
        string d2 = gm.digitoDescoberto[1] ? gm.codigoPorta[1].ToString() : "?";
        string d3 = gm.digitoDescoberto[2] ? gm.codigoPorta[2].ToString() : "?";
        string d4 = gm.digitoDescoberto[3] ? gm.codigoPorta[3].ToString() : "?";

        string textoPistas = $"EVIDÊNCIAS: {gm.evidenciasColetadas}/3\nCÓDIGO DA PORTA: [ {d1} - {d2} - {d3} - {d4} ]";
        GUI.Box(new Rect(20, 20, 260, 60), textoPistas);

        // Lanterna
        if (lanterna == null) lanterna = Object.FindFirstObjectByType<LanternaUV>();
        if (gm.temLanterna && lanterna != null)
        {
            float pct = lanterna.bateriaAtual / lanterna.bateriaMaxima;
            string estadoLuz = lanterna.ligada ? "<color=magenta>[LIGADA - F]</color>" : "[DESLIGADA - F]";
            GUI.Box(new Rect(20, Screen.height - 70, 240, 50), $"LANTERNA UV: {estadoLuz}\nBateria: {(pct * 100):0}%");
        }

        // Mensagens
        if (!string.IsNullOrEmpty(gm.mensagemInteracao))
        {
            GUIStyle estiloInteracao = new GUIStyle(GUI.skin.box);
            estiloInteracao.fontSize = 16;
            GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height - 110, 400, 35), gm.mensagemInteracao, estiloInteracao);
        }

        if (!string.IsNullOrEmpty(gm.mensagemAlerta))
        {
            GUIStyle estiloAlerta = new GUIStyle(GUI.skin.box);
            estiloAlerta.fontSize = 15;
            estiloAlerta.normal.textColor = Color.yellow;
            GUI.Box(new Rect(Screen.width / 2 - 250, 70, 500, 50), gm.mensagemAlerta, estiloAlerta);
        }

        // Vitória
        if (gm.jogoVencido)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            int w = 450;
            int h = 220;
            int x = (Screen.width - w) / 2;
            int y = (Screen.height - h) / 2;

            GUI.Box(new Rect(x, y, w, h), "PARABÉNS! VOCÊ ESCAPOU!");
            GUI.Label(new Rect(x + 20, y + 40, w - 40, 70),
                "Você reuniu as evidências de rato e o pano laranja!\n" +
                "A inocência dos gatos do campus está provada!");

            if (GUI.Button(new Rect(x + 140, y + 130, 170, 50), "REINICIAR JOGO"))
            {
                gm.ReiniciarFase();
            }
        }

        // Derrota
        if (gm.jogoPerdido)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            int w = 400;
            int h = 200;
            int x = (Screen.width - w) / 2;
            int y = (Screen.height - h) / 2;

            GUI.Box(new Rect(x, y, w, h), "TEMPO ESGOTADO!");
            GUI.Label(new Rect(x + 20, y + 50, w - 40, 50), "O tempo acabou antes de você conseguir abrir a porta.");

            if (GUI.Button(new Rect(x + 120, y + 110, 160, 45), "REINICIAR"))
            {
                gm.ReiniciarFase();
            }
        }
    }
}
#endregion
