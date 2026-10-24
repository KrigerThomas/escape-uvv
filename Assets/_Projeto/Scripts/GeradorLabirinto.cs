using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monta as paredes de sebe do labirinto a partir de um "desenho" em texto.
///
/// COMO FUNCIONA O MAPA (campo "Mapa" no Inspector):
///  - Cada caractere é um pedaço do chão visto de cima. A primeira linha é o lado norte.
///  - '#' é sebe (parede). Qualquer outro caractere ('.') é caminho livre.
///  - Linhas e colunas PARES (0, 2, 4...) são finas: viram sebes com a "Espessura da Sebe".
///  - Linhas e colunas ÍMPARES (1, 3, 5...) são largas: viram corredores com a "Largura do Corredor".
///  - Todas as linhas precisam ter o mesmo número de caracteres.
///
/// COMO USAR:
///  1. Edite o mapa no Inspector (troque '#' por '.' para abrir passagem, e o contrário para fechar).
///  2. Clique nos três pontinhos do componente (ou com o botão direito no título dele)
///     e escolha "Construir sebes a partir do mapa". As sebes antigas são apagadas e refeitas.
///  3. Opcional: "Gerar mapa aleatório (busca em profundidade)" cria um labirinto novo
///     usando a semente. Depois é só construir as sebes de novo.
///
/// Atenção: reconstruir apaga só o objeto filho "Sebes". Postes, bancos e segredos
/// ficam onde estão, então confira se nenhum ficou dentro de uma parede nova.
/// </summary>
public class GeradorLabirinto : MonoBehaviour
{
    [Header("Mapa ('#' = sebe, '.' = caminho, primeira linha = norte)")]
    [TextArea(19, 30)]
    public string mapa =
        "#############.#####\n" +
        "#.............#...#\n" +
        "###.#######.#.###.#\n" +
        "#.......#...#.#...#\n" +
        "#.#####.#.###.#.###\n" +
        "#...#.....#...#...#\n" +
        "###.#.#######.#.#.#\n" +
        "#...#.#.....#.#.#.#\n" +
        "#.###.#.....#.###.#\n" +
        "#.#...#.....#...#.#\n" +
        "#.#.###.....###.#.#\n" +
        "#.#.#.........#...#\n" +
        "#.#.#.#######.#####\n" +
        "#.#.#.......#.....#\n" +
        "#.###.#.#####.###.#\n" +
        "#.......#...#...#.#\n" +
        "#.#.###.#.#.#####.#\n" +
        "#.#.....#.#.......#\n" +
        "#########.#########";

    [Header("Medidas (em metros)")]
    [Tooltip("Largura dos corredores (linhas e colunas ímpares do mapa).")]
    public float larguraCorredor = 3.5f;

    [Tooltip("Espessura das sebes (linhas e colunas pares do mapa).")]
    public float espessuraSebe = 1f;

    [Tooltip("Altura visível das sebes. Mais alta que o pulo do jogador.")]
    public float alturaSebe = 2.8f;

    [Tooltip("Altura do collider. Fica maior que a sebe para ninguém conseguir subir em cima.")]
    public float alturaColisor = 6f;

    [Tooltip("Sebes mais compridas que isso são divididas em pedaços (ajuda a luz e o desempenho).")]
    public float comprimentoMaximoBloco = 9f;

    [Tooltip("Quantos metros a textura das folhas ocupa antes de repetir.")]
    public float tamanhoTexturaMetros = 1.6f;

    [Header("Visual")]
    [Tooltip("Material das folhas da sebe.")]
    public Material materialSebe;

    [Header("Gerador aleatório")]
    [Tooltip("Número que define o sorteio. A mesma semente sempre gera o mesmo labirinto.")]
    public int semente = 3;

    [Tooltip("Quantas células (corredores) o labirinto aleatório tem de cada lado.")]
    public int tamanhoEmCelulas = 9;

    private const char SEBE = '#';
    private const string NOME_PASTA_SEBES = "Sebes";

    /// <summary>Retângulo do mapa que vira um bloco de sebe (linhas e colunas inclusivas).</summary>
    private struct Bloco
    {
        public int linhaInicio, linhaFim, colunaInicio, colunaFim;
    }

    // ------------------------------------------------------------------
    //  Leitura do mapa e conversão para posições no mundo
    // ------------------------------------------------------------------

    /// <summary>Separa o texto do mapa em linhas, ignorando linhas vazias e espaços nas pontas.</summary>
    /// <returns>Vetor com as linhas do mapa, da linha norte (0) até a sul.</returns>
    public string[] LinhasDoMapa()
    {
        List<string> linhas = new List<string>();
        foreach (string linha in mapa.Replace("\r", "").Split('\n'))
        {
            string limpa = linha.Trim();
            if (limpa.Length > 0)
                linhas.Add(limpa);
        }
        return linhas.ToArray();
    }

    /// <summary>Largura, em metros, de uma linha ou coluna do mapa.</summary>
    /// <param name="indice">Número da linha ou coluna.</param>
    /// <returns>A espessura da sebe se o índice for par, ou a largura do corredor se for ímpar.</returns>
    public float TamanhoDaFaixa(int indice)
    {
        return indice % 2 == 0 ? espessuraSebe : larguraCorredor;
    }

    /// <summary>Soma o tamanho das primeiras faixas do mapa.</summary>
    /// <param name="quantidade">Quantas faixas somar, a partir da 0.</param>
    /// <returns>Distância em metros do começo do mapa até o início da faixa "quantidade".</returns>
    private float SomaDasFaixas(int quantidade)
    {
        float soma = 0f;
        for (int i = 0; i < quantidade; i++)
            soma += TamanhoDaFaixa(i);
        return soma;
    }

    /// <summary>
    /// Converte uma linha e uma coluna do mapa em uma posição local (relativa a este objeto).
    /// O centro do mapa fica na posição deste objeto; o norte (linha 0) fica no +Z.
    /// </summary>
    /// <param name="linha">Linha do mapa (0 = norte).</param>
    /// <param name="coluna">Coluna do mapa (0 = oeste).</param>
    /// <returns>Centro daquele pedaço do mapa, na altura do chão (Y = 0).</returns>
    public Vector3 PosicaoLocal(int linha, int coluna)
    {
        string[] linhas = LinhasDoMapa();
        float larguraTotal = SomaDasFaixas(linhas[0].Length);
        float profundidadeTotal = SomaDasFaixas(linhas.Length);

        float x = -larguraTotal / 2f + SomaDasFaixas(coluna) + TamanhoDaFaixa(coluna) / 2f;
        float z = profundidadeTotal / 2f - SomaDasFaixas(linha) - TamanhoDaFaixa(linha) / 2f;
        return new Vector3(x, 0f, z);
    }

    /// <summary>Mesma coisa que PosicaoLocal, mas já em coordenadas do mundo.</summary>
    /// <param name="linha">Linha do mapa (0 = norte).</param>
    /// <param name="coluna">Coluna do mapa (0 = oeste).</param>
    /// <returns>Posição no mundo do centro daquele pedaço do mapa.</returns>
    public Vector3 PosicaoNoMundo(int linha, int coluna)
    {
        return transform.TransformPoint(PosicaoLocal(linha, coluna));
    }

    // ------------------------------------------------------------------
    //  Construção das sebes
    // ------------------------------------------------------------------

    /// <summary>
    /// Apaga as sebes antigas e cria as novas: junta os '#' vizinhos em retângulos,
    /// divide os muito compridos e cria um bloco (malha + collider) para cada um.
    /// </summary>
    [ContextMenu("Construir sebes a partir do mapa")]
    public void ConstruirSebes()
    {
        string[] linhas = LinhasDoMapa();
        if (!MapaValido(linhas))
            return;

        ApagarSebesAntigas();

        GameObject pasta = new GameObject(NOME_PASTA_SEBES);
        pasta.transform.SetParent(transform, false);

        List<Bloco> blocos = JuntarEmRetangulos(linhas);
        int criados = 0;
        foreach (Bloco bloco in blocos)
            foreach (Bloco pedaco in DividirBlocoComprido(bloco))
            {
                CriarBlocoDeSebe(pedaco, pasta.transform, criados);
                criados++;
            }

#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(pasta, "Construir sebes");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        Debug.Log("GeradorLabirinto: " + criados + " blocos de sebe criados.");
    }

    /// <summary>Confere se o mapa tem conteúdo e se todas as linhas têm o mesmo tamanho.</summary>
    /// <param name="linhas">Linhas do mapa.</param>
    /// <returns>Verdadeiro se o mapa pode ser construído.</returns>
    private bool MapaValido(string[] linhas)
    {
        if (linhas.Length == 0)
        {
            Debug.LogWarning("GeradorLabirinto: o mapa está vazio.");
            return false;
        }

        for (int i = 1; i < linhas.Length; i++)
            if (linhas[i].Length != linhas[0].Length)
            {
                Debug.LogWarning("GeradorLabirinto: a linha " + i + " tem " + linhas[i].Length +
                                 " caracteres, mas a primeira tem " + linhas[0].Length + ".");
                return false;
            }

        return true;
    }

    /// <summary>Remove o objeto filho "Sebes" criado na última construção (se existir).</summary>
    private void ApagarSebesAntigas()
    {
        Transform antiga = transform.Find(NOME_PASTA_SEBES);
        if (antiga == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.DestroyObjectImmediate(antiga.gameObject);
            return;
        }
#endif
        Destroy(antiga.gameObject);
    }

    /// <summary>
    /// Percorre o mapa e junta os '#' vizinhos em retângulos: primeiro estica para a direita,
    /// depois para baixo enquanto a faixa inteira continuar sendo sebe.
    /// </summary>
    /// <param name="linhas">Linhas do mapa.</param>
    /// <returns>Lista de retângulos que cobrem todas as sebes.</returns>
    private List<Bloco> JuntarEmRetangulos(string[] linhas)
    {
        int altura = linhas.Length;
        int largura = linhas[0].Length;
        bool[,] usado = new bool[altura, largura];
        List<Bloco> blocos = new List<Bloco>();

        for (int l = 0; l < altura; l++)
            for (int c = 0; c < largura; c++)
            {
                if (linhas[l][c] != SEBE || usado[l, c])
                    continue;

                int colunaFim = c;
                while (colunaFim + 1 < largura && linhas[l][colunaFim + 1] == SEBE && !usado[l, colunaFim + 1])
                    colunaFim++;

                int linhaFim = l;
                while (linhaFim + 1 < altura && FaixaInteiraLivre(linhas, usado, linhaFim + 1, c, colunaFim))
                    linhaFim++;

                for (int i = l; i <= linhaFim; i++)
                    for (int j = c; j <= colunaFim; j++)
                        usado[i, j] = true;

                blocos.Add(new Bloco { linhaInicio = l, linhaFim = linhaFim, colunaInicio = c, colunaFim = colunaFim });
            }

        return blocos;
    }

    /// <summary>Verifica se, numa linha, todas as colunas de um intervalo são sebe ainda não usada.</summary>
    /// <param name="linhas">Linhas do mapa.</param>
    /// <param name="usado">Marcação dos '#' que já pertencem a um bloco.</param>
    /// <param name="linha">Linha a verificar.</param>
    /// <param name="colunaInicio">Primeira coluna do intervalo.</param>
    /// <param name="colunaFim">Última coluna do intervalo.</param>
    /// <returns>Verdadeiro se o retângulo pode crescer para essa linha.</returns>
    private bool FaixaInteiraLivre(string[] linhas, bool[,] usado, int linha, int colunaInicio, int colunaFim)
    {
        for (int c = colunaInicio; c <= colunaFim; c++)
            if (linhas[linha][c] != SEBE || usado[linha, c])
                return false;
        return true;
    }

    /// <summary>Divide um bloco em pedaços menores quando ele passa do comprimento máximo.</summary>
    /// <param name="bloco">Retângulo original.</param>
    /// <returns>O próprio bloco, ou os pedaços dele.</returns>
    private List<Bloco> DividirBlocoComprido(Bloco bloco)
    {
        List<Bloco> pedacos = new List<Bloco>();
        float largura = SomaDasFaixas(bloco.colunaFim + 1) - SomaDasFaixas(bloco.colunaInicio);
        float profundidade = SomaDasFaixas(bloco.linhaFim + 1) - SomaDasFaixas(bloco.linhaInicio);
        bool horizontal = largura >= profundidade;
        float comprimento = horizontal ? largura : profundidade;

        if (comprimento <= comprimentoMaximoBloco)
        {
            pedacos.Add(bloco);
            return pedacos;
        }

        int inicio = horizontal ? bloco.colunaInicio : bloco.linhaInicio;
        int fim = horizontal ? bloco.colunaFim : bloco.linhaFim;
        int corteInicio = inicio;
        float acumulado = 0f;

        for (int i = inicio; i <= fim; i++)
        {
            acumulado += TamanhoDaFaixa(i);
            bool ultimo = i == fim;
            bool cortaAqui = acumulado >= comprimentoMaximoBloco * 0.75f && i % 2 == 0;

            if (!cortaAqui && !ultimo)
                continue;

            Bloco pedaco = bloco;
            if (horizontal) { pedaco.colunaInicio = corteInicio; pedaco.colunaFim = i; }
            else { pedaco.linhaInicio = corteInicio; pedaco.linhaFim = i; }
            pedacos.Add(pedaco);

            corteInicio = i + 1;
            acumulado = 0f;
        }

        return pedacos;
    }

    /// <summary>Cria um GameObject com a malha da sebe, o material e um BoxCollider alto.</summary>
    /// <param name="bloco">Retângulo do mapa que este bloco ocupa.</param>
    /// <param name="pai">Objeto "Sebes" que agrupa todos os blocos.</param>
    /// <param name="numero">Número do bloco, usado no nome.</param>
    private void CriarBlocoDeSebe(Bloco bloco, Transform pai, int numero)
    {
        Vector3 cantoNorteOeste = PosicaoLocal(bloco.linhaInicio, bloco.colunaInicio)
                                  + new Vector3(-TamanhoDaFaixa(bloco.colunaInicio) / 2f, 0f, TamanhoDaFaixa(bloco.linhaInicio) / 2f);
        Vector3 cantoSulLeste = PosicaoLocal(bloco.linhaFim, bloco.colunaFim)
                                + new Vector3(TamanhoDaFaixa(bloco.colunaFim) / 2f, 0f, -TamanhoDaFaixa(bloco.linhaFim) / 2f);

        Vector3 centro = (cantoNorteOeste + cantoSulLeste) / 2f;
        Vector3 tamanho = new Vector3(cantoSulLeste.x - cantoNorteOeste.x, alturaSebe, cantoNorteOeste.z - cantoSulLeste.z);

        GameObject sebe = new GameObject("Sebe_" + numero.ToString("000"));
        sebe.transform.SetParent(pai, false);
        sebe.transform.localPosition = new Vector3(centro.x, 0f, centro.z);

        sebe.AddComponent<MeshFilter>().sharedMesh = CriarMalhaCaixa(tamanho, centro, sebe.name);
        MeshRenderer renderizador = sebe.AddComponent<MeshRenderer>();
        renderizador.sharedMaterial = materialSebe;

        BoxCollider colisor = sebe.AddComponent<BoxCollider>();
        colisor.size = new Vector3(tamanho.x, alturaColisor, tamanho.z);
        colisor.center = new Vector3(0f, alturaColisor / 2f, 0f);

#if UNITY_EDITOR
        UnityEditor.GameObjectUtility.SetStaticEditorFlags(sebe,
            UnityEditor.StaticEditorFlags.BatchingStatic | UnityEditor.StaticEditorFlags.OccludeeStatic | UnityEditor.StaticEditorFlags.OccluderStatic);
#endif
    }

    /// <summary>
    /// Monta uma caixa (sem a face de baixo) com coordenadas de textura em metros.
    /// Como as coordenadas usam a posição no labirinto, a textura continua sem emenda
    /// de um bloco para o outro.
    /// </summary>
    /// <param name="tamanho">Largura (X), altura (Y) e profundidade (Z) da caixa.</param>
    /// <param name="centroNoLabirinto">Posição do centro da caixa dentro do labirinto, usada nas coordenadas de textura.</param>
    /// <param name="nome">Nome da malha.</param>
    /// <returns>A malha pronta, com normais e tangentes calculadas.</returns>
    private Mesh CriarMalhaCaixa(Vector3 tamanho, Vector3 centroNoLabirinto, string nome)
    {
        float x = tamanho.x / 2f, z = tamanho.z / 2f, y = tamanho.y;
        float t = Mathf.Max(0.1f, tamanhoTexturaMetros);
        float ox = centroNoLabirinto.x, oz = centroNoLabirinto.z;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangulos = new List<int>();

        // Cada face: 4 cantos em ordem horária vista de fora + as coordenadas de textura.
        AdicionarFace(vertices, uvs, triangulos,
            new Vector3(-x, 0, -z), new Vector3(-x, y, -z), new Vector3(x, y, -z), new Vector3(x, 0, -z),
            p => new Vector2((p.x + ox) / t, p.y / t));                       // sul (-Z)
        AdicionarFace(vertices, uvs, triangulos,
            new Vector3(x, 0, z), new Vector3(x, y, z), new Vector3(-x, y, z), new Vector3(-x, 0, z),
            p => new Vector2(-(p.x + ox) / t, p.y / t));                      // norte (+Z)
        AdicionarFace(vertices, uvs, triangulos,
            new Vector3(-x, 0, z), new Vector3(-x, y, z), new Vector3(-x, y, -z), new Vector3(-x, 0, -z),
            p => new Vector2(-(p.z + oz) / t, p.y / t));                      // oeste (-X)
        AdicionarFace(vertices, uvs, triangulos,
            new Vector3(x, 0, -z), new Vector3(x, y, -z), new Vector3(x, y, z), new Vector3(x, 0, z),
            p => new Vector2((p.z + oz) / t, p.y / t));                       // leste (+X)
        AdicionarFace(vertices, uvs, triangulos,
            new Vector3(-x, y, -z), new Vector3(-x, y, z), new Vector3(x, y, z), new Vector3(x, y, -z),
            p => new Vector2((p.x + ox) / t, (p.z + oz) / t));                // topo (+Y)

        Mesh malha = new Mesh { name = nome };
        malha.SetVertices(vertices);
        malha.SetUVs(0, uvs);
        malha.SetTriangles(triangulos, 0);
        malha.RecalculateNormals();
        malha.RecalculateTangents();
        malha.RecalculateBounds();
        return malha;
    }

    /// <summary>Adiciona os 4 vértices e os 2 triângulos de uma face retangular.</summary>
    /// <param name="vertices">Lista de vértices da malha.</param>
    /// <param name="uvs">Lista de coordenadas de textura.</param>
    /// <param name="triangulos">Lista de índices dos triângulos.</param>
    /// <param name="a">Canto 1 (embaixo à esquerda, olhando de fora).</param>
    /// <param name="b">Canto 2 (em cima à esquerda).</param>
    /// <param name="c">Canto 3 (em cima à direita).</param>
    /// <param name="d">Canto 4 (embaixo à direita).</param>
    /// <param name="paraUV">Função que transforma um canto na coordenada de textura dele.</param>
    private void AdicionarFace(List<Vector3> vertices, List<Vector2> uvs, List<int> triangulos,
                               Vector3 a, Vector3 b, Vector3 c, Vector3 d, System.Func<Vector3, Vector2> paraUV)
    {
        int i = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
        uvs.Add(paraUV(a)); uvs.Add(paraUV(b)); uvs.Add(paraUV(c)); uvs.Add(paraUV(d));
        triangulos.Add(i); triangulos.Add(i + 1); triangulos.Add(i + 2);
        triangulos.Add(i); triangulos.Add(i + 2); triangulos.Add(i + 3);
    }

    // ------------------------------------------------------------------
    //  Gerador aleatório (busca em profundidade com pilha)
    // ------------------------------------------------------------------

    /// <summary>
    /// Escreve no campo "Mapa" um labirinto novo, sorteado com a semente.
    /// Algoritmo: busca em profundidade (DFS) com uma pilha. Começa na célula da entrada,
    /// sorteia um vizinho ainda não visitado, derruba a sebe entre os dois e continua dali.
    /// Quando não há vizinho livre, volta (desempilha). O resultado tem exatamente um
    /// caminho entre quaisquer duas células.
    /// </summary>
    [ContextMenu("Gerar mapa aleatório (busca em profundidade)")]
    public void GerarMapaAleatorio()
    {
        int n = Mathf.Max(3, tamanhoEmCelulas);
        int lado = 2 * n + 1;
        char[,] grade = new char[lado, lado];
        for (int l = 0; l < lado; l++)
            for (int c = 0; c < lado; c++)
                grade[l, c] = SEBE;

        System.Random sorteio = new System.Random(semente);
        bool[,] visitada = new bool[n, n];
        Stack<Vector2Int> pilha = new Stack<Vector2Int>();
        Vector2Int[] direcoes = { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1) };

        Vector2Int entrada = new Vector2Int(n - 1, n / 2); // (linha, coluna) da célula no lado sul
        visitada[entrada.x, entrada.y] = true;
        grade[2 * entrada.x + 1, 2 * entrada.y + 1] = '.';
        pilha.Push(entrada);

        List<Vector2Int> livres = new List<Vector2Int>();
        while (pilha.Count > 0)
        {
            Vector2Int atual = pilha.Peek();
            livres.Clear();
            foreach (Vector2Int d in direcoes)
            {
                Vector2Int vizinha = atual + d;
                if (vizinha.x >= 0 && vizinha.x < n && vizinha.y >= 0 && vizinha.y < n && !visitada[vizinha.x, vizinha.y])
                    livres.Add(vizinha);
            }

            if (livres.Count == 0)
            {
                pilha.Pop();
                continue;
            }

            Vector2Int escolhida = livres[sorteio.Next(livres.Count)];
            grade[atual.x + escolhida.x + 1, atual.y + escolhida.y + 1] = '.'; // sebe entre as duas células
            grade[2 * escolhida.x + 1, 2 * escolhida.y + 1] = '.';
            visitada[escolhida.x, escolhida.y] = true;
            pilha.Push(escolhida);
        }

        grade[lado - 1, 2 * entrada.y + 1] = '.';              // entrada no sul
        grade[0, 2 * sorteio.Next(n) + 1] = '.';               // saída sorteada no norte

        System.Text.StringBuilder texto = new System.Text.StringBuilder();
        for (int l = 0; l < lado; l++)
        {
            for (int c = 0; c < lado; c++)
                texto.Append(grade[l, c]);
            if (l < lado - 1)
                texto.Append('\n');
        }
        mapa = texto.ToString();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("GeradorLabirinto: mapa novo gerado com a semente " + semente + ". Agora use \"Construir sebes a partir do mapa\".");
    }

    /// <summary>Desenha o contorno do labirinto na Scene View quando o objeto está selecionado.</summary>
    void OnDrawGizmosSelected()
    {
        string[] linhas = LinhasDoMapa();
        if (linhas.Length == 0)
            return;

        Vector3 tamanho = new Vector3(SomaDasFaixas(linhas[0].Length), alturaSebe, SomaDasFaixas(linhas.Length));
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(new Vector3(0f, alturaSebe / 2f, 0f), tamanho);
    }
}
