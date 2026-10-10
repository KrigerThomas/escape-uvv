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
        gc.AddComponent<GameManager>();
        gc.AddComponent<UIManager>();

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
        portaEntrada.AddComponent<PortaEntrada>();

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
            quadro.AddComponent<InteracaoQuadro>();

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

        LanternaUV luv = lanterna.AddComponent<LanternaUV>();
        luv.luzUV = spot;

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

        TecladoPortaSaida tps = teclado.AddComponent<TecladoPortaSaida>();
        tps.portaSaidaObjeto = portaSaidaObj;
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

        EvidenciaItem item = ev.AddComponent<EvidenciaItem>();
        item.nomeEvidencia = nomeExibicao;
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
}
