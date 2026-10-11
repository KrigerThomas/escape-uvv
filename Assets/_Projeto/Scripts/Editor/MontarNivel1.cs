using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta o Nível 1 COMPLETO e 100% JOGÁVEL com um único clique:
/// - Estrutura fechada do InovaLab (Piso, Paredes sem buracos, Teto e Luz).
/// - Porta de entrada deslizante que abre automaticamente quando o jogador chega perto.
/// - Mobília posicionada e computadores perfeitamente apoiados em cima das mesas.
/// - Lanterna UV na recepção (pega com E, liga com F).
/// - 3 Evidências escondidas nas mesas que só aparecem sob luz UV.
/// - Quadro de segurança com pergunta de tecnologia (tecla E).
/// - Porta de saída com teclado numérico eletrônico (tecla E).
/// - HUD com cronômetro de 5 minutos, código descoberto e botão de reiniciar.
/// - Calouro (Remy) configurado sem o bug de voar.
/// </summary>
public static class MontarNivel1
{
    private const float LabLargura = 18f;
    private const float LabProfundidade = 13f;
    private const float LabAltura = 3.4f;

    private const float PatioLargura = 18f;
    private const float PatioProfundidade = 16f;

    private const string Props = "Assets/school/Prefabs/props/";
    private const string PrefabBancada = Props + "table3.prefab";
    private const string PrefabRecepcao = Props + "table1.prefab";
    private const string PrefabCadeira = Props + "chair.prefab";
    private const string PrefabComputador = Props + "computer.prefab";
    private const string PrefabNotebook = Props + "a laptop.prefab";
    private const string PrefabVitrine = Props + "showcase.prefab";
    private const string PrefabArmario = Props + "locker.prefab";
    private const string PrefabQuadro = Props + "board.prefab";

    private const string PastaMateriais = "Assets/_Projeto/Materiais";
    private static Material matPadraoPack;

    [MenuItem("Escape UVV/Montar Nivel 1 (COMPLETO E JOGAVEL)")]
    public static void Montar()
    {
        bool confirmou = EditorUtility.DisplayDialog("Montar Nível 1 - Jogo Completo",
            "Vou montar o Nível 1 com todas as mecânicas: portas funcionando, lanterna UV, pistas, quiz e saída. Continuar?",
            "Sim, Montar Tudo", "Cancelar");
        if (!confirmou) return;

        GarantirArquivoJogo();
        GarantirTags();
        LimparCenaAnterior();

        matPadraoPack = AssetDatabase.LoadAssetAtPath<Material>("Assets/school/material/Materials/1.mat");

        GameObject raiz = new GameObject("Nivel1_Cenario");
        Undo.RegisterCreatedObjectUndo(raiz, "Montar Nivel 1");

        Transform estrutura = CriarGrupo(raiz.transform, "Estrutura_InovaLab");
        Transform mobiliario = CriarGrupo(raiz.transform, "Mobiliario");
        Transform patio = CriarGrupo(raiz.transform, "Patio_Externo");
        Transform gameplay = CriarGrupo(raiz.transform, "Gameplay_Objetivos");

        GameObject portaSaidaObj = null;
        ConstruirEstruturaInovaLab(estrutura, out portaSaidaObj);
        ConstruirMobiliario(mobiliario);
        ConstruirPatio(patio);
        InstalarGameplay(gameplay, portaSaidaObj);
        ConfigurarJogadorECamera();

        // Controladores de Jogo e Interface
        GameObject gc = new GameObject("_GameController");
        AdicionarComponenteSeguro(gc, "GameManager");
        AdicionarComponenteSeguro(gc, "UIManager");

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Selection.activeGameObject = raiz;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log("<color=green>[Escape UVV]</color> Nível 1 COMPLETO montado! Pressione PLAY para jogar o Escape Room.");
    }

    private static void GarantirTags()
    {
        string[] tags = { "Pista", "Rato", "Porta", "Chao", "Bateria", "Player" };
        Object[] todos = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (todos == null || todos.Length == 0) return;

        SerializedObject gerente = new SerializedObject(todos[0]);
        SerializedProperty lista = gerente.FindProperty("tags");

        for (int t = 0; t < tags.Length; t++)
        {
            bool existe = false;
            for (int i = 0; i < lista.arraySize; i++)
            {
                if (lista.GetArrayElementAtIndex(i).stringValue == tags[t]) { existe = true; break; }
            }
            if (!existe)
            {
                lista.InsertArrayElementAtIndex(lista.arraySize);
                lista.GetArrayElementAtIndex(lista.arraySize - 1).stringValue = tags[t];
            }
        }
        gerente.ApplyModifiedProperties();
    }

    private static void LimparCenaAnterior()
    {
        string[] antigos = { "Nivel1_Cenario", "Jogador", "_GameController" };
        foreach (string n in antigos)
        {
            GameObject obj = GameObject.Find(n);
            if (obj != null) Undo.DestroyObjectImmediate(obj);
        }
    }

    private static Transform CriarGrupo(Transform pai, string nome)
    {
        GameObject grupo = new GameObject(nome);
        grupo.transform.SetParent(pai, false);
        return grupo.transform;
    }

    private static void ConstruirEstruturaInovaLab(Transform pai, out GameObject portaSaidaObj)
    {
        Material matPiso = CriarMaterial("InovaLab_Piso", new Color(0.22f, 0.25f, 0.30f));
        Material matParede = CriarMaterial("InovaLab_Parede", new Color(0.88f, 0.90f, 0.93f));
        Material matTeto = CriarMaterial("InovaLab_Teto", new Color(0.35f, 0.37f, 0.40f));
        Material matPorta = CriarMaterial("Porta_Material", new Color(0.45f, 0.28f, 0.16f));
        Material matVidro = CriarMaterial("Vidro_Porta", new Color(0.50f, 0.75f, 0.90f));

        float W = LabLargura;
        float D = LabProfundidade;
        float H = LabAltura;

        // Piso e Teto
        GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piso.name = "Piso_InovaLab";
        piso.tag = "Chao";
        piso.transform.SetParent(pai, false);
        piso.transform.localScale = new Vector3(W, 0.2f, D);
        piso.transform.position = new Vector3(W * 0.5f, -0.1f, D * 0.5f);
        piso.GetComponent<Renderer>().sharedMaterial = matPiso;

        GameObject teto = GameObject.CreatePrimitive(PrimitiveType.Cube);
        teto.name = "Teto_InovaLab";
        teto.transform.SetParent(pai, false);
        teto.transform.localScale = new Vector3(W, 0.2f, D);
        teto.transform.position = new Vector3(W * 0.5f, H + 0.1f, D * 0.5f);
        teto.GetComponent<Renderer>().sharedMaterial = matTeto;

        // Paredes Norte e Sul
        CriarParede(pai, "Parede_Norte", new Vector3(W * 0.5f, H * 0.5f, D), new Vector3(W, H, 0.3f), matParede);
        CriarParede(pai, "Parede_Sul", new Vector3(W * 0.5f, H * 0.5f, 0f), new Vector3(W, H, 0.3f), matParede);

        float zEntrada = D * 0.5f;
        float largPorta = 2.4f;
        float altPorta = 2.5f;

        // Parede Oeste (com vão da porta)
        CriarParede(pai, "Parede_Oeste_A", new Vector3(0f, H * 0.5f, (zEntrada - largPorta * 0.5f) * 0.5f), new Vector3(0.3f, H, zEntrada - largPorta * 0.5f), matParede);
        CriarParede(pai, "Parede_Oeste_B", new Vector3(0f, H * 0.5f, (D + zEntrada + largPorta * 0.5f) * 0.5f), new Vector3(0.3f, H, D - (zEntrada + largPorta * 0.5f)), matParede);
        CriarParede(pai, "Parede_Oeste_Topo", new Vector3(0f, (H + altPorta) * 0.5f, zEntrada), new Vector3(0.3f, H - altPorta, largPorta), matParede);

        // Porta de Entrada DESLIZANTE
        GameObject portaEntrada = GameObject.CreatePrimitive(PrimitiveType.Cube);
        portaEntrada.name = "Porta_Entrada_Deslizante";
        portaEntrada.tag = "Porta";
        portaEntrada.transform.SetParent(pai, false);
        portaEntrada.transform.localScale = new Vector3(0.08f, altPorta, largPorta);
        portaEntrada.transform.position = new Vector3(0f, altPorta * 0.5f, zEntrada);
        portaEntrada.GetComponent<Renderer>().sharedMaterial = matVidro;

        // Gatilho que abre a porta automaticamente
        BoxCollider gatilhoEntrada = portaEntrada.AddComponent<BoxCollider>();
        gatilhoEntrada.isTrigger = true;
        gatilhoEntrada.size = new Vector3(40f, 1.2f, 1.5f); // Área ao redor da porta
        AdicionarComponenteSeguro(portaEntrada, "PortaEntrada");

        // Parede Leste (com vão da porta de saída)
        float zSaida = 2.0f;
        CriarParede(pai, "Parede_Leste_A", new Vector3(W, H * 0.5f, (D + zSaida + largPorta * 0.5f) * 0.5f), new Vector3(0.3f, H, D - (zSaida + largPorta * 0.5f)), matParede);
        CriarParede(pai, "Parede_Leste_Topo", new Vector3(W, (H + altPorta) * 0.5f, zSaida), new Vector3(0.3f, H - altPorta, largPorta), matParede);

        // Porta de Saída (Trancada com teclado)
        portaSaidaObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        portaSaidaObj.name = "Porta_Saida_Trancada";
        portaSaidaObj.tag = "Porta";
        portaSaidaObj.transform.SetParent(pai, false);
        portaSaidaObj.transform.localScale = new Vector3(0.12f, altPorta, largPorta);
        portaSaidaObj.transform.position = new Vector3(W, altPorta * 0.5f, zSaida);
        portaSaidaObj.GetComponent<Renderer>().sharedMaterial = matPorta;

        // Luz de teto
        GameObject luzLab = new GameObject("Luz_Interna_InovaLab");
        luzLab.transform.SetParent(pai, false);
        luzLab.transform.position = new Vector3(W * 0.5f, H - 0.4f, D * 0.5f);
        Light luz = luzLab.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 22f;
        luz.intensity = 2.2f;
        luz.color = new Color(0.88f, 0.93f, 1f);
    }

    private static void CriarParede(Transform pai, string nome, Vector3 pos, Vector3 escala, Material mat)
    {
        GameObject parede = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parede.name = nome;
        parede.transform.SetParent(pai, false);
        parede.transform.position = pos;
        parede.transform.localScale = escala;
        parede.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void ConstruirMobiliario(Transform pai)
    {
        float W = LabLargura;
        float D = LabProfundidade;

        float[] colunas = { W * 0.33f, W * 0.50f, W * 0.67f };
        float[] fileiras = { D * 0.62f, D * 0.38f };
        int idx = 1;

        for (int f = 0; f < fileiras.Length; f++)
        {
            for (int c = 0; c < colunas.Length; c++)
            {
                Instanciar(PrefabBancada, pai, "Bancada_" + idx, new Vector3(colunas[c], 0f, fileiras[f]), 0f, 1f);
                // Altura Y = 1.05m para os computadores apoiarem exatamente no tampo
                Instanciar(PrefabComputador, pai, "Computador_" + idx, new Vector3(colunas[c], 1.151f, fileiras[f]), 180f, 1f);
                Instanciar(PrefabCadeira, pai, "Cadeira_" + idx, new Vector3(colunas[c], 0f, fileiras[f] - 0.6f), 0f, 1f);
                idx++;
            }
        }

        Instanciar(PrefabRecepcao, pai, "Mesa_Recepcao", new Vector3(2.5f, 0f, D * 0.75f), 90f, 1f);
        Instanciar(PrefabNotebook, pai, "Notebook_Recepcao", new Vector3(2.5f, 0.95f, D * 0.75f), 90f, 1f);
        Instanciar(PrefabVitrine, pai, "Vitrine_Trofeu", new Vector3(W * 0.5f, 0f, D - 0.6f), 180f, 1f);
        Instanciar(PrefabArmario, pai, "Rack_Servidores_1", new Vector3(W - 1.2f, 0f, D - 0.6f), 180f, 1f);
        Instanciar(PrefabArmario, pai, "Rack_Servidores_2", new Vector3(W - 2.2f, 0f, D - 0.6f), 180f, 1f);

        // Quadro de Perguntas rente à parede e interativo
        GameObject quadro = Instanciar(PrefabQuadro, pai, "Quadro_Pergunta", new Vector3(W - 0.35f, 1.4f, D * 0.55f), 270f, 0.8f);
        if (quadro != null)
            AdicionarComponenteSeguro(quadro, "InteracaoQuadro");

        Instanciar(PrefabBancada, pai, "Bancada_Prototipos_1", new Vector3(W * 0.38f, 0f, 0.8f), 0f, 1f);
        Instanciar(PrefabBancada, pai, "Bancada_Prototipos_2", new Vector3(W * 0.62f, 0f, 0.8f), 0f, 1f);
    }

    private static void ConstruirPatio(Transform pai)
    {
        Material matGrama = CriarMaterial("Patio_Grama", new Color(0.32f, 0.55f, 0.26f));
        Material matCascalho = CriarMaterial("Patio_Cascalho", new Color(0.58f, 0.56f, 0.52f));
        Material matMuro = CriarMaterial("Patio_Muro", new Color(0.72f, 0.70f, 0.65f));
        Material matTronco = CriarMaterial("Patio_Tronco", new Color(0.42f, 0.28f, 0.15f));
        Material matFolhas = CriarMaterial("Patio_Folhas", new Color(0.20f, 0.48f, 0.22f));

        float D = LabProfundidade;

        GameObject grama = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grama.name = "Chao_Patio";
        grama.tag = "Chao";
        grama.transform.SetParent(pai, false);
        grama.transform.localScale = new Vector3(PatioLargura, 0.2f, PatioProfundidade);
        grama.transform.position = new Vector3(-PatioLargura * 0.5f, -0.1f, D * 0.5f);
        grama.GetComponent<Renderer>().sharedMaterial = matGrama;

        GameObject caminho = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caminho.name = "Caminho_Cascalho";
        caminho.transform.SetParent(pai, false);
        caminho.transform.localScale = new Vector3(PatioLargura, 0.02f, 2.4f);
        caminho.transform.position = new Vector3(-PatioLargura * 0.5f, 0.01f, D * 0.5f);
        caminho.GetComponent<Renderer>().sharedMaterial = matCascalho;
        Object.DestroyImmediate(caminho.GetComponent<Collider>());

        GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        muro.name = "Muro_TreinoPulo";
        muro.transform.SetParent(pai, false);
        muro.transform.localScale = new Vector3(0.5f, 0.5f, 4.0f);
        muro.transform.position = new Vector3(-8f, 0.425f, D * 0.5f + 2.5f);
        muro.GetComponent<Renderer>().sharedMaterial = matMuro;

        Vector2[] posArvores =
        {
            new Vector2(-4f, 2f), new Vector2(-4f, D - 2f),
            new Vector2(-11f, 2.5f), new Vector2(-11f, D - 2.5f),
            new Vector2(-16f, D * 0.5f - 4.5f), new Vector2(-16f, D * 0.5f + 4.5f)
        };

        for (int i = 0; i < posArvores.Length; i++)
        {
            GameObject arvore = new GameObject("Arvore_" + (i + 1));
            arvore.transform.SetParent(pai, false);
            arvore.transform.position = new Vector3(posArvores[i].x, 0f, posArvores[i].y);

            GameObject tronco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tronco.transform.SetParent(arvore.transform, false);
            tronco.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);
            tronco.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            tronco.GetComponent<Renderer>().sharedMaterial = matTronco;

            GameObject copa = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            copa.transform.SetParent(arvore.transform, false);
            copa.transform.localScale = new Vector3(2.8f, 2.6f, 2.8f);
            copa.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            copa.GetComponent<Renderer>().sharedMaterial = matFolhas;
            Object.DestroyImmediate(copa.GetComponent<Collider>());
        }
    }

    /// <summary>
    /// Instala a Lanterna UV, as 3 Evidências secretas e o Teclado numérico da porta de saída.
    /// </summary>
    private static void InstalarGameplay(Transform pai, GameObject portaSaidaObj)
    {
        float W = LabLargura;
        float D = LabProfundidade;

        // 1. Lanterna UV na mesa da recepção
        GameObject lanterna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lanterna.name = "Lanterna_UV_Pickup";
        lanterna.tag = "Bateria";
        lanterna.transform.SetParent(pai, false);
        lanterna.transform.localScale = new Vector3(0.12f, 0.25f, 0.12f);
        lanterna.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        lanterna.transform.position = new Vector3(2.5f, 0.5f, D * 0.75f + 0.4f);
        lanterna.GetComponent<Renderer>().sharedMaterial = CriarMaterial("Lanterna_Cor", new Color(0.5f, 0.1f, 0.9f));

        GameObject luzObj = new GameObject("Luz_Spot_UV");
        luzObj.transform.SetParent(lanterna.transform, false);
        Light spot = luzObj.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.spotAngle = 48f;
        spot.range = 10f;
        spot.intensity = 3.5f;
        spot.color = new Color(0.65f, 0.2f, 1f);

        Component compLuv = AdicionarComponenteSeguro(lanterna, "LanternaUV");
        if (compLuv != null)
        {
            var field = compLuv.GetType().GetField("luzUV");
            if (field != null) field.SetValue(compLuv, spot);
        }

        // 2. Três Evidências Secretas (aparecem sob luz UV)
        CriarEvidencia(pai, "Evidencia_1_PanoLaranja", new Vector3(W * 0.33f, 0.82f, D * 0.62f + 0.4f), "Pano Laranja Rasgado", new Color(1f, 0.5f, 0f));
        CriarEvidencia(pai, "Evidencia_2_Queijo", new Vector3(W * 0.50f, 0.82f, D * 0.38f + 0.4f), "Migalhas de Queijo", new Color(1f, 0.9f, 0.2f));
        CriarEvidencia(pai, "Evidencia_3_PenDrive", new Vector3(W * 0.67f, 0.82f, D * 0.62f + 0.4f), "Pen Drive de Segurança", new Color(0.2f, 0.8f, 1f));

        // 3. Teclado da Porta de Saída
        GameObject teclado = GameObject.CreatePrimitive(PrimitiveType.Cube);
        teclado.name = "Teclado_Porta_Saida";
        teclado.transform.SetParent(pai, false);
        teclado.transform.localScale = new Vector3(0.08f, 0.4f, 0.3f);
        teclado.transform.position = new Vector3(W - 0.2f, 1.3f, 3.4f);
        teclado.GetComponent<Renderer>().sharedMaterial = CriarMaterial("Teclado_Cor", new Color(0.2f, 0.2f, 0.2f));

        Component compTps = AdicionarComponenteSeguro(teclado, "TecladoPortaSaida");
        if (compTps != null && portaSaidaObj != null)
        {
            var field = compTps.GetType().GetField("portaSaidaObjeto");
            if (field != null) field.SetValue(compTps, portaSaidaObj);
        }
    }

    private static void CriarEvidencia(Transform pai, string objNome, Vector3 pos, string nomeExibicao, Color cor)
    {
        GameObject ev = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ev.name = objNome;
        ev.tag = "Pista";
        ev.transform.SetParent(pai, false);
        ev.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        ev.transform.position = pos;
        ev.GetComponent<Renderer>().sharedMaterial = CriarMaterial(objNome + "_Mat", cor);

        Component compEv = AdicionarComponenteSeguro(ev, "EvidenciaItem");
        if (compEv != null)
        {
            var field = compEv.GetType().GetField("nomeEvidencia");
            if (field != null) field.SetValue(compEv, nomeExibicao);
        }
    }

    private static void ConfigurarJogadorECamera()
    {
        Vector3 posInicio = new Vector3(-12f, 0.05f, LabProfundidade * 0.5f);

        GameObject jogador = GameObject.Find("Jogador");
        if (jogador == null) jogador = GameObject.Find("Remy");

        if (jogador == null)
        {
            GameObject prefabRemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Projeto/Models/Remy.fbx");
            if (prefabRemy != null)
                jogador = (GameObject)PrefabUtility.InstantiatePrefab(prefabRemy);
        }

        if (jogador == null)
            jogador = GameObject.CreatePrimitive(PrimitiveType.Capsule);

        jogador.name = "Jogador";
        jogador.tag = "Player";
        jogador.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        jogador.transform.position = posInicio + Vector3.up * 0.05f;
        jogador.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        CapsuleCollider colisor = jogador.GetComponent<CapsuleCollider>();
        if (colisor == null) colisor = jogador.AddComponent<CapsuleCollider>();
        colisor.center = new Vector3(0f, 0.9f, 0f);
        colisor.height = 1.75f;
        colisor.radius = 0.32f;

        Rigidbody corpo = jogador.GetComponent<Rigidbody>();
        if (corpo == null) corpo = jogador.AddComponent<Rigidbody>();
        corpo.mass = 1f;

        PlayerController pc = jogador.GetComponent<PlayerController>();
        if (pc == null) pc = jogador.AddComponent<PlayerController>();

        // Desativa Root Motion no Animator para o boneco não voar
        Animator anim = jogador.GetComponent<Animator>();
        if (anim != null)
            anim.applyRootMotion = false;

        Camera camera = Camera.main;
        if (camera == null) camera = Object.FindFirstObjectByType<Camera>();

        if (camera != null)
        {
            CameraFollow seguir = camera.GetComponent<CameraFollow>();
            if (seguir == null) seguir = camera.gameObject.AddComponent<CameraFollow>();
            seguir.alvo = jogador.transform;
            seguir.deslocamentoAlvo = new Vector3(0f, 1.25f, 0f);
            seguir.distancia = 3.6f;

            Quaternion rotacao = Quaternion.Euler(14f, 90f, 0f);
            Vector3 pivo = jogador.transform.position + seguir.deslocamentoAlvo;
            camera.transform.rotation = rotacao;
            camera.transform.position = pivo + rotacao * Vector3.back * seguir.distancia;
        }
    }

    private static GameObject Instanciar(string caminho, Transform pai, string nome, Vector3 pos, float yaw, float escala)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null) return null;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
        go.name = nome;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one * escala;

        if (matPadraoPack != null)
        {
            MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer r in renderers)
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                int qtd = (mf != null && mf.sharedMesh != null) ? Mathf.Max(1, mf.sharedMesh.subMeshCount) : 1;
                Material[] novosMats = new Material[qtd];
                for (int i = 0; i < qtd; i++)
                    novosMats[i] = matPadraoPack;
                r.sharedMaterials = novosMats;
            }
        }

        return go;
    }

    private static Material CriarMaterial(string nome, Color cor)
    {
        if (!AssetDatabase.IsValidFolder(PastaMateriais))
            AssetDatabase.CreateFolder("Assets/_Projeto", "Materiais");

        string caminho = PastaMateriais + "/" + nome + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(caminho);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, caminho);
        }

        material.SetColor("_BaseColor", cor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", cor);
        return material;
    }

    private static Component AdicionarComponenteSeguro(GameObject go, string nomeDoTipo)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var tipo = assembly.GetType(nomeDoTipo);
            if (tipo != null && typeof(Component).IsAssignableFrom(tipo))
            {
                return go.AddComponent(tipo);
            }
        }
        return null;
    }

    private static void GarantirArquivoJogo()
    {
        string pastaScripts = "Assets/_Projeto/Scripts";
        if (!System.IO.Directory.Exists(pastaScripts))
            System.IO.Directory.CreateDirectory(pastaScripts);

        string caminhoArquivo = pastaScripts + "/EscapeUVV_Game.cs";
        if (!System.IO.File.Exists(caminhoArquivo))
        {
            System.IO.File.WriteAllText(caminhoArquivo, CodigoDoJogo);
            AssetDatabase.Refresh();
            Debug.Log("<color=cyan>[Escape UVV]</color> Arquivo EscapeUVV_Game.cs gerado automaticamente!");
        }
    }

    private const string CodigoDoJogo = @"using System.Collections.Generic;
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
        if (other.CompareTag(""Player""))
        {
            jogadorPerto = true;
            if (GameManager.Instancia != null)
                GameManager.Instancia.IniciarCronometro();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(""Player""))
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

    [Header(""Cronômetro"")]
    public float tempoRestante = 300f; // 5 minutos
    public bool cronometroAtivo = false;

    [Header(""Código Secreto da Porta"")]
    public int[] codigoPorta = new int[4];
    public bool[] digitoDescoberto = new bool[4];

    [Header(""Progresso"")]
    public int evidenciasColetadas = 0;
    public bool temLanterna = false;
    public bool jogoVencido = false;
    public bool jogoPerdido = false;

    [Header(""Interface"")]
    public string mensagemInteracao = """";
    public string mensagemAlerta = """";
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
                Derrota(""O tempo esgotou! Os gatos correm perigo!"");
            }
        }

        if (tempoAlerta > 0f)
        {
            tempoAlerta -= Time.deltaTime;
            if (tempoAlerta <= 0f)
                mensagemAlerta = """";
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
            MostrarAlerta($""Evidência encontrada ({evidenciasColetadas}/3): {nomeEvidencia}!\nDígito {evidenciasColetadas} do código: [{digito}]"");
        }
    }

    public void AcertarPerguntaQuadro()
    {
        digitoDescoberto[3] = true;
        MostrarAlerta($""Resposta correta no quadro!\nÚltimo dígito (4º) do código: [{codigoPorta[3]}]"");
    }

    public bool ValidarCodigo(int d1, int d2, int d3, int d4)
    {
        if (d1 == codigoPorta[0] && d2 == codigoPorta[1] && d3 == codigoPorta[2] && d4 == codigoPorta[3])
        {
            Vitoria();
            return true;
        }
        MostrarAlerta(""Código incorreto! Confira as pistas e a resposta do quadro."");
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

        GameObject jog = GameObject.FindGameObjectWithTag(""Player"");
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
                GameManager.Instancia.mensagemInteracao = ""Pressione [E] para pegar a Lanterna UV"";

            if (Input.GetKeyDown(KeyCode.E))
            {
                coletada = true;
                if (GameManager.Instancia != null)
                {
                    GameManager.Instancia.temLanterna = true;
                    GameManager.Instancia.mensagemInteracao = """";
                    GameManager.Instancia.MostrarAlerta(""Lanterna UV coletada! Aperte [F] para ligar a luz roxa."");
                }
            }
        }
        else if (GameManager.Instancia != null && GameManager.Instancia.mensagemInteracao.Contains(""Lanterna""))
        {
            GameManager.Instancia.mensagemInteracao = """";
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
    public string nomeEvidencia = ""Pedaço de Pano Laranja"";
    public float velocidadeGiro = 45f;
    private Renderer rend;
    private bool coletada = false;
    private Transform jogador;
    private LanternaUV lanterna;

    void Start()
    {
        rend = GetComponent<Renderer>();
        GameObject jog = GameObject.FindGameObjectWithTag(""Player"");
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
                GameManager.Instancia.mensagemInteracao = $""Pressione [E] para coletar: {nomeEvidencia}"";

            if (Input.GetKeyDown(KeyCode.E))
            {
                coletada = true;
                if (GameManager.Instancia != null)
                {
                    GameManager.Instancia.mensagemInteracao = """";
                    GameManager.Instancia.ColetarEvidencia(nomeEvidencia);
                }
                gameObject.SetActive(false);
            }
        }
        else if (GameManager.Instancia != null && GameManager.Instancia.mensagemInteracao.Contains(nomeEvidencia))
        {
            GameManager.Instancia.mensagemInteracao = """";
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
        GameObject jog = GameObject.FindGameObjectWithTag(""Player"");
        if (jog != null)
            jogadorPerto = Vector3.Distance(transform.position, jog.transform.position) < 3.2f;

        if (jogadorPerto && !acertou)
        {
            if (!mostrarJanela && GameManager.Instancia != null)
                GameManager.Instancia.mensagemInteracao = ""Pressione [E] para ler o Quadro de Segurança"";

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

        GUI.Box(new Rect(x, y, w, h), ""PERGUNTA DE SEGURANÇA - INOVALAB"");
        GUI.Label(new Rect(x + 20, y + 40, w - 40, 60), 
            ""Quantos bits formam exatamente 1 Byte no sistema de computação?\n(A resposta correta é o 4º dígito do código da porta!)"");

        if (GUI.Button(new Rect(x + 30, y + 110, 110, 45), ""4 Bits""))
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.MostrarAlerta(""Byte diz: 'Esse palpite nem compila! Tente de novo.'"");
        }

        if (GUI.Button(new Rect(x + 160, y + 110, 110, 45), ""6 Bits""))
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.MostrarAlerta(""Byte diz: 'Quase! Releia com atenção.'"");
        }

        if (GUI.Button(new Rect(x + 290, y + 110, 110, 45), ""8 Bits""))
        {
            acertou = true;
            mostrarJanela = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (GameManager.Instancia != null)
                GameManager.Instancia.AcertarPerguntaQuadro();
        }

        if (GUI.Button(new Rect(x + 160, y + 190, 110, 35), ""Fechar [E]""))
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
    private string entradaCodigo = """";
    public GameObject portaSaidaObjeto;

    void Update()
    {
        GameObject jog = GameObject.FindGameObjectWithTag(""Player"");
        if (jog != null)
            jogadorPerto = Vector3.Distance(transform.position, jog.transform.position) < 2.5f;

        if (jogadorPerto)
        {
            if (!mostrarTeclado && GameManager.Instancia != null && !GameManager.Instancia.jogoVencido)
                GameManager.Instancia.mensagemInteracao = ""Pressione [E] para acessar o Teclado da Porta"";

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

        GUI.Box(new Rect(x, y, w, h), ""FECHADURA ELETRÔNICA - SAÍDA"");
        GUI.Box(new Rect(x + 40, y + 40, w - 80, 40), $""CÓDIGO: {entradaCodigo}"");

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

        if (GUI.Button(new Rect(x + 50, y + 270, 60, btnH), ""C"")) entradaCodigo = """";
        if (GUI.Button(new Rect(x + 125, y + 270, 60, btnH), ""0""))
        {
            if (entradaCodigo.Length < 4) entradaCodigo += ""0"";
        }

        if (GUI.Button(new Rect(x + 200, y + 270, 60, btnH), ""OK""))
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
                    entradaCodigo = """";
                }
            }
        }

        if (GUI.Button(new Rect(x + 110, y + 330, 100, 30), ""Fechar [E]""))
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
        string corTempo = gm.tempoRestante < 30f ? ""<color=red>"" : ""<color=white>"";
        string textoTempo = $""{corTempo}TEMPO: {minutos:00}:{segundos:00}</color>"";

        GUIStyle estiloTempo = new GUIStyle(GUI.skin.box);
        estiloTempo.fontSize = 18;
        estiloTempo.richText = true;
        GUI.Box(new Rect(Screen.width / 2 - 90, 15, 180, 40), textoTempo, estiloTempo);

        // Painel de Pistas
        string d1 = gm.digitoDescoberto[0] ? gm.codigoPorta[0].ToString() : ""?"";
        string d2 = gm.digitoDescoberto[1] ? gm.codigoPorta[1].ToString() : ""?"";
        string d3 = gm.digitoDescoberto[2] ? gm.codigoPorta[2].ToString() : ""?"";
        string d4 = gm.digitoDescoberto[3] ? gm.codigoPorta[3].ToString() : ""?"";

        string textoPistas = $""EVIDÊNCIAS: {gm.evidenciasColetadas}/3\nCÓDIGO DA PORTA: [ {d1} - {d2} - {d3} - {d4} ]"";
        GUI.Box(new Rect(20, 20, 260, 60), textoPistas);

        // Lanterna
        if (lanterna == null) lanterna = Object.FindFirstObjectByType<LanternaUV>();
        if (gm.temLanterna && lanterna != null)
        {
            float pct = lanterna.bateriaAtual / lanterna.bateriaMaxima;
            string estadoLuz = lanterna.ligada ? ""<color=magenta>[LIGADA - F]</color>"" : ""[DESLIGADA - F]"";
            GUI.Box(new Rect(20, Screen.height - 70, 240, 50), $""LANTERNA UV: {estadoLuz}\nBateria: {(pct * 100):0}%"");
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

            GUI.Box(new Rect(x, y, w, h), ""PARABÉNS! VOCÊ ESCAPOU!"");
            GUI.Label(new Rect(x + 20, y + 40, w - 40, 70),
                ""Você reuniu as evidências de rato e o pano laranja!\n"" +
                ""A inocência dos gatos do campus está provada!"");

            if (GUI.Button(new Rect(x + 140, y + 130, 170, 50), ""REINICIAR JOGO""))
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

            GUI.Box(new Rect(x, y, w, h), ""TEMPO ESGOTADO!"");
            GUI.Label(new Rect(x + 20, y + 50, w - 40, 50), ""O tempo acabou antes de você conseguir abrir a porta."");

            if (GUI.Button(new Rect(x + 120, y + 110, 160, 45), ""REINICIAR""))
            {
                gm.ReiniciarFase();
            }
        }
    }
}
#endregion
";
}
