using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Monta, na cena Nivel2, as plantas e os animais dos pacotes importados:
/// flores (Patchmesh e Ornamental Flower Set), o gato cinza (Realistic Cat Demo),
/// o esquilo (Furry Squirrel) e os pássaros (Living Birds).
/// Tudo fica dentro do objeto "Fauna_e_Flora". Rodar de novo apaga esse objeto e monta outra vez,
/// então qualquer ajuste manual nele se perde: ajuste as posições aqui no código.
/// Também deixa os pássaros prontos para o URP (materiais) e cria as tags que o Living Birds usa.
/// </summary>
public static class MontarFaunaFloraNivel2
{
    private const string RAIZ = "Fauna_e_Flora";
    private const string PATCH = "Assets/_Pacotes/Patchmesh/Free Sample Stylized Hand-Painted Plant & Flower Pack/Prefabs/";
    private const string ORNAMENTAL = "Assets/_Pacotes/Ornamental Flower Set/Resources/Furniture01/FurniturePrefab/";
    private const string PREFAB_GATO = "Assets/_Pacotes/Realistic Cat Demo/Prefabs/URP/HD/Cat_05.prefab";
    private const string PREFAB_ESQUILO = "Assets/_Pacotes/Furry Squirrel/Prefab/Squirrel_URP.prefab";
    private const string FBX_ESQUILO = "Assets/_Pacotes/Furry Squirrel/Meshes/Squirrel.fbx";
    private const string CONTROLLER_ESQUILO = "Assets/_Projeto/Animations/Esquilo.controller";
    private const string PASSAROS = "Assets/_Pacotes/living birds/";
    private const string AUDIO = "Assets/_Projeto/Audio/Nivel2/";
    private const string EFEITO_COLETA = "Assets/_Projeto/Prefabs/Efeitos/Efeito_Coleta.prefab";

    /// <summary>Ajuste do giro do modelo do esquilo para ele correr de frente (o FBX não aponta para +Z).</summary>
    private const float GIRO_MODELO_ESQUILO = 0f;

    [MenuItem("Escape UVV/Nível 2/Adicionar plantas e animais")]
    public static void Montar()
    {
        if (EditorSceneManager.GetActiveScene().name != "Nivel2")
        {
            EditorUtility.DisplayDialog("Plantas e animais", "Abra a cena Nivel2 antes de usar este menu.", "OK");
            return;
        }

        GarantirTags("lb_bird", "lb_groundTarget", "lb_perchTarget");
        ConverterMateriaisPassaros();
        RuntimeAnimatorController controllerEsquilo = CriarControllerEsquilo();

        GameObject antigo = GameObject.Find(RAIZ);
        if (antigo != null)
            Object.DestroyImmediate(antigo);

        GameObject raiz = new GameObject(RAIZ);
        Transform plantas = NovoFilho(raiz.transform, "Plantas");
        Transform animais = NovoFilho(raiz.transform, "Animais");

        MontarPlantas(plantas);
        MontarGatoCinza(animais);
        MontarEsquilo(animais, controllerEsquilo);
        MontarPassaros(animais);
        LigarEspantaPassarosNaCena();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(raiz.scene);
        EditorSceneManager.SaveScene(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log("[Fauna e Flora] Plantas e animais montados na cena Nivel2.");
    }

    // ------------------------------------------------------------------
    //  Plantas
    // ------------------------------------------------------------------

    /// <summary>Canteiros do pátio (entre a porta do InovaLab e a entrada do labirinto) e da praça central.</summary>
    private static void MontarPlantas(Transform pai)
    {
        AudioClip folhas = Audio("sfx_folhas_provisorio.wav");
        AudioClip coruja = Audio("sfx_coruja.mp3");

        // Porta do InovaLab: floreiras dos dois lados e vasos junto à parede
        GameObject floreira1 = Colocar(ORNAMENTAL + "Furniture_flowerstand_01_lod_set.prefab", pai, -2.6f, -55.9f, 0f, 1.0f);
        GameObject floreira2 = Colocar(ORNAMENTAL + "Furniture_flowerstand_02_lod_set.prefab", pai, 2.6f, -55.9f, 0f, 1.0f);
        ColocarEmCima(ORNAMENTAL + "Furniture_foliageplant_09_lod_set.prefab", floreira1, 30f, 0.5f);
        ColocarEmCima(ORNAMENTAL + "Furniture_foliageplant_11_lod_set.prefab", floreira2, -40f, 0.5f);
        Colocar(ORNAMENTAL + "Furniture_foliageplant_08_lod_set.prefab", pai, -5.0f, -56.2f, 20f, 0.75f);
        Colocar(ORNAMENTAL + "Furniture_foliageplant_10_lod_set.prefab", pai, 5.0f, -56.2f, 60f, 0.75f);
        Colocar(ORNAMENTAL + "Furniture_foliageplant_12_lod_set.prefab", pai, 7.0f, -56.2f, 10f, 0.7f);
        Colocar(ORNAMENTAL + "Furniture_foliageplant_09_lod_set.prefab", pai, -7.0f, -56.2f, -35f, 0.7f);
        Colocar(ORNAMENTAL + "Furniture_foliageplant_11_lod_set.prefab", pai, 12.5f, -36.4f, 0f, 0.7f);

        // Amores-perfeitos na frente das floreiras
        ComBalanco(Colocar(PATCH + "Pansy Purple & Yellow 1.prefab", pai, -3.7f, -54.9f, 30f, 0.3f), folhas);
        ComBalanco(Colocar(PATCH + "Pansy Purple & Yellow 1.prefab", pai, 3.7f, -54.9f, -50f, 0.3f), folhas);

        // Margaridas nas beiradas do caminho principal
        float[] zEsquerda = { -48.5f, -40.5f, -29.5f, -24.5f };
        float[] zDireita = { -48.8f, -37.8f, -31.5f };
        for (int i = 0; i < zEsquerda.Length; i++)
        {
            string flor = i % 2 == 0 ? "Daisy 1 White.prefab" : "Daisy 1 Pink.prefab";
            ComBalanco(Colocar(PATCH + flor, pai, -4.3f, zEsquerda[i], i * 47f, 0.45f), folhas);
        }
        for (int i = 0; i < zDireita.Length; i++)
        {
            string flor = i % 2 == 0 ? "Daisy 1 Pink.prefab" : "Daisy 1 White.prefab";
            ComBalanco(Colocar(PATCH + flor, pai, 4.3f, zDireita[i], 100f + i * 63f, 0.45f), folhas);
        }
        Colocar(PATCH + "Daisy White & Pink Fallen Petals 3.prefab", pai, -3.9f, -40.0f, 15f, 0.7f, true);
        Colocar(PATCH + "Daisy Pink Fallen Petals 3.prefab", pai, 3.9f, -48.2f, 80f, 0.6f, true);
        Colocar(PATCH + "Daisy White Fallen Petals 3.prefab", pai, -3.9f, -24.0f, 200f, 0.6f, true);

        // Roseiras perto da horta (com pétalas caídas)
        ComBalanco(Colocar(PATCH + "Red Roses 2.prefab", pai, 7.6f, -29.6f, 0f, 0.8f), folhas);
        ComBalanco(Colocar(PATCH + "Red Roses 2.prefab", pai, 8.4f, -28.4f, 140f, 0.75f), folhas);
        Colocar(PATCH + "Roses Red Fallen Petals 3.prefab", pai, 7.9f, -28.9f, 30f, 0.8f, true);

        // Jiboias em vaso ao lado dos bancos
        Colocar(PATCH + "Pothos 4.prefab", pai, -4.3f, -33.4f, 0f, 0.55f);
        Colocar(PATCH + "Pothos 4.prefab", pai, 4.3f, -25.0f, 90f, 0.55f);

        // Praça central (meio do labirinto): roseiras ao lado do canteiro
        ComBalanco(Colocar(PATCH + "Red Roses 2.prefab", pai, -1.4f, 5.1f, 60f, 0.95f), folhas);
        ComBalanco(Colocar(PATCH + "Red Roses 2.prefab", pai, 1.4f, 5.1f, 220f, 0.95f), folhas);
        Colocar(PATCH + "Roses Red Fallen Petals 3.prefab", pai, 0f, 3.9f, 0f, 0.9f, true);

        // Easter egg: girassóis que seguem o jogador, perto do primeiro poste
        Vector3[] girassois = { new Vector3(-7.6f, 0f, -47.2f), new Vector3(-8.5f, 0f, -46.3f), new Vector3(-6.9f, 0f, -45.7f) };
        for (int i = 0; i < girassois.Length; i++)
        {
            GameObject g = Colocar(PATCH + "Sunflower 2.prefab", pai, girassois[i].x, girassois[i].z, 90f, 1.6f + i * 0.12f);
            if (g == null) continue;
            GirassolSegueJogador seguir = g.AddComponent<GirassolSegueJogador>();
            seguir.velocidadeGiro = 1.0f + i * 0.25f;
            seguir.mostrarAviso = i == 0;
            seguir.somAviso = coruja;
        }
    }

    /// <summary>Adiciona o balanço com som de folhas numa planta (se ela existir).</summary>
    private static void ComBalanco(GameObject planta, AudioClip folhas)
    {
        if (planta == null) return;
        PlantaBalanco balanco = planta.AddComponent<PlantaBalanco>();
        balanco.somFolhas = folhas;
    }

    // ------------------------------------------------------------------
    //  Animais
    // ------------------------------------------------------------------

    /// <summary>Gato cinza sentado perto do banco da entrada; muda de lugar depois do easter egg.</summary>
    private static void MontarGatoCinza(Transform pai)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_GATO);
        if (prefab == null) { Debug.LogWarning("[Fauna e Flora] Não achei " + PREFAB_GATO); return; }

        Transform lugaresPai = NovoFilho(pai, "Gato_Cinza_Lugares");
        Transform[] lugares =
        {
            NovoPonto(lugaresPai, "Lugar_1_Banco_Entrada", 2.1f, -44.6f, 200f),
            NovoPonto(lugaresPai, "Lugar_2_Horta", 12.8f, -30.4f, -120f),
            NovoPonto(lugaresPai, "Lugar_3_Banco_Esquerda", -2.1f, -35.6f, 150f),
        };
        foreach (Transform lugar in lugares)
            LimparGrama(lugar.position, 1.4f);

        GameObject gato = new GameObject("Gato_Cinza");
        gato.transform.SetParent(pai);
        gato.transform.position = lugares[0].position;
        gato.transform.rotation = lugares[0].rotation;

        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gato.transform);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;
        Animator anim = modelo.GetComponentInChildren<Animator>();
        if (anim != null) anim.applyRootMotion = false;

        gato.AddComponent<AudioSource>().playOnAwake = false;
        GatoCinza script = gato.AddComponent<GatoCinza>();
        script.lugares = lugares;
        script.somMiado = Audio("sfx_miau_provisorio.wav");
        script.somRonronar = Audio("sfx_gato_ronronar_provisorio.wav");
        script.somChiado = Audio("sfx_gato_chiado_provisorio.wav");

        gato.AddComponent<EspantaPassaros>().raio = 1.6f;
    }

    /// <summary>Esquilo que foge entre pontos na beira da trilha, do início do pátio até o labirinto.</summary>
    private static void MontarEsquilo(Transform pai, RuntimeAnimatorController controller)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_ESQUILO);
        if (prefab == null) { Debug.LogWarning("[Fauna e Flora] Não achei " + PREFAB_ESQUILO); return; }

        Transform pontosPai = NovoFilho(pai, "Esquilo_Esconderijos");
        Transform[] esconderijos =
        {
            // Na beira da trilha de cascalho (x de -1,9 a 1,9), perto dos postes,
            // para o jogador ver o esquilo no caminho até o labirinto.
            NovoPonto(pontosPai, "Beira_Trilha_1", -4.0f, -43.5f, 0f),
            NovoPonto(pontosPai, "Beira_Trilha_2", -4.2f, -39.0f, 0f),
            NovoPonto(pontosPai, "Beira_Trilha_3", 4.3f, -39.5f, 0f),
            NovoPonto(pontosPai, "Beira_Trilha_4", -4.0f, -29.5f, 0f),
            NovoPonto(pontosPai, "Beira_Trilha_5", 4.2f, -23.5f, 0f),
        };
        foreach (Transform ponto in esconderijos)
            LimparGrama(ponto.position, 1.8f);

        GameObject esquilo = new GameObject("Esquilo");
        esquilo.transform.SetParent(pai);
        esquilo.transform.position = esconderijos[0].position;
        esquilo.transform.rotation = Quaternion.Euler(0f, 120f, 0f);

        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, esquilo.transform);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.Euler(0f, GIRO_MODELO_ESQUILO, 0f);
        NormalizarTamanho(modelo, 0.7f, true);
        ApoiarNoChao(modelo, esquilo.transform.position.y);

        Animator anim = modelo.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
        }

        esquilo.AddComponent<AudioSource>().playOnAwake = false;
        EsquiloFujao script = esquilo.AddComponent<EsquiloFujao>();
        script.esconderijos = esconderijos;
        script.somChiado = Audio("sfx_esquilo_chiado_provisorio.wav");
        script.distanciaSusto = 3f;
        script.intervaloChiado = new Vector2(4f, 8f);
        script.somPresente = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Projeto/Audio/Geral/sfx_coleta.wav");
        script.efeitoPresente = AssetDatabase.LoadAssetAtPath<GameObject>(EFEITO_COLETA);

        esquilo.AddComponent<EspantaPassaros>().raio = 1.2f;
    }

    /// <summary>Controlador do Living Birds e as áreas de pouso (gramados do pátio e praça central).</summary>
    private static void MontarPassaros(Transform pai)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PASSAROS + "prefabs/_livingBirdsController.prefab");
        if (prefab == null) { Debug.LogWarning("[Fauna e Flora] Não achei o controlador do Living Birds."); return; }

        Transform grupo = NovoFilho(pai, "Passaros");
        GameObject controlador = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo);
        controlador.transform.position = new Vector3(0f, 0f, -38f);

        lb_BirdController passaros = controlador.GetComponent<lb_BirdController>();
        passaros.idealNumberOfBirds = 5;
        passaros.maximumNumberOfBirds = 8;
        passaros.unspawnDistance = 35f;
        passaros.highQuality = true;
        passaros.collideWithObjects = true;
        passaros.groundLayer = 1 << 0; // Default (terreno)
        passaros.crow = false;          // corvos espantam os outros pássaros o tempo todo
        EditorUtility.SetDirty(passaros);

        // Só em chão de pedra/terra: na grama do terreno os pássaros somem
        AreaDePouso(grupo, "Pouso_Caminho_Sul", 0f, -40.5f, 3f, 4f);
        AreaDePouso(grupo, "Pouso_Caminho_Norte", 0f, -30f, 3f, 5f);
        AreaDePouso(grupo, "Pouso_Horta", 11.8f, -27.2f, 3f, 2.5f);
        AreaDePouso(grupo, "Pouso_Porta_InovaLab", 0f, -53.5f, 4f, 2f);
        AreaDePouso(grupo, "Pouso_Praca_Central", 0f, 1.5f, 3.5f, 3.5f);
    }

    /// <summary>Caixa (trigger) onde os pássaros podem pousar. Fica na camada Ignore Raycast para não atrapalhar o chão do jogador.</summary>
    private static void AreaDePouso(Transform pai, string nome, float x, float z, float largura, float comprimento)
    {
        GameObject area = new GameObject(nome);
        area.tag = "lb_groundTarget";
        area.layer = 2; // Ignore Raycast
        area.transform.SetParent(pai);
        area.transform.position = new Vector3(x, AlturaChao(x, z), z);

        BoxCollider caixa = area.AddComponent<BoxCollider>();
        caixa.isTrigger = true;
        caixa.center = new Vector3(0f, 1f, 0f);
        caixa.size = new Vector3(largura, 2f, comprimento);
    }

    /// <summary>Coloca o EspantaPassaros no jogador e na Misty (se ainda não tiverem).</summary>
    private static void LigarEspantaPassarosNaCena()
    {
        GameObject jogador = GameObject.FindWithTag("Player");
        if (jogador != null && jogador.GetComponent<EspantaPassaros>() == null)
            Undo.AddComponent<EspantaPassaros>(jogador).raio = 2.5f;

        GameObject misty = GameObject.Find("Misty");
        if (misty != null && misty.GetComponent<EspantaPassaros>() == null)
            Undo.AddComponent<EspantaPassaros>(misty).raio = 1.6f;
    }

    // ------------------------------------------------------------------
    //  Preparação dos assets
    // ------------------------------------------------------------------

    /// <summary>Cria as tags que faltarem no projeto.</summary>
    private static void GarantirTags(params string[] tags)
    {
        SerializedObject gerenciador = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty lista = gerenciador.FindProperty("tags");

        foreach (string tag in tags)
        {
            bool existe = false;
            for (int i = 0; i < lista.arraySize; i++)
                if (lista.GetArrayElementAtIndex(i).stringValue == tag) { existe = true; break; }

            if (existe) continue;
            lista.InsertArrayElementAtIndex(lista.arraySize);
            lista.GetArrayElementAtIndex(lista.arraySize - 1).stringValue = tag;
        }

        gerenciador.ApplyModifiedProperties();
    }

    /// <summary>Troca os shaders antigos (Built-in) dos pássaros por shaders do URP, mantendo as texturas.</summary>
    private static void ConverterMateriaisPassaros()
    {
        Shader simplesLit = Shader.Find("Universal Render Pipeline/Simple Lit");
        Shader particula = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { PASSAROS + "Materials-Textures" }))
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat == null || mat.shader == null || mat.shader.name.StartsWith("Universal Render Pipeline"))
                continue;

            Texture textura = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Color cor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

            if (mat.name.Contains("feather"))
            {
                mat.shader = particula;
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                mat.shader = simplesLit;
            }

            if (textura != null) mat.SetTexture("_BaseMap", textura);
            mat.SetColor("_BaseColor", cor);
            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Cria um Animator Controller só com as animações que o EsquiloFujao usa (o do pacote
    /// passa sozinho por todas, inclusive a de morrer). Também liga o "loop" das animações no FBX.
    /// </summary>
    private static RuntimeAnimatorController CriarControllerEsquilo()
    {
        ModelImporter importador = AssetImporter.GetAtPath(FBX_ESQUILO) as ModelImporter;
        if (importador != null)
        {
            ModelImporterClipAnimation[] clipes = importador.clipAnimations;
            bool mudou = false;
            if (clipes == null || clipes.Length == 0)
            {
                clipes = importador.defaultClipAnimations;
                mudou = true; // grava a lista de animações no .meta para o loop ficar salvo
            }

            foreach (ModelImporterClipAnimation clipe in clipes)
            {
                bool deveRepetir = clipe.name.EndsWith("Idle") || clipe.name.EndsWith("Idle2") || clipe.name.EndsWith("Running");
                if (deveRepetir && !clipe.loopTime)
                {
                    clipe.loopTime = true;
                    mudou = true;
                }
            }

            if (mudou)
            {
                importador.clipAnimations = clipes;
                importador.SaveAndReimport();
            }
        }

        var clipesPorNome = new Dictionary<string, AnimationClip>();
        foreach (Object objeto in AssetDatabase.LoadAllAssetsAtPath(FBX_ESQUILO))
            if (objeto is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                clipesPorNome[clip.name] = clip;

        AssetDatabase.DeleteAsset(CONTROLLER_ESQUILO);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_ESQUILO);
        AnimatorStateMachine maquina = controller.layers[0].stateMachine;

        AnimatorState parado = maquina.AddState("parado");
        parado.motion = Clipe(clipesPorNome, "Idle");
        maquina.AddState("parado2").motion = Clipe(clipesPorNome, "Idle2");
        maquina.AddState("correr").motion = Clipe(clipesPorNome, "Running");
        maquina.defaultState = parado;

        AssetDatabase.SaveAssets();
        return controller;
    }

    /// <summary>Acha o clipe cujo nome termina com "|sufixo" (ex.: Squirrel_Armature|Idle).</summary>
    private static AnimationClip Clipe(Dictionary<string, AnimationClip> clipes, string sufixo)
    {
        foreach (var par in clipes)
            if (par.Key.EndsWith("|" + sufixo) || par.Key == sufixo)
                return par.Value;

        Debug.LogWarning("[Fauna e Flora] Não achei a animação " + sufixo + " do esquilo.");
        return null;
    }

    // ------------------------------------------------------------------
    //  Utilitários
    // ------------------------------------------------------------------

    /// <summary>Instancia um prefab no chão, com o tamanho desejado (altura, ou largura se "porLargura").</summary>
    private static GameObject Colocar(string caminho, Transform pai, float x, float z, float giroY, float tamanho, bool porLargura = false)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null)
        {
            Debug.LogWarning("[Fauna e Flora] Não achei " + caminho);
            return null;
        }

        GameObject objeto = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
        objeto.transform.position = Vector3.zero;
        objeto.transform.rotation = Quaternion.Euler(0f, giroY, 0f);
        NormalizarTamanho(objeto, tamanho, porLargura);

        objeto.transform.position = new Vector3(x, 0f, z);
        ApoiarNoChao(objeto, AlturaChao(x, z));
        return objeto;
    }

    /// <summary>Coloca um prefab apoiado no topo de outro objeto (ex.: vaso em cima da floreira).</summary>
    private static GameObject ColocarEmCima(string caminho, GameObject base_, float giroY, float tamanho)
    {
        if (base_ == null) return null;
        Bounds limitesBase = Limites(base_);
        GameObject objeto = Colocar(caminho, base_.transform.parent, limitesBase.center.x, limitesBase.center.z, giroY, tamanho);
        if (objeto != null) ApoiarNoChao(objeto, limitesBase.max.y - 0.02f);
        return objeto;
    }

    /// <summary>
    /// Tira a grama (detalhes do terreno) num círculo, para os animais pequenos não sumirem nela.
    /// Muda o asset do terreno (Nivel2_Terreno).
    /// </summary>
    private static void LimparGrama(Vector3 centro, float raio)
    {
        Terrain terreno = Terrain.activeTerrain;
        if (terreno == null) return;

        TerrainData dados = terreno.terrainData;
        int resolucao = dados.detailResolution;
        Vector3 local = centro - terreno.transform.position;
        float cx = local.x / dados.size.x * resolucao;
        float cz = local.z / dados.size.z * resolucao;
        float r = raio / dados.size.x * resolucao;

        int x0 = Mathf.Clamp(Mathf.FloorToInt(cx - r), 0, resolucao - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(cz - r), 0, resolucao - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(cx + r), 0, resolucao - 1);
        int z1 = Mathf.Clamp(Mathf.CeilToInt(cz + r), 0, resolucao - 1);
        int largura = x1 - x0 + 1;
        int altura = z1 - z0 + 1;

        for (int camada = 0; camada < dados.detailPrototypes.Length; camada++)
        {
            int[,] mapa = dados.GetDetailLayer(x0, z0, largura, altura, camada);
            for (int z = 0; z < altura; z++)
                for (int x = 0; x < largura; x++)
                {
                    float dx = x0 + x + 0.5f - cx;
                    float dz = z0 + z + 0.5f - cz;
                    if (dx * dx + dz * dz <= r * r)
                        mapa[z, x] = 0;
                }
            dados.SetDetailLayer(x0, z0, camada, mapa);
        }

        EditorUtility.SetDirty(dados);
    }

    /// <summary>Muda a escala para a altura (ou maior largura) do objeto ficar igual a "tamanho".</summary>
    private static void NormalizarTamanho(GameObject objeto, float tamanho, bool porLargura)
    {
        if (tamanho <= 0f) return;
        Bounds limites = Limites(objeto);
        float medida = porLargura ? Mathf.Max(limites.size.x, limites.size.z) : limites.size.y;
        if (medida > 0.0001f)
            objeto.transform.localScale *= tamanho / medida;
    }

    /// <summary>Sobe ou desce o objeto para a parte mais baixa dele encostar no Y do chão.</summary>
    private static void ApoiarNoChao(GameObject objeto, float chao)
    {
        Bounds limites = Limites(objeto);
        if (limites.size == Vector3.zero) return;
        objeto.transform.position += Vector3.up * (chao - limites.min.y);
    }

    /// <summary>Caixa que envolve todos os renderers (sem partículas).</summary>
    private static Bounds Limites(GameObject objeto)
    {
        Bounds limites = new Bounds(objeto.transform.position, Vector3.zero);
        bool primeiro = true;
        foreach (Renderer r in objeto.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (primeiro) { limites = r.bounds; primeiro = false; }
            else limites.Encapsulate(r.bounds);
        }
        return limites;
    }

    /// <summary>Altura do terreno no ponto (0 se não houver terreno).</summary>
    private static float AlturaChao(float x, float z)
    {
        Terrain terreno = Terrain.activeTerrain;
        if (terreno == null) return 0f;
        return terreno.SampleHeight(new Vector3(x, 0f, z)) + terreno.transform.position.y;
    }

    private static Transform NovoFilho(Transform pai, string nome)
    {
        GameObject filho = new GameObject(nome);
        filho.transform.SetParent(pai, false);
        return filho.transform;
    }

    private static Transform NovoPonto(Transform pai, string nome, float x, float z, float giroY)
    {
        Transform ponto = NovoFilho(pai, nome);
        ponto.position = new Vector3(x, AlturaChao(x, z), z);
        ponto.rotation = Quaternion.Euler(0f, giroY, 0f);
        return ponto;
    }

    private static AudioClip Audio(string arquivo)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO + arquivo);
        if (clip == null) Debug.LogWarning("[Fauna e Flora] Não achei o som " + arquivo);
        return clip;
    }
}
