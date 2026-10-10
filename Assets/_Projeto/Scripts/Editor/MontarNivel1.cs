using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ferramenta de editor (menu "Escape UVV") que monta o rascunho visual do Nível 1:
/// pátio externo, InovaLab com paredes, bancadas, computadores, vitrine e armários,
/// pontos de evidência (P1 a P6), ventilações dos ratos (V1 a V4), jogador e câmera.
/// As medidas dos prefabs do pack são lidas automaticamente, então a montagem se adapta ao tamanho de cada peça.
/// </summary>
public static class MontarNivel1
{
    // ===================== AJUSTES FÁCEIS =====================
    // Mude os valores abaixo e rode o menu de novo para refazer o cenário.

    /// <summary>Largura do InovaLab (eixo X), em metros.</summary>
    private const float LabLargura = 22f;

    /// <summary>Profundidade do InovaLab (eixo Z), em metros.</summary>
    private const float LabProfundidade = 16f;

    /// <summary>Largura do pátio (eixo X), à esquerda do laboratório.</summary>
    private const float PatioLargura = 26f;

    /// <summary>Profundidade do pátio (eixo Z).</summary>
    private const float PatioProfundidade = 24f;

    // Prefabs do pack School assets. Para trocar uma peça, mude só o nome do arquivo.
    private const string Props = "Assets/school/Prefabs/props/";
    private const string Estrada = "Assets/school/Prefabs/road/";
    private const string PrefabPiso = Estrada + "floor.prefab";
    private const string PrefabParede = Props + "wall.prefab";
    private const string PrefabPorta = Props + "a door.prefab";
    private const string PrefabBancada = Props + "table3.prefab";
    private const string PrefabRecepcao = Props + "table1.prefab";
    private const string PrefabCadeira = Props + "chair.prefab";
    private const string PrefabComputador = Props + "computer.prefab";
    private const string PrefabNotebook = Props + "a laptop.prefab";
    private const string PrefabVitrine = Props + "showcase.prefab";
    private const string PrefabArmario = Props + "locker.prefab";
    private const string PrefabQuadro = Props + "board.prefab";

    private const string PastaMateriais = "Assets/_Projeto/Materiais";
    // ==========================================================

    private static int contadorParedes;
    private static float aberturaPorta;
    private static string relatorio;
    private static HashSet<string> relatados;

    /// <summary>
    /// Item de menu principal: apaga o cenário anterior (se existir) e monta o Nível 1 completo.
    /// </summary>
    [MenuItem("Escape UVV/Montar Nivel 1 (rascunho visual)")]
    public static void Montar()
    {
        bool confirmou = EditorUtility.DisplayDialog("Montar Nível 1",
            "Vou apagar os objetos 'Nivel1_Cenario' e 'Jogador' desta cena (se existirem) e montar tudo de novo. Continuar?",
            "Montar", "Cancelar");
        if (!confirmou)
            return;

        relatorio = "";
        relatados = new HashSet<string>();

        GarantirTags();
        LimparCenaAnterior();

        GameObject raiz = new GameObject("Nivel1_Cenario");
        Undo.RegisterCreatedObjectUndo(raiz, "Montar Nivel 1");

        Transform piso = CriarGrupo(raiz.transform, "Piso");
        Transform paredes = CriarGrupo(raiz.transform, "Paredes");
        Transform mobiliario = CriarGrupo(raiz.transform, "Mobiliario");
        Transform patio = CriarGrupo(raiz.transform, "Patio");
        Transform pontos = CriarGrupo(raiz.transform, "Pontos");

        ConstruirPiso(piso);
        ConstruirParedes(paredes);
        List<Bounds> bancadas = ConstruirMobiliario(mobiliario);
        ConstruirPatio(patio);
        ConstruirPontos(pontos, bancadas);
        CriarJogadorECamera(pontos);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Selection.activeGameObject = raiz;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log("[Montar Nivel 1] Pronto! Salve a cena (Ctrl+S) e aperte Play.\nMedidas dos prefabs usados (metros, X Y Z):\n" + relatorio);
    }

    // ------------------------------------------------------------------
    // Preparação
    // ------------------------------------------------------------------

    /// <summary>Cria as tags usadas no jogo (Pista, Rato, Porta, Chao e Bateria) se ainda não existirem.</summary>
    private static void GarantirTags()
    {
        string[] tags = { "Pista", "Rato", "Porta", "Chao", "Bateria" };

        Object[] todos = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (todos == null || todos.Length == 0)
        {
            Debug.LogWarning("[Montar Nivel 1] Nao consegui abrir o TagManager. Crie as tags manualmente.");
            return;
        }

        SerializedObject gerente = new SerializedObject(todos[0]);
        SerializedProperty lista = gerente.FindProperty("tags");

        for (int t = 0; t < tags.Length; t++)
        {
            bool existe = false;
            for (int i = 0; i < lista.arraySize; i++)
            {
                if (lista.GetArrayElementAtIndex(i).stringValue == tags[t])
                {
                    existe = true;
                    break;
                }
            }

            if (existe)
                continue;

            lista.InsertArrayElementAtIndex(lista.arraySize);
            lista.GetArrayElementAtIndex(lista.arraySize - 1).stringValue = tags[t];
        }

        gerente.ApplyModifiedProperties();
    }

    /// <summary>Apaga da cena o cenário e o jogador gerados numa montagem anterior.</summary>
    private static void LimparCenaAnterior()
    {
        string[] nomes = { "Nivel1_Cenario", "Jogador" };
        for (int i = 0; i < nomes.Length; i++)
        {
            GameObject antigo = GameObject.Find(nomes[i]);
            while (antigo != null)
            {
                Undo.DestroyObjectImmediate(antigo);
                antigo = GameObject.Find(nomes[i]);
            }
        }
    }

    /// <summary>Cria um objeto vazio dentro de outro, só para organizar a Hierarchy.</summary>
    /// <param name="pai">Objeto que vai conter o grupo.</param>
    /// <param name="nome">Nome do grupo.</param>
    /// <returns>O Transform do grupo criado.</returns>
    private static Transform CriarGrupo(Transform pai, string nome)
    {
        GameObject grupo = new GameObject(nome);
        grupo.transform.SetParent(pai, false);
        return grupo.transform;
    }

    /// <summary>Define a tag de um objeto sem travar a montagem caso a tag não exista.</summary>
    /// <param name="go">Objeto que receberá a tag.</param>
    /// <param name="tag">Nome da tag.</param>
    private static void DefinirTag(GameObject go, string tag)
    {
        try
        {
            go.tag = tag;
        }
        catch (UnityException)
        {
            Debug.LogWarning("[Montar Nivel 1] A tag '" + tag + "' ainda nao existe. Crie em Tags and Layers.");
        }
    }

    // ------------------------------------------------------------------
    // Ferramentas de medida e posicionamento
    // ------------------------------------------------------------------

    /// <summary>
    /// Cria uma cópia (instância) do prefab na cena, sem rotação, com escala 1 e na origem.
    /// Também anota o tamanho do prefab no relatório mostrado no final.
    /// </summary>
    /// <param name="caminho">Caminho do prefab dentro de Assets.</param>
    /// <param name="pai">Objeto que será o pai da cópia (pode ser nulo).</param>
    /// <returns>A cópia criada, ou nulo se o prefab não for encontrado.</returns>
    private static GameObject Instanciar(string caminho, Transform pai)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null)
        {
            Debug.LogWarning("[Montar Nivel 1] Prefab nao encontrado: " + caminho);
            return null;
        }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
        go.transform.localScale = Vector3.one;
        go.transform.rotation = Quaternion.identity;
        go.transform.position = Vector3.zero;

        if (relatados.Add(caminho))
            relatorio += "  " + caminho.Substring(caminho.LastIndexOf('/') + 1) + ": " + ObterLimites(go).size.ToString("F2") + "\n";

        return go;
    }

    /// <summary>Mede o tamanho de um prefab criando uma cópia temporária e apagando em seguida.</summary>
    /// <param name="caminho">Caminho do prefab dentro de Assets.</param>
    /// <returns>Tamanho (X, Y, Z) em metros, ou zero se o prefab não existir.</returns>
    private static Vector3 Medir(string caminho)
    {
        GameObject go = Instanciar(caminho, null);
        if (go == null)
            return Vector3.zero;

        Vector3 tamanho = ObterLimites(go).size;
        Object.DestroyImmediate(go);
        return tamanho;
    }

    /// <summary>
    /// Calcula a caixa que envolve todas as malhas do objeto (e dos filhos), em coordenadas do mundo.
    /// </summary>
    /// <param name="go">Objeto a medir.</param>
    /// <returns>A caixa (Bounds) que envolve o objeto.</returns>
    private static Bounds ObterLimites(GameObject go)
    {
        Bounds resultado = new Bounds(go.transform.position, Vector3.zero);
        bool iniciou = false;

        MeshFilter[] filtros = go.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < filtros.Length; i++)
        {
            if (filtros[i].sharedMesh == null)
                continue;

            Bounds malha = filtros[i].sharedMesh.bounds;
            Vector3 centro = malha.center;
            Vector3 meia = malha.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 ponto = filtros[i].transform.TransformPoint(centro + new Vector3(meia.x * x, meia.y * y, meia.z * z));
                        if (!iniciou)
                        {
                            resultado = new Bounds(ponto, Vector3.zero);
                            iniciou = true;
                        }
                        else
                        {
                            resultado.Encapsulate(ponto);
                        }
                    }
                }
            }
        }

        return resultado;
    }

    /// <summary>
    /// Posiciona o objeto com o centro da caixa em (x, z) e a parte de baixo na altura yBase.
    /// Serve para móveis que ficam apoiados no chão ou em cima de outro móvel.
    /// </summary>
    /// <param name="go">Objeto a posicionar.</param>
    /// <param name="x">Posição desejada no eixo X.</param>
    /// <param name="yBase">Altura desejada para a base do objeto.</param>
    /// <param name="z">Posição desejada no eixo Z.</param>
    /// <param name="yaw">Giro em graus ao redor do eixo Y.</param>
    private static void PosicionarBase(GameObject go, float x, float yBase, float z, float yaw)
    {
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.position = Vector3.zero;
        Bounds b = ObterLimites(go);
        go.transform.position += new Vector3(x - b.center.x, yBase - b.min.y, z - b.center.z);
    }

    /// <summary>
    /// Posiciona o objeto com o centro da caixa em (x, z) e o pivô na altura yPivo.
    /// Serve para peças presas na parede (como o quadro), que já nascem com a altura certa no modelo.
    /// </summary>
    /// <param name="go">Objeto a posicionar.</param>
    /// <param name="x">Posição desejada no eixo X.</param>
    /// <param name="yPivo">Altura do pivô do objeto.</param>
    /// <param name="z">Posição desejada no eixo Z.</param>
    /// <param name="yaw">Giro em graus ao redor do eixo Y.</param>
    private static void PosicionarPivo(GameObject go, float x, float yPivo, float z, float yaw)
    {
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.position = new Vector3(0f, yPivo, 0f);
        Bounds b = ObterLimites(go);
        go.transform.position += new Vector3(x - b.center.x, 0f, z - b.center.z);
    }

    // ------------------------------------------------------------------
    // Laboratório: piso, paredes e portas
    // ------------------------------------------------------------------

    /// <summary>Estica uma peça de piso do pack para cobrir todo o InovaLab, com o topo na altura zero.</summary>
    /// <param name="pai">Grupo que receberá o piso.</param>
    private static void ConstruirPiso(Transform pai)
    {
        GameObject piso = Instanciar(PrefabPiso, pai);
        if (piso == null)
            return;

        Vector3 tam = ObterLimites(piso).size;
        if (tam.x > 0.01f && tam.z > 0.01f)
            piso.transform.localScale = new Vector3(LabLargura / tam.x, 1f, LabProfundidade / tam.z);

        Bounds b = ObterLimites(piso);
        piso.transform.position += new Vector3(LabLargura * 0.5f - b.center.x, -b.max.y, LabProfundidade * 0.5f - b.center.z);
        piso.name = "Piso_InovaLab";
        DefinirTag(piso, "Chao");
    }

    /// <summary>
    /// Monta as quatro paredes do laboratório com peças do pack e abre dois vãos:
    /// a porta de entrada (parede oeste) e a porta de saída (parede leste, perto do canto sul).
    /// </summary>
    /// <param name="pai">Grupo que receberá as paredes.</param>
    private static void ConstruirParedes(Transform pai)
    {
        contadorParedes = 1;

        Vector3 tam = Medir(PrefabParede);
        if (Mathf.Max(tam.x, tam.z) < 0.1f)
        {
            Debug.LogWarning("[Montar Nivel 1] Nao consegui medir a parede: " + PrefabParede);
            return;
        }

        float esp = Mathf.Min(tam.x, tam.z);

        Vector3 tamPorta = Medir(PrefabPorta);
        aberturaPorta = Mathf.Max(Mathf.Max(tamPorta.x, tamPorta.z), 2.4f);
        float zEntrada = LabProfundidade * 0.5f;

        // Norte e sul (ao longo de X), um pouco mais longas para fechar os cantos.
        Segmento(pai, true, LabProfundidade + esp * 0.5f, -esp, LabLargura + esp, tam);
        Segmento(pai, true, -esp * 0.5f, -esp, LabLargura + esp, tam);

        // Oeste (ao longo de Z), com o vão da porta de entrada no meio.
        Segmento(pai, false, -esp * 0.5f, 0f, zEntrada - aberturaPorta * 0.5f, tam);
        Segmento(pai, false, -esp * 0.5f, zEntrada + aberturaPorta * 0.5f, LabProfundidade, tam);

        // Leste (ao longo de Z), com o vão da porta de saída no canto sul.
        Segmento(pai, false, LabLargura + esp * 0.5f, aberturaPorta, LabProfundidade, tam);

        ColocarPorta(pai, false, -esp * 0.5f, zEntrada, "Porta_Entrada");
        ColocarPorta(pai, false, LabLargura + esp * 0.5f, aberturaPorta * 0.5f, "Porta_Saida");
    }

    /// <summary>
    /// Preenche um trecho reto de parede com peças lado a lado, esticando levemente cada peça
    /// para o trecho fechar certinho.
    /// </summary>
    /// <param name="pai">Grupo que receberá as peças.</param>
    /// <param name="aoLongoDeX">Verdadeiro se a parede corre ao longo do eixo X; falso se corre ao longo do Z.</param>
    /// <param name="fixo">Coordenada que não muda (Z para paredes ao longo de X, X para paredes ao longo de Z).</param>
    /// <param name="ini">Início do trecho.</param>
    /// <param name="fim">Fim do trecho.</param>
    /// <param name="tam">Tamanho de uma peça de parede, já medido.</param>
    private static void Segmento(Transform pai, bool aoLongoDeX, float fixo, float ini, float fim, Vector3 tam)
    {
        bool compNoX = tam.x >= tam.z;
        float comp = Mathf.Max(tam.x, tam.z);
        float total = fim - ini;
        if (total < 0.2f)
            return;

        int n = Mathf.Max(1, Mathf.RoundToInt(total / comp));
        float passo = total / n;

        for (int i = 0; i < n; i++)
        {
            float centro = ini + passo * (i + 0.5f);

            GameObject peca = Instanciar(PrefabParede, pai);
            if (peca == null)
                return;

            peca.transform.localScale = compNoX ? new Vector3(passo / comp, 1f, 1f) : new Vector3(1f, 1f, passo / comp);

            float yaw = (compNoX == aoLongoDeX) ? 0f : 90f;
            float x = aoLongoDeX ? centro : fixo;
            float z = aoLongoDeX ? fixo : centro;
            PosicionarBase(peca, x, 0f, z, yaw);
            peca.name = "Parede_" + contadorParedes;
            contadorParedes++;
        }
    }

    /// <summary>
    /// Coloca uma porta do pack em um vão da parede. Os colisores da porta ficam desligados
    /// para o jogador conseguir passar; a porta deslizante de verdade vem depois, por script.
    /// </summary>
    /// <param name="pai">Grupo que receberá a porta.</param>
    /// <param name="aoLongoDeX">Verdadeiro se a parede corre ao longo do eixo X.</param>
    /// <param name="fixo">Coordenada fixa da parede.</param>
    /// <param name="centro">Centro do vão ao longo da parede.</param>
    /// <param name="nome">Nome do objeto na Hierarchy.</param>
    private static void ColocarPorta(Transform pai, bool aoLongoDeX, float fixo, float centro, string nome)
    {
        GameObject porta = Instanciar(PrefabPorta, pai);
        if (porta == null)
            return;

        Vector3 tam = ObterLimites(porta).size;
        bool compNoX = tam.x >= tam.z;
        float yaw = (compNoX == aoLongoDeX) ? 0f : 90f;
        float x = aoLongoDeX ? centro : fixo;
        float z = aoLongoDeX ? fixo : centro;

        PosicionarBase(porta, x, 0f, z, yaw);
        porta.name = nome;
        DefinirTag(porta, "Porta");

        Collider[] colisores = porta.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colisores.Length; i++)
            colisores[i].enabled = false;
    }

    // ------------------------------------------------------------------
    // Laboratório: móveis
    // ------------------------------------------------------------------

    /// <summary>
    /// Posiciona as 6 bancadas com computador e cadeira, a recepção, as bancadas de protótipos,
    /// a vitrine do troféu, os armários que fazem o papel do rack de servidores e o quadro.
    /// </summary>
    /// <param name="pai">Grupo que receberá os móveis.</param>
    /// <returns>As caixas das 6 bancadas, usadas depois para posicionar as evidências.</returns>
    private static List<Bounds> ConstruirMobiliario(Transform pai)
    {
        List<Bounds> bancadas = new List<Bounds>();
        float W = LabLargura;
        float D = LabProfundidade;

        // 6 bancadas de computadores: 2 fileiras x 3 colunas, cada uma com computador e cadeira.
        float[] colunas = { W * 0.32f, W * 0.50f, W * 0.68f };
        float[] fileiras = { D * 0.62f, D * 0.38f };
        int numero = 1;

        for (int f = 0; f < fileiras.Length; f++)
        {
            for (int c = 0; c < colunas.Length; c++)
            {
                GameObject mesa = Instanciar(PrefabBancada, pai);
                if (mesa == null)
                    return bancadas;

                PosicionarBase(mesa, colunas[c], 0f, fileiras[f], 0f);
                mesa.name = "Bancada_" + numero;
                Bounds bm = ObterLimites(mesa);
                bancadas.Add(bm);

                GameObject pc = Instanciar(PrefabComputador, pai);
                if (pc != null)
                {
                    PosicionarBase(pc, bm.center.x, bm.max.y, bm.center.z, 180f);
                    pc.name = "Computador_" + numero;
                }

                GameObject cadeira = Instanciar(PrefabCadeira, pai);
                if (cadeira != null)
                {
                    PosicionarBase(cadeira, bm.center.x, 0f, bm.min.z, 0f);
                    Vector3 tc = ObterLimites(cadeira).size;
                    cadeira.transform.position += new Vector3(0f, 0f, -tc.z * 0.5f + 0.1f);
                    cadeira.name = "Cadeira_" + numero;
                }

                numero++;
            }
        }

        // Recepção perto da porta de entrada, com um notebook em cima.
        GameObject recepcao = Instanciar(PrefabRecepcao, pai);
        if (recepcao != null)
        {
            PosicionarBase(recepcao, 3f, 0f, D * 0.78f, 270f);
            recepcao.name = "Recepcao";
            Bounds br = ObterLimites(recepcao);

            GameObject notebook = Instanciar(PrefabNotebook, pai);
            if (notebook != null)
            {
                PosicionarBase(notebook, br.center.x, br.max.y, br.center.z, 270f);
                notebook.name = "Notebook_Recepcao";
            }
        }

        // Bancadas de protótipos na parede sul (impressora 3D, braço robótico e drone entram aqui depois).
        for (int i = 0; i < 2; i++)
        {
            GameObject proto = Instanciar(PrefabBancada, pai);
            if (proto == null)
                break;

            Vector3 tp = ObterLimites(proto).size;
            float lado = (i == 0) ? -1f : 1f;
            PosicionarBase(proto, W * 0.5f + lado * (tp.x * 0.5f + 0.1f), 0f, tp.z * 0.5f + 0.15f, 0f);
            proto.name = "Bancada_Prototipos_" + (i + 1);
        }

        // Vitrine do troféu, encostada na parede norte e virada para o laboratório.
        GameObject vitrine = Instanciar(PrefabVitrine, pai);
        if (vitrine != null)
        {
            PosicionarBase(vitrine, W * 0.5f, 0f, D, 180f);
            Vector3 tv = ObterLimites(vitrine).size;
            vitrine.transform.position += new Vector3(0f, 0f, -tv.z * 0.5f - 0.1f);
            vitrine.name = "Vitrine_Trofeu";
        }

        // Dois armários no canto nordeste fazem o papel do rack de servidores (por enquanto).
        float xAtual = W - 0.6f;
        for (int i = 0; i < 2; i++)
        {
            GameObject armario = Instanciar(PrefabArmario, pai);
            if (armario == null)
                break;

            Vector3 ta = ObterLimites(armario).size;
            PosicionarBase(armario, xAtual - ta.x * 0.5f, 0f, D, 180f);
            armario.transform.position += new Vector3(0f, 0f, -ta.z * 0.5f - 0.1f);
            armario.name = "Rack_Servidores_" + (i + 1);
            xAtual -= ta.x + 0.15f;
        }

        // Quadro da pergunta, na parede leste.
        GameObject quadro = Instanciar(PrefabQuadro, pai);
        if (quadro != null)
        {
            PosicionarPivo(quadro, W, 0f, D * 0.55f, 270f);
            Vector3 tq = ObterLimites(quadro).size;
            quadro.transform.position += new Vector3(-tq.x * 0.5f - 0.05f, 0f, 0f);
            quadro.name = "Quadro_Pergunta";
        }

        return bancadas;
    }

    // ------------------------------------------------------------------
    // Pátio externo
    // ------------------------------------------------------------------

    /// <summary>
    /// Monta o pátio com formas simples: terreno de grama, caminho de cascalho, muro baixo para treinar o pulo,
    /// árvores e limites invisíveis para o jogador não cair do mapa.
    /// </summary>
    /// <param name="pai">Grupo que receberá o pátio.</param>
    private static void ConstruirPatio(Transform pai)
    {
        Material grama = CriarMaterial("Grama_Temp", new Color(0.36f, 0.60f, 0.28f));
        Material cascalho = CriarMaterial("Cascalho_Temp", new Color(0.62f, 0.60f, 0.56f));
        Material tronco = CriarMaterial("Tronco_Temp", new Color(0.40f, 0.26f, 0.13f));
        Material folha = CriarMaterial("Folha_Temp", new Color(0.18f, 0.50f, 0.22f));
        Material muroCor = CriarMaterial("Muro_Temp", new Color(0.78f, 0.74f, 0.66f));

        float D = LabProfundidade;
        float xMin = -PatioLargura;
        float xMax = LabLargura + 6f;
        float zMin = D * 0.5f - PatioProfundidade * 0.5f;
        float zMax = D * 0.5f + PatioProfundidade * 0.5f;

        // Terreno: a face de cima fica 2 cm abaixo do piso do laboratório para não brigar visualmente com ele.
        GameObject terreno = GameObject.CreatePrimitive(PrimitiveType.Cube);
        terreno.name = "Patio_Terreno";
        terreno.transform.SetParent(pai, false);
        terreno.transform.localScale = new Vector3(xMax - xMin, 0.4f, zMax - zMin);
        terreno.transform.position = new Vector3((xMin + xMax) * 0.5f, -0.22f, (zMin + zMax) * 0.5f);
        terreno.GetComponent<Renderer>().sharedMaterial = grama;
        DefinirTag(terreno, "Chao");

        // Caminho de cascalho até a porta do laboratório.
        GameObject caminho = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caminho.name = "Patio_Caminho";
        caminho.transform.SetParent(pai, false);
        caminho.transform.localScale = new Vector3(16f, 0.02f, 3f);
        caminho.transform.position = new Vector3(-8f, -0.01f, D * 0.5f);
        caminho.GetComponent<Renderer>().sharedMaterial = cascalho;
        Object.DestroyImmediate(caminho.GetComponent<Collider>());

        // Muro baixo para treinar o pulo.
        GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        muro.name = "Patio_MuroBaixo";
        muro.transform.SetParent(pai, false);
        muro.transform.localScale = new Vector3(0.6f, 0.8f, 5f);
        muro.transform.position = new Vector3(-14f, 0.4f, D * 0.5f + 5f);
        muro.GetComponent<Renderer>().sharedMaterial = muroCor;

        // Árvores.
        Vector2[] arvores =
        {
            new Vector2(-4f, zMin + 2.5f), new Vector2(-5f, zMax - 2.5f), new Vector2(-10f, zMax - 2f),
            new Vector2(-11f, zMin + 2f), new Vector2(-17f, zMax - 3f), new Vector2(-18f, zMin + 3f),
            new Vector2(-23f, D * 0.5f - 6f), new Vector2(-23f, D * 0.5f + 6f)
        };
        for (int i = 0; i < arvores.Length; i++)
            CriarArvore(pai, arvores[i], tronco, folha, i + 1);

        // Paredes invisíveis ao redor de todo o terreno.
        GameObject limites = new GameObject("Limites_Invisiveis");
        limites.transform.SetParent(pai, false);
        float cx = (xMin + xMax) * 0.5f;
        float cz = (zMin + zMax) * 0.5f;
        AdicionarLimite(limites, new Vector3(xMin - 0.5f, 1.5f, cz), new Vector3(1f, 3f, zMax - zMin + 2f));
        AdicionarLimite(limites, new Vector3(xMax + 0.5f, 1.5f, cz), new Vector3(1f, 3f, zMax - zMin + 2f));
        AdicionarLimite(limites, new Vector3(cx, 1.5f, zMax + 0.5f), new Vector3(xMax - xMin + 2f, 3f, 1f));
        AdicionarLimite(limites, new Vector3(cx, 1.5f, zMin - 0.5f), new Vector3(xMax - xMin + 2f, 3f, 1f));
    }

    /// <summary>Cria uma árvore simples: tronco (cilindro) e copa (esfera).</summary>
    /// <param name="pai">Grupo que receberá a árvore.</param>
    /// <param name="xz">Posição da árvore no chão (X e Z).</param>
    /// <param name="tronco">Material do tronco.</param>
    /// <param name="folha">Material da copa.</param>
    /// <param name="numero">Número usado no nome do objeto.</param>
    private static void CriarArvore(Transform pai, Vector2 xz, Material tronco, Material folha, int numero)
    {
        GameObject raiz = new GameObject("Arvore_" + numero);
        raiz.transform.SetParent(pai, false);
        raiz.transform.position = new Vector3(xz.x, 0f, xz.y);

        GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        t.transform.SetParent(raiz.transform, false);
        t.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);
        t.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        t.GetComponent<Renderer>().sharedMaterial = tronco;

        GameObject copa = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        copa.transform.SetParent(raiz.transform, false);
        copa.transform.localScale = new Vector3(3.2f, 3f, 3.2f);
        copa.transform.localPosition = new Vector3(0f, 3.4f, 0f);
        copa.GetComponent<Renderer>().sharedMaterial = folha;
        Object.DestroyImmediate(copa.GetComponent<Collider>());
    }

    /// <summary>Adiciona uma parede invisível (BoxCollider) a um objeto que está na origem.</summary>
    /// <param name="go">Objeto que receberá o colisor.</param>
    /// <param name="centro">Centro do colisor.</param>
    /// <param name="tamanho">Tamanho do colisor.</param>
    private static void AdicionarLimite(GameObject go, Vector3 centro, Vector3 tamanho)
    {
        BoxCollider caixa = go.AddComponent<BoxCollider>();
        caixa.center = centro;
        caixa.size = tamanho;
    }

    // ------------------------------------------------------------------
    // Pontos de referência para os próximos scripts
    // ------------------------------------------------------------------

    /// <summary>
    /// Cria objetos vazios com ícone colorido que marcam onde ficam o início do jogador,
    /// as evidências (P1 a P6), as ventilações dos ratos (V1 a V4) e outros pontos importantes.
    /// Os próximos scripts (sorteio de evidências e de ratos) vão usar estes pontos.
    /// </summary>
    /// <param name="pai">Grupo que receberá os pontos.</param>
    /// <param name="bancadas">Caixas das bancadas, para colocar evidências em cima delas.</param>
    private static void ConstruirPontos(Transform pai, List<Bounds> bancadas)
    {
        float W = LabLargura;
        float D = LabProfundidade;

        Marcar(pai, "InicioJogador", new Vector3(-PatioLargura * 0.75f, 0f, D * 0.5f), 1);
        Marcar(pai, "Entrada_InovaLab", new Vector3(0f, 0f, D * 0.5f), 1);
        Marcar(pai, "Lanterna_Recepcao", new Vector3(3f, 1f, D * 0.78f), 4);
        Marcar(pai, "TecladoPorta", new Vector3(W - 0.3f, 1.3f, aberturaPorta + 0.5f), 4);

        Marcar(pai, "Evidencia_P1", TopoDe(bancadas, 0, new Vector3(W * 0.32f, 1f, D * 0.62f)), 3);
        Marcar(pai, "Evidencia_P2", TopoDe(bancadas, 4, new Vector3(W * 0.50f, 1f, D * 0.38f)), 3);
        Marcar(pai, "Evidencia_P3", TopoDe(bancadas, 2, new Vector3(W * 0.68f, 1f, D * 0.62f)), 3);
        Marcar(pai, "Evidencia_P4", new Vector3(W * 0.5f + 2.5f, 0.4f, D - 1.5f), 3);
        Marcar(pai, "Evidencia_P5", new Vector3(W - 3.2f, 0.4f, D - 1.5f), 3);
        Marcar(pai, "Evidencia_P6", new Vector3(W - 2f, 0.4f, D * 0.55f + 2f), 3);

        Marcar(pai, "Ventilacao_V1", new Vector3(1f, 0f, D - 1f), 6);
        Marcar(pai, "Ventilacao_V2", new Vector3(W * 0.62f, 0f, 1f), 6);
        Marcar(pai, "Ventilacao_V3", new Vector3(W - 1f, 0f, D * 0.62f), 6);
        Marcar(pai, "Ventilacao_V4", new Vector3(W * 0.35f, 0f, D - 1f), 6);
    }

    /// <summary>Devolve o ponto central do topo de uma bancada, ou um valor padrão se ela não existir.</summary>
    /// <param name="lista">Lista de caixas das bancadas.</param>
    /// <param name="indice">Posição da bancada na lista.</param>
    /// <param name="padrao">Valor usado se a bancada não existir.</param>
    /// <returns>O ponto no topo da bancada.</returns>
    private static Vector3 TopoDe(List<Bounds> lista, int indice, Vector3 padrao)
    {
        if (lista.Count > indice)
            return new Vector3(lista[indice].center.x, lista[indice].max.y, lista[indice].center.z);

        return padrao;
    }

    /// <summary>Cria um objeto vazio com ícone colorido para marcar um ponto no cenário.</summary>
    /// <param name="pai">Grupo que receberá o ponto.</param>
    /// <param name="nome">Nome do ponto.</param>
    /// <param name="posicao">Posição no mundo.</param>
    /// <param name="icone">Número do ícone colorido (0 a 7) usado na Scene.</param>
    /// <returns>O objeto criado.</returns>
    private static GameObject Marcar(Transform pai, string nome, Vector3 posicao, int icone)
    {
        GameObject ponto = new GameObject(nome);
        ponto.transform.SetParent(pai, false);
        ponto.transform.position = posicao;

        GUIContent conteudo = EditorGUIUtility.IconContent("sv_label_" + icone);
        Texture2D textura = (conteudo != null) ? conteudo.image as Texture2D : null;
        if (textura != null)
            EditorGUIUtility.SetIconForObject(ponto, textura);

        return ponto;
    }

    // ------------------------------------------------------------------
    // Jogador, câmera e materiais
    // ------------------------------------------------------------------

    /// <summary>
    /// Cria o jogador provisório (uma cápsula azul com um "nariz" para mostrar a frente)
    /// e liga a câmera principal a ele com o script CameraFollow.
    /// </summary>
    /// <param name="pontos">Grupo com os pontos de referência, de onde sai o início do jogador.</param>
    private static void CriarJogadorECamera(Transform pontos)
    {
        Transform inicio = pontos.Find("InicioJogador");
        Vector3 posInicio = (inicio != null) ? inicio.position : new Vector3(-10f, 0f, LabProfundidade * 0.5f);

        GameObject jogador = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        jogador.name = "Jogador";
        jogador.tag = "Player";
        jogador.transform.position = posInicio + new Vector3(0f, 1.05f, 0f);
        jogador.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        jogador.GetComponent<Renderer>().sharedMaterial = CriarMaterial("Jogador_Temp", new Color(0.20f, 0.45f, 0.95f));

        CapsuleCollider colisor = jogador.GetComponent<CapsuleCollider>();
        colisor.radius = 0.4f;

        Rigidbody corpo = jogador.AddComponent<Rigidbody>();
        corpo.mass = 1f;
        jogador.AddComponent<PlayerController>();

        GameObject nariz = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nariz.name = "Frente";
        nariz.transform.SetParent(jogador.transform, false);
        nariz.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        nariz.transform.localPosition = new Vector3(0f, 0.55f, 0.45f);
        nariz.GetComponent<Renderer>().sharedMaterial = CriarMaterial("Frente_Temp", new Color(1f, 0.85f, 0.2f));
        Object.DestroyImmediate(nariz.GetComponent<Collider>());

        Camera camera = Camera.main;
        if (camera == null)
            camera = Object.FindFirstObjectByType<Camera>();

        if (camera == null)
        {
            Debug.LogWarning("[Montar Nivel 1] Nao encontrei a camera da cena.");
            return;
        }

        CameraFollow seguir = camera.GetComponent<CameraFollow>();
        if (seguir == null)
            seguir = camera.gameObject.AddComponent<CameraFollow>();
        seguir.alvo = jogador.transform;

        Quaternion rotacao = Quaternion.Euler(15f, 90f, 0f);
        Vector3 pivo = jogador.transform.position + seguir.deslocamentoAlvo;
        camera.transform.rotation = rotacao;
        camera.transform.position = pivo + rotacao * Vector3.back * seguir.distancia;
    }

    /// <summary>
    /// Cria (ou atualiza) um material de cor sólida na pasta Assets/_Projeto/Materiais.
    /// </summary>
    /// <param name="nome">Nome do arquivo do material.</param>
    /// <param name="cor">Cor do material.</param>
    /// <returns>O material pronto para usar.</returns>
    private static Material CriarMaterial(string nome, Color cor)
    {
        if (!AssetDatabase.IsValidFolder(PastaMateriais))
            AssetDatabase.CreateFolder("Assets/_Projeto", "Materiais");

        string caminho = PastaMateriais + "/" + nome + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(caminho);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, caminho);
        }

        material.SetColor("_BaseColor", cor);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", cor);

        return material;
    }
}
