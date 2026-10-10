using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta o esqueleto principal do Nível 1 (Pátio + InovaLab Fechado com Teto):
/// - Corrige automaticamente os materiais para eliminar 100% dos objetos rosas.
/// - Cria paredes sólidas de laboratório e teto fechado (sem buracos abertos de janela).
/// - Posiciona as 6 bancadas com computadores e cadeiras, recepção, vitrine, rack e lousa.
/// - Configura o Pátio externo com grama, caminho de cascalho, muro de pulo e árvores.
/// - Posiciona os pontos de evidência (P1 a P6) e ventilações de ratos (V1 a V4) do Croqui.
/// - Configura o Calouro (Remy) e enquadra a câmera em terceira pessoa com visão perfeita.
/// </summary>
public static class MontarNivel1
{
    // Dimensões do InovaLab (em metros)
    private const float LabLargura = 20f;       // Eixo X
    private const float LabProfundidade = 14f;   // Eixo Z
    private const float LabAltura = 3.6f;        // Altura do teto (Eixo Y)

    // Dimensões do Pátio Externo
    private const float PatioLargura = 22f;
    private const float PatioProfundidade = 20f;

    // Prefabs do pack School
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

    [MenuItem("Escape UVV/Montar Nivel 1 (Estrutura Limpa e Fechada)")]
    public static void Montar()
    {
        bool confirmou = EditorUtility.DisplayDialog("Montar Nível 1 - InovaLab",
            "Vou recriar o esqueleto do cenário e posicionar o Remy e a Câmera. Deseja continuar?",
            "Sim, Montar", "Cancelar");
        if (!confirmou)
            return;

        GarantirTags();
        LimparCenaAnterior();

        // Carrega o material do pack corrigido para evitar objetos rosas
        matPadraoPack = AssetDatabase.LoadAssetAtPath<Material>("Assets/school/material/Materials/1.mat");

        GameObject raiz = new GameObject("Nivel1_Cenario");
        Undo.RegisterCreatedObjectUndo(raiz, "Montar Nivel 1");

        Transform estrutura = CriarGrupo(raiz.transform, "Estrutura_InovaLab");
        Transform mobiliario = CriarGrupo(raiz.transform, "Mobiliario");
        Transform patio = CriarGrupo(raiz.transform, "Patio_Externo");
        Transform pontos = CriarGrupo(raiz.transform, "Pontos_Logicos");

        ConstruirEstruturaInovaLab(estrutura);
        ConstruirMobiliario(mobiliario);
        ConstruirPatio(patio);
        ConstruirPontos(pontos);
        ConfigurarJogadorECamera(pontos);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Selection.activeGameObject = raiz;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log("<color=green>[Escape UVV]</color> Nível 1 montado com sucesso! InovaLab fechado com teto e sem materiais rosas.");
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
        GameObject cenarioAntigo = GameObject.Find("Nivel1_Cenario");
        if (cenarioAntigo != null) Undo.DestroyObjectImmediate(cenarioAntigo);
    }

    private static Transform CriarGrupo(Transform pai, string nome)
    {
        GameObject grupo = new GameObject(nome);
        grupo.transform.SetParent(pai, false);
        return grupo.transform;
    }

    /// <summary>
    /// Constrói o laboratório fechado: Piso, Paredes Sólidas, Teto e Luz de Teto.
    /// </summary>
    private static void ConstruirEstruturaInovaLab(Transform pai)
    {
        Material matPiso = CriarMaterial("InovaLab_Piso", new Color(0.20f, 0.23f, 0.28f));
        Material matParede = CriarMaterial("InovaLab_Parede", new Color(0.85f, 0.88f, 0.92f));
        Material matTeto = CriarMaterial("InovaLab_Teto", new Color(0.30f, 0.32f, 0.35f));

        float W = LabLargura;
        float D = LabProfundidade;
        float H = LabAltura;

        // 1. Piso do Laboratório
        GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piso.name = "Piso_InovaLab";
        piso.tag = "Chao";
        piso.transform.SetParent(pai, false);
        piso.transform.localScale = new Vector3(W, 0.2f, D);
        piso.transform.position = new Vector3(W * 0.5f, -0.1f, D * 0.5f);
        piso.GetComponent<Renderer>().sharedMaterial = matPiso;

        // 2. Teto do Laboratório (Fecha a sala completamente)
        GameObject teto = GameObject.CreatePrimitive(PrimitiveType.Cube);
        teto.name = "Teto_InovaLab";
        teto.transform.SetParent(pai, false);
        teto.transform.localScale = new Vector3(W, 0.2f, D);
        teto.transform.position = new Vector3(W * 0.5f, H + 0.1f, D * 0.5f);
        teto.GetComponent<Renderer>().sharedMaterial = matTeto;

        // 3. Paredes Sólidas
        CriarParede(pai, "Parede_Norte", new Vector3(W * 0.5f, H * 0.5f, D), new Vector3(W, H, 0.3f), matParede);
        CriarParede(pai, "Parede_Sul", new Vector3(W * 0.5f, H * 0.5f, 0f), new Vector3(W, H, 0.3f), matParede);

        // Parede Oeste (Lado do pátio com o vão da porta)
        float zEntrada = D * 0.5f;
        float largPorta = 2.4f;
        CriarParede(pai, "Parede_Oeste_1", new Vector3(0f, H * 0.5f, (zEntrada - largPorta * 0.5f) * 0.5f), new Vector3(0.3f, H, zEntrada - largPorta * 0.5f), matParede);
        CriarParede(pai, "Parede_Oeste_2", new Vector3(0f, H * 0.5f, (D + zEntrada + largPorta * 0.5f) * 0.5f), new Vector3(0.3f, H, D - (zEntrada + largPorta * 0.5f)), matParede);
        CriarParede(pai, "Parede_Oeste_TopoPorta", new Vector3(0f, H - 0.5f, zEntrada), new Vector3(0.3f, 1f, largPorta), matParede);

        // Parede Leste (Lado do teclado com o vão de saída)
        float zSaida = 2.2f;
        CriarParede(pai, "Parede_Leste_1", new Vector3(W, H * 0.5f, (D + zSaida + largPorta * 0.5f) * 0.5f), new Vector3(0.3f, H, D - (zSaida + largPorta * 0.5f)), matParede);
        CriarParede(pai, "Parede_Leste_TopoPorta", new Vector3(W, H - 0.5f, zSaida), new Vector3(0.3f, 1f, largPorta), matParede);

        // 4. Luz interna do Laboratório
        GameObject luzLab = new GameObject("Luz_Interna_InovaLab");
        luzLab.transform.SetParent(pai, false);
        luzLab.transform.position = new Vector3(W * 0.5f, H - 0.5f, D * 0.5f);
        Light luz = luzLab.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 25f;
        luz.intensity = 2.2f;
        luz.color = new Color(0.85f, 0.92f, 1f);
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

    /// <summary>
    /// Instancia móveis e limpa os slots vazios que causavam a cor rosa.
    /// </summary>
    private static void ConstruirMobiliario(Transform pai)
    {
        float W = LabLargura;
        float D = LabProfundidade;

        // 6 Bancadas no centro (2 fileiras x 3 colunas)
        float[] colunas = { W * 0.32f, W * 0.50f, W * 0.68f };
        float[] fileiras = { D * 0.62f, D * 0.38f };
        int idx = 1;

        for (int f = 0; f < fileiras.Length; f++)
        {
            for (int c = 0; c < colunas.Length; c++)
            {
                Instanciar(PrefabBancada, pai, "Bancada_" + idx, new Vector3(colunas[c], 0f, fileiras[f]), 0f);
                Instanciar(PrefabComputador, pai, "Computador_" + idx, new Vector3(colunas[c], 0.75f, fileiras[f]), 180f);
                Instanciar(PrefabCadeira, pai, "Cadeira_" + idx, new Vector3(colunas[c], 0f, fileiras[f] - 0.6f), 0f);
                idx++;
            }
        }

        // Recepção
        Instanciar(PrefabRecepcao, pai, "Mesa_Recepcao", new Vector3(2.5f, 0f, D * 0.75f), 90f);
        Instanciar(PrefabNotebook, pai, "Notebook_Recepcao", new Vector3(2.5f, 0.75f, D * 0.75f), 90f);

        // Vitrine do Troféu na parede Norte
        Instanciar(PrefabVitrine, pai, "Vitrine_Trofeu", new Vector3(W * 0.5f, 0f, D - 0.8f), 180f);

        // Racks de Servidores no canto
        Instanciar(PrefabArmario, pai, "Rack_Servidores_1", new Vector3(W - 1.2f, 0f, D - 0.8f), 180f);
        Instanciar(PrefabArmario, pai, "Rack_Servidores_2", new Vector3(W - 2.4f, 0f, D - 0.8f), 180f);

        // Quadro de Perguntas na parede leste
        Instanciar(PrefabQuadro, pai, "Quadro_Pergunta", new Vector3(W - 0.2f, 1.2f, D * 0.60f), 270f);

        // Bancadas de protótipos na parede sul
        Instanciar(PrefabBancada, pai, "Bancada_Prototipos_1", new Vector3(W * 0.40f, 0f, 0.8f), 0f);
        Instanciar(PrefabBancada, pai, "Bancada_Prototipos_2", new Vector3(W * 0.60f, 0f, 0.8f), 0f);
    }

    private static void ConstruirPatio(Transform pai)
    {
        Material matGrama = CriarMaterial("Patio_Grama", new Color(0.32f, 0.55f, 0.26f));
        Material matCascalho = CriarMaterial("Patio_Cascalho", new Color(0.58f, 0.56f, 0.52f));
        Material matMuro = CriarMaterial("Patio_Muro", new Color(0.72f, 0.70f, 0.65f));
        Material matTronco = CriarMaterial("Patio_Tronco", new Color(0.42f, 0.28f, 0.15f));
        Material matFolhas = CriarMaterial("Patio_Folhas", new Color(0.20f, 0.48f, 0.22f));

        float D = LabProfundidade;

        // Terreno do Pátio
        GameObject grama = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grama.name = "Chao_Patio";
        grama.tag = "Chao";
        grama.transform.SetParent(pai, false);
        grama.transform.localScale = new Vector3(PatioLargura, 0.2f, PatioProfundidade);
        grama.transform.position = new Vector3(-PatioLargura * 0.5f, -0.1f, D * 0.5f);
        grama.GetComponent<Renderer>().sharedMaterial = matGrama;

        // Caminho de cascalho
        GameObject caminho = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caminho.name = "Caminho_Cascalho";
        caminho.transform.SetParent(pai, false);
        caminho.transform.localScale = new Vector3(PatioLargura, 0.05f, 2.6f);
        caminho.transform.position = new Vector3(-PatioLargura * 0.5f, 0.02f, D * 0.5f);
        caminho.GetComponent<Renderer>().sharedMaterial = matCascalho;
        Object.DestroyImmediate(caminho.GetComponent<Collider>());

        // Muro baixo (Treino de pulo)
        GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        muro.name = "Muro_TreinoPulo";
        muro.transform.SetParent(pai, false);
        muro.transform.localScale = new Vector3(0.5f, 0.85f, 4.5f);
        muro.transform.position = new Vector3(-8f, 0.425f, D * 0.5f + 3f);
        muro.GetComponent<Renderer>().sharedMaterial = matMuro;

        // Árvores simples
        Vector2[] posArvores =
        {
            new Vector2(-4f, 2f), new Vector2(-4f, D - 2f),
            new Vector2(-12f, 2.5f), new Vector2(-12f, D - 2.5f),
            new Vector2(-18f, D * 0.5f - 5f), new Vector2(-18f, D * 0.5f + 5f)
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
            copa.transform.localScale = new Vector3(3f, 2.8f, 3f);
            copa.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            copa.GetComponent<Renderer>().sharedMaterial = matFolhas;
            Object.DestroyImmediate(copa.GetComponent<Collider>());
        }
    }

    private static void ConstruirPontos(Transform pai)
    {
        float W = LabLargura;
        float D = LabProfundidade;

        Marcar(pai, "InicioJogador", new Vector3(-14f, 0f, D * 0.5f));
        Marcar(pai, "Lanterna_Recepcao", new Vector3(2.5f, 0.85f, D * 0.75f));
        Marcar(pai, "TecladoPorta", new Vector3(W - 0.2f, 1.3f, 3.5f));

        // 6 Pontos de Evidências (Croqui)
        Marcar(pai, "Evidencia_P1", new Vector3(W * 0.32f, 0.85f, D * 0.62f));
        Marcar(pai, "Evidencia_P2", new Vector3(W * 0.50f, 0.85f, D * 0.38f));
        Marcar(pai, "Evidencia_P3", new Vector3(W * 0.68f, 0.85f, D * 0.62f));
        Marcar(pai, "Evidencia_P4", new Vector3(W * 0.50f + 2f, 0.3f, D - 1.2f));
        Marcar(pai, "Evidencia_P5", new Vector3(W - 2.5f, 0.3f, D - 1.2f));
        Marcar(pai, "Evidencia_P6", new Vector3(W - 2.5f, 0.3f, D * 0.55f));

        // 4 Ventilações de Ratos (Croqui V1 a V4)
        Marcar(pai, "Ventilacao_V1", new Vector3(1f, 0.2f, D - 0.5f));
        Marcar(pai, "Ventilacao_V2", new Vector3(W * 0.62f, 0.2f, 0.5f));
        Marcar(pai, "Ventilacao_V3", new Vector3(W - 0.5f, 0.2f, D * 0.62f));
        Marcar(pai, "Ventilacao_V4", new Vector3(W * 0.35f, 0.2f, D - 0.5f));
    }

    private static void ConfigurarJogadorECamera(Transform pontos)
    {
        Transform inicio = pontos.Find("InicioJogador");
        Vector3 posInicio = (inicio != null) ? inicio.position : new Vector3(-14f, 0f, LabProfundidade * 0.5f);

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
        jogador.transform.position = posInicio;
        jogador.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        CapsuleCollider colisor = jogador.GetComponent<CapsuleCollider>();
        if (colisor == null) colisor = jogador.AddComponent<CapsuleCollider>();
        colisor.center = new Vector3(0f, 0.9f, 0f);
        colisor.height = 1.8f;
        colisor.radius = 0.35f;

        Rigidbody corpo = jogador.GetComponent<Rigidbody>();
        if (corpo == null) corpo = jogador.AddComponent<Rigidbody>();
        corpo.mass = 1f;

        PlayerController pc = jogador.GetComponent<PlayerController>();
        if (pc == null) pc = jogador.AddComponent<PlayerController>();

        // Câmera
        Camera camera = Camera.main;
        if (camera == null) camera = Object.FindFirstObjectByType<Camera>();

        if (camera != null)
        {
            CameraFollow seguir = camera.GetComponent<CameraFollow>();
            if (seguir == null) seguir = camera.gameObject.AddComponent<CameraFollow>();
            seguir.alvo = jogador.transform;
            seguir.deslocamentoAlvo = new Vector3(0f, 1.4f, 0f); // Mira na altura dos olhos
            seguir.distancia = 3.6f;                            // Distância ideal de 3ª pessoa

            Quaternion rotacao = Quaternion.Euler(12f, 90f, 0f);
            Vector3 pivo = jogador.transform.position + seguir.deslocamentoAlvo;
            camera.transform.rotation = rotacao;
            camera.transform.position = pivo + rotacao * Vector3.back * seguir.distancia;
        }
    }

    /// <summary>
    /// Instancia prefab do pack e remove os slots nulos que causavam a cor rosa.
    /// </summary>
    private static GameObject Instanciar(string caminho, Transform pai, string nome, Vector3 pos, float yaw)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null) return null;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
        go.name = nome;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // CORREÇÃO DEFINITIVA: Remove os slots vazios (nulos) que deixavam tudo rosa
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

    private static GameObject Marcar(Transform pai, string nome, Vector3 posicao)
    {
        GameObject ponto = new GameObject(nome);
        ponto.transform.SetParent(pai, false);
        ponto.transform.position = posicao;

        GUIContent conteudo = EditorGUIUtility.IconContent("sv_label_3");
        Texture2D textura = (conteudo != null) ? conteudo.image as Texture2D : null;
        if (textura != null)
            EditorGUIUtility.SetIconForObject(ponto, textura);

        return ponto;
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