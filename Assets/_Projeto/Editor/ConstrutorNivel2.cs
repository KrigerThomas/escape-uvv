using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// FERRAMENTA DE EDITOR (não vai para o jogo): monta o cenário do Nível 2 por etapas.
/// Foi usada para construir a primeira versão da cena "Nivel2" (terreno, labirinto,
/// postes solares, praça, horta, céu noturno, vagalumes e pós-processamento).
///
/// Cada etapa apaga e recria só o grupo dela na cena (ex.: "Cenario_Parque"), então
/// mudanças feitas à mão DENTRO desses grupos se perdem se a etapa for rodada de novo.
/// Os menus ficam em "Escape UVV > Nível 2 (construtor)" e pedem confirmação antes.
/// No dia a dia, edite a cena normalmente; use isto só para refazer uma parte do zero.
/// </summary>
public static class ConstrutorNivel2
{
    // ------------------------------------------------------------------
    //  Caminhos e medidas usados por todas as etapas
    // ------------------------------------------------------------------
    private const string PASTA_PATIO = "Assets/_Projeto/Materiais/Patio/";
    private const string PASTA_TERRENO = "Assets/_Projeto/Terreno/";
    private const string PASTA_POLYGON = "Assets/POLYGON city pack/Prefabs/";
    private const string PASTA_ESCOLA = "Assets/school/Prefabs/props/";

    /// <summary>Canto do terreno (o terreno vai de -120 a 120 em X e de -150 a 90 em Z).</summary>
    private static readonly Vector3 POSICAO_TERRENO = new Vector3(-120f, -8f, -150f);
    private static readonly Vector3 TAMANHO_TERRENO = new Vector3(240f, 50f, 240f);
    private const int RESOLUCAO_ALTURA = 257;
    private const int RESOLUCAO_TEXTURA = 256;
    private const int RESOLUCAO_DETALHE = 512;

    /// <summary>Lago: centro e raio médio.</summary>
    private static readonly Vector2 CENTRO_LAGO = new Vector2(38f, -36f);
    private const float NIVEL_AGUA = -0.35f;

    /// <summary>Nomes das 10 camadas de textura do terreno, na ordem em que são pintadas.</summary>
    public static readonly string[] CAMADAS =
    {
        "01_Grama", "02_Grama_Musgo", "03_Grama_Terra", "04_Lama", "05_Cascalho",
        "06_Areia", "07_Rocha", "08_Folhas_Secas", "09_Seixos_Rio", "10_Pedras_Calcamento"
    };

    /// <summary>Código do ambientCG que substitui cada camada provisória (mesma ordem de CAMADAS).</summary>
    public static readonly string[] CODIGOS_AMBIENTCG =
    {
        "Grass004", "Ground037", "Ground003", "Ground106", "Gravel043",
        "Ground080", "Rock058", "ScatteredLeaves009", "Ground108", "PavingStones138"
    };

    private const int GRAMA = 0, MUSGO = 1, GRAMA_TERRA = 2, LAMA = 3, CASCALHO = 4,
                      AREIA = 5, ROCHA = 6, FOLHAS = 7, SEIXOS = 8, CALCAMENTO = 9;

    // ------------------------------------------------------------------
    //  Menus
    // ------------------------------------------------------------------

    /// <summary>Roda todas as etapas em ordem, depois de confirmar com quem clicou.</summary>
    [MenuItem("Escape UVV/Nível 2 (construtor)/Reconstruir tudo")]
    public static void MenuReconstruirTudo()
    {
        if (!Confirmar("todo o cenário do Nível 2"))
            return;
        ConstruirTudo();
    }

    /// <summary>Refaz só as texturas do terreno (use depois de baixar as texturas do ambientCG).</summary>
    [MenuItem("Escape UVV/Nível 2 (construtor)/Atualizar texturas do terreno")]
    public static void MenuAtualizarTexturas()
    {
        int trocadas = AtualizarTexturasDoTerreno();
        EditorUtility.DisplayDialog("Texturas do terreno", trocadas + " de 10 camadas usam as texturas baixadas.", "OK");
    }

    /// <summary>Executa todas as etapas da construção.</summary>
    public static void ConstruirTudo()
    {
        PrepararAssets();
        ConstruirTerreno();
        ConstruirLabirinto();
        MontarParque();
        MontarAtmosfera();
        MontarGameplay();
        Debug.Log("ConstrutorNivel2: cenário completo montado.");
    }

    /// <summary>Mostra uma janela pedindo confirmação.</summary>
    /// <param name="oque">Descrição do que será refeito.</param>
    /// <returns>Verdadeiro se a pessoa confirmou.</returns>
    private static bool Confirmar(string oque)
    {
        return EditorUtility.DisplayDialog("Construtor do Nível 2",
            "Isso apaga e refaz " + oque + ". Mudanças feitas à mão nesses objetos serão perdidas. Continuar?",
            "Continuar", "Cancelar");
    }

    // ------------------------------------------------------------------
    //  Etapa 1: assets (materiais, árvores do terreno, camadas de textura)
    // ------------------------------------------------------------------

    /// <summary>
    /// Cria (ou atualiza) os assets que o cenário usa: árvores prontas para o terreno,
    /// as 10 camadas de textura e o material do terreno.
    /// Os materiais (sebe, água, lâmpadas, lixeiras, partículas e céu) já ficam em Materiais/Patio.
    /// </summary>
    public static void PrepararAssets()
    {
        CriarPasta(PASTA_TERRENO + "Arvores");
        CriarPasta(PASTA_TERRENO + "Camadas");
        CriarMateriais();

        CriarArvoreParaTerreno(PASTA_POLYGON + "Props/Tree prefab.prefab", "Arvore_Parque");
        CriarArvoreParaTerreno(PASTA_POLYGON + "Props/Bush 2 prefab.prefab", "Arbusto_Parque");

        for (int i = 0; i < CAMADAS.Length; i++)
        {
            string caminho = PASTA_TERRENO + "Camadas/Camada_" + CAMADAS[i] + ".terrainlayer";
            TerrainLayer camada = AssetDatabase.LoadAssetAtPath<TerrainLayer>(caminho);
            if (camada == null)
            {
                camada = new TerrainLayer();
                AssetDatabase.CreateAsset(camada, caminho);
            }
            camada.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PASTA_TERRENO + "Provisorias/Provisoria_" + CAMADAS[i] + ".png");
            camada.tileSize = new Vector2(5f, 5f);
            camada.smoothness = 0f;
            camada.metallic = 0f;
            EditorUtility.SetDirty(camada);
        }

        Material terreno = CarregarOuCriarMaterial(PASTA_TERRENO + "Terreno_Lit.mat", Shader.Find("Universal Render Pipeline/Terrain/Lit"));
        EditorUtility.SetDirty(terreno);
        AssetDatabase.SaveAssets();

        AtualizarTexturasDoTerreno();
    }

    /// <summary>
    /// Ajusta a importação das texturas feitas para o pátio e cria os materiais:
    /// sebe, água do lago, painel solar, lâmpadas, lixeiras coloridas, partículas e céu noturno.
    /// </summary>
    private static void CriarMateriais()
    {
        ConfigurarTextura(PASTA_PATIO + "Tex_Sebe_Folhas.png", t => t.anisoLevel = 4);
        ConfigurarTextura(PASTA_PATIO + "Tex_Sebe_Folhas_Normal.png", t => { t.textureType = TextureImporterType.NormalMap; t.anisoLevel = 4; });
        ConfigurarTextura(PASTA_PATIO + "Tex_Agua_Normal.png", t => t.textureType = TextureImporterType.NormalMap);
        ConfigurarTextura(PASTA_PATIO + "Tex_Brilho_Suave.png", t => { t.alphaIsTransparency = true; t.wrapMode = TextureWrapMode.Clamp; });
        ConfigurarTextura("Assets/_Projeto/Materiais/Ceu/Tex_Ceu_Noite.png", t =>
        {
            t.mipmapEnabled = false;
            t.wrapModeU = TextureWrapMode.Repeat;
            t.wrapModeV = TextureWrapMode.Clamp;
            t.textureCompression = TextureImporterCompression.CompressedHQ;
        });
        ConfigurarTextura("Assets/POLYGON city pack/Meshes,textures/props_FBX/grass 2.png", t => { t.alphaIsTransparency = true; t.wrapMode = TextureWrapMode.Clamp; });

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Shader particula = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        Material sebe = CarregarOuCriarMaterial(PASTA_PATIO + "Patio_Sebe.mat", lit);
        sebe.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PASTA_PATIO + "Tex_Sebe_Folhas.png"));
        sebe.SetColor("_BaseColor", new Color(0.9f, 1f, 0.9f));
        sebe.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PASTA_PATIO + "Tex_Sebe_Folhas_Normal.png"));
        sebe.SetFloat("_BumpScale", 1.2f);
        sebe.SetFloat("_Smoothness", 0.12f);
        AjustarPalavrasChave(sebe);

        Material agua = CarregarOuCriarMaterial(PASTA_PATIO + "Patio_Agua_Lago.mat", lit);
        agua.SetFloat("_Surface", 1f);
        agua.SetFloat("_Blend", 0f);
        agua.SetColor("_BaseColor", new Color(0.02f, 0.05f, 0.08f, 0.88f));
        agua.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PASTA_PATIO + "Tex_Agua_Normal.png"));
        agua.SetFloat("_BumpScale", 0.35f);
        agua.SetFloat("_Smoothness", 0.96f);
        agua.SetTextureScale("_BaseMap", new Vector2(8f, 8f));
        AjustarPalavrasChave(agua);

        Material painel = CarregarOuCriarMaterial(PASTA_PATIO + "Patio_Painel_Solar.mat", lit);
        painel.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PASTA_PATIO + "Tex_Painel_Solar.png"));
        painel.SetFloat("_Smoothness", 0.85f);
        painel.SetFloat("_Metallic", 0.4f);
        AjustarPalavrasChave(painel);

        CriarMaterialQueBrilha("Patio_Lampada_Acesa", lit, new Color(1f, 0.9f, 0.7f), new Color(1f, 0.78f, 0.5f) * 6f);
        CriarMaterialQueBrilha("Patio_Lampada_Saida", lit, new Color(0.6f, 1f, 0.7f), new Color(0.3f, 1f, 0.45f) * 5f);

        Material placa = CarregarOuCriarMaterial(PASTA_PATIO + "Patio_Placa_Verde.mat", lit);
        placa.SetColor("_BaseColor", new Color(0.07f, 0.24f, 0.12f));
        placa.SetFloat("_Smoothness", 0.35f);
        AjustarPalavrasChave(placa);

        string[] lixeiras = { "Papel", "Plastico", "Vidro", "Metal", "Organico" };
        Color[] cores = { new Color(0.08f, 0.3f, 0.85f), new Color(0.85f, 0.1f, 0.1f), new Color(0.1f, 0.6f, 0.2f), new Color(0.95f, 0.8f, 0.1f), new Color(0.45f, 0.28f, 0.12f) };
        for (int i = 0; i < lixeiras.Length; i++)
        {
            Material m = CarregarOuCriarMaterial(PASTA_PATIO + "Patio_Lixeira_" + lixeiras[i] + ".mat", lit);
            m.SetColor("_BaseColor", cores[i]);
            m.SetFloat("_Smoothness", 0.45f);
            AjustarPalavrasChave(m);
        }

        Texture2D brilho = AssetDatabase.LoadAssetAtPath<Texture2D>(PASTA_PATIO + "Tex_Brilho_Suave.png");
        foreach (var par in new[] { ("Patio_Vagalume", new Color(0.75f, 1f, 0.25f) * 2.5f), ("Patio_Brilho_Segredo", new Color(1f, 0.85f, 0.4f) * 2f) })
        {
            Material m = CarregarOuCriarMaterial(PASTA_PATIO + par.Item1 + ".mat", particula);
            m.SetTexture("_BaseMap", brilho);
            m.SetColor("_BaseColor", par.Item2);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f); // aditivo: a cor soma com o fundo e parece brilhar
            AjustarPalavrasChave(m);
        }

        Material ceu = CarregarOuCriarMaterial("Assets/_Projeto/Materiais/Ceu/Ceu_Noite.mat", Shader.Find("Skybox/Panoramic"));
        ceu.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Projeto/Materiais/Ceu/Tex_Ceu_Noite.png"));
        ceu.SetFloat("_Mapping", 1f);
        ceu.SetFloat("_ImageType", 0f);
        ceu.SetFloat("_Exposure", 1f);
        EditorUtility.SetDirty(ceu);

        AssetDatabase.SaveAssets();
    }

    /// <summary>Cria um material Lit que brilha (Emission), usado nas lâmpadas.</summary>
    /// <param name="nome">Nome do material.</param>
    /// <param name="lit">Shader Lit do URP.</param>
    /// <param name="cor">Cor base.</param>
    /// <param name="emissao">Cor do brilho (valores acima de 1 deixam o brilho mais forte com o Bloom).</param>
    private static void CriarMaterialQueBrilha(string nome, Shader lit, Color cor, Color emissao)
    {
        Material m = CarregarOuCriarMaterial(PASTA_PATIO + nome + ".mat", lit);
        m.SetColor("_BaseColor", cor);
        m.SetColor("_EmissionColor", emissao);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.EnableKeyword("_EMISSION");
        AjustarPalavrasChave(m);
        m.EnableKeyword("_EMISSION");
    }

    /// <summary>Muda as configurações de importação de uma textura e reimporta.</summary>
    /// <param name="caminho">Caminho da textura.</param>
    /// <param name="ajuste">O que mudar no importador.</param>
    private static void ConfigurarTextura(string caminho, System.Action<TextureImporter> ajuste)
    {
        TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
        if (importador == null)
        {
            Debug.LogWarning("ConstrutorNivel2: textura não encontrada: " + caminho);
            return;
        }
        ajuste(importador);
        importador.SaveAndReimport();
    }

    /// <summary>
    /// Faz o mesmo que o Inspector faz ao mudar um material do URP: liga e desliga as
    /// palavras-chave do shader (transparência, mapa normal, brilho...) de acordo com as propriedades.
    /// </summary>
    /// <param name="material">Material a ajustar.</param>
    private static void AjustarPalavrasChave(Material material)
    {
        foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type tipo = assembly.GetType("UnityEditor.BaseShaderGUI");
            if (tipo == null)
                continue;
            tipo.GetMethod("SetMaterialKeywords").Invoke(null, new object[] { material, null, null });
            break;
        }
        EditorUtility.SetDirty(material);
    }

    /// <summary>
    /// Procura, na pasta Terreno/Texturas, as texturas baixadas do ambientCG
    /// (arquivos que começam com o código, ex.: Grass004_1K-JPG_Color.jpg)
    /// e coloca cada uma na camada certa. Camadas sem textura baixada continuam com a provisória.
    /// </summary>
    /// <returns>Quantas camadas passaram a usar a textura baixada.</returns>
    public static int AtualizarTexturasDoTerreno()
    {
        int trocadas = 0;
        for (int i = 0; i < CAMADAS.Length; i++)
        {
            TerrainLayer camada = AssetDatabase.LoadAssetAtPath<TerrainLayer>(PASTA_TERRENO + "Camadas/Camada_" + CAMADAS[i] + ".terrainlayer");
            if (camada == null)
                continue;

            Texture2D cor = ProcurarTextura(CODIGOS_AMBIENTCG[i], "_Color");
            if (cor == null)
                continue;

            // O ambientCG usa "_NormalGL" (padrão OpenGL, o que a Unity espera);
            // alguns pacotes vêm só com "_Normal", então tenta esse como segunda opção.
            Texture2D normal = ProcurarTextura(CODIGOS_AMBIENTCG[i], "_NormalGL");
            if (normal == null)
                normal = ProcurarTextura(CODIGOS_AMBIENTCG[i], "_Normal");
            if (normal != null)
            {
                TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal));
                if (importador.textureType != TextureImporterType.NormalMap)
                {
                    importador.textureType = TextureImporterType.NormalMap;
                    importador.SaveAndReimport();
                }
            }

            camada.diffuseTexture = cor;
            camada.normalMapTexture = normal;
            camada.normalScale = 1f;
            camada.tileSize = i == CALCAMENTO ? new Vector2(3f, 3f) : new Vector2(4f, 4f);
            EditorUtility.SetDirty(camada);
            trocadas++;
        }
        AssetDatabase.SaveAssets();
        return trocadas;
    }

    /// <summary>Procura uma textura na pasta Terreno/Texturas pelo código e pelo tipo de mapa.</summary>
    /// <param name="codigo">Código do ambientCG, ex.: Grass004.</param>
    /// <param name="sufixo">Parte do nome do arquivo que indica o mapa, ex.: _Color ou _NormalGL.</param>
    /// <returns>A textura encontrada ou null.</returns>
    private static Texture2D ProcurarTextura(string codigo, string sufixo)
    {
        if (!AssetDatabase.IsValidFolder(PASTA_TERRENO + "Texturas"))
            return null;

        foreach (string guid in AssetDatabase.FindAssets(codigo + " t:Texture2D", new[] { PASTA_TERRENO + "Texturas" }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            string nome = System.IO.Path.GetFileNameWithoutExtension(caminho);
            if (nome.StartsWith(codigo) && nome.Contains(sufixo))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
        }
        return null;
    }

    /// <summary>
    /// O terreno da Unity só aceita árvores cuja malha está no objeto raiz.
    /// Os modelos do POLYGON city pack têm a malha num filho girado e escalado,
    /// então esta função "assa" esse giro e essa escala numa malha nova e salva um prefab simples.
    /// </summary>
    /// <param name="caminhoOriginal">Prefab original do pack.</param>
    /// <param name="nome">Nome do prefab novo.</param>
    private static void CriarArvoreParaTerreno(string caminhoOriginal, string nome)
    {
        GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoOriginal);
        MeshFilter filtro = original.GetComponentInChildren<MeshFilter>();
        MeshRenderer renderizador = original.GetComponentInChildren<MeshRenderer>();
        Matrix4x4 matriz = original.transform.worldToLocalMatrix * filtro.transform.localToWorldMatrix;

        Mesh fonte = filtro.sharedMesh;
        Mesh malha = new Mesh { name = nome + "_Malha" };
        Vector3[] vertices = fonte.vertices;
        Vector3[] normais = fonte.normals;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = matriz.MultiplyPoint3x4(vertices[i]);
            if (normais.Length == vertices.Length)
                normais[i] = matriz.MultiplyVector(normais[i]).normalized;
        }
        malha.vertices = vertices;
        malha.normals = normais;
        malha.uv = fonte.uv;
        malha.subMeshCount = fonte.subMeshCount;
        for (int s = 0; s < fonte.subMeshCount; s++)
            malha.SetTriangles(fonte.GetTriangles(s), s);
        malha.RecalculateBounds();
        malha.RecalculateTangents();

        string caminhoMalha = PASTA_TERRENO + "Arvores/" + nome + "_Malha.asset";
        AssetDatabase.DeleteAsset(caminhoMalha);
        AssetDatabase.CreateAsset(malha, caminhoMalha);

        GameObject temporario = new GameObject(nome);
        temporario.AddComponent<MeshFilter>().sharedMesh = malha;
        MeshRenderer novoRenderizador = temporario.AddComponent<MeshRenderer>();
        novoRenderizador.sharedMaterials = renderizador.sharedMaterials;
        // Com um LODGroup o terreno aceita a árvore com os materiais do URP (sem o aviso de shader "Nature").
        LODGroup lod = temporario.AddComponent<LODGroup>();
        lod.SetLODs(new[] { new LOD(0.015f, new Renderer[] { novoRenderizador }) });
        foreach (Material m in renderizador.sharedMaterials)
            if (m != null && !m.enableInstancing)
            {
                m.enableInstancing = true; // desenha muitas árvores iguais de uma vez (mais leve)
                EditorUtility.SetDirty(m);
            }
        PrefabUtility.SaveAsPrefabAsset(temporario, PASTA_TERRENO + "Arvores/" + nome + ".prefab");
        Object.DestroyImmediate(temporario);
    }

    // ------------------------------------------------------------------
    //  Etapa 2: terreno (relevo, texturas, árvores e grama)
    // ------------------------------------------------------------------

    /// <summary>
    /// Cria o terreno do pátio: área plana do campus no meio, morros em volta,
    /// um lago a leste, uma ravina a oeste, 10 camadas de textura pintadas por regras,
    /// árvores nos morros e grama nas áreas verdes.
    /// </summary>
    public static void ConstruirTerreno()
    {
        ApagarRaiz("Cenario_Terreno");
        GameObject grupo = new GameObject("Cenario_Terreno");

        string caminho = PASTA_TERRENO + "Nivel2_Terreno.asset";
        AssetDatabase.DeleteAsset(caminho);

        TerrainData dados = new TerrainData();
        dados.heightmapResolution = RESOLUCAO_ALTURA;
        dados.size = TAMANHO_TERRENO;
        dados.alphamapResolution = RESOLUCAO_TEXTURA;
        dados.baseMapResolution = 512;
        dados.SetDetailResolution(RESOLUCAO_DETALHE, 32);
        AssetDatabase.CreateAsset(dados, caminho);

        // Relevo
        float[,] alturas = new float[RESOLUCAO_ALTURA, RESOLUCAO_ALTURA];
        for (int iz = 0; iz < RESOLUCAO_ALTURA; iz++)
            for (int ix = 0; ix < RESOLUCAO_ALTURA; ix++)
            {
                Vector2 p = PontoDoMundo(ix, iz, RESOLUCAO_ALTURA);
                alturas[iz, ix] = Mathf.Clamp01((AlturaNoMundo(p.x, p.y) - POSICAO_TERRENO.y) / TAMANHO_TERRENO.y);
            }
        dados.SetHeights(0, 0, alturas);

        // Camadas de textura
        TerrainLayer[] camadas = new TerrainLayer[CAMADAS.Length];
        for (int i = 0; i < CAMADAS.Length; i++)
            camadas[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(PASTA_TERRENO + "Camadas/Camada_" + CAMADAS[i] + ".terrainlayer");
        dados.terrainLayers = camadas;

        // Árvores (antes da pintura, para pintar folhas secas embaixo delas)
        List<Vector2> posicoesArvores = PlantarArvores(dados);
        PintarTexturas(dados, posicoesArvores);
        PlantarGrama(dados);

        GameObject objetoTerreno = Terrain.CreateTerrainGameObject(dados);
        objetoTerreno.name = "Terreno";
        objetoTerreno.tag = "Chao";
        objetoTerreno.transform.SetParent(grupo.transform);
        objetoTerreno.transform.position = POSICAO_TERRENO;

        Terrain terreno = objetoTerreno.GetComponent<Terrain>();
        terreno.materialTemplate = AssetDatabase.LoadAssetAtPath<Material>(PASTA_TERRENO + "Terreno_Lit.mat");
        terreno.heightmapPixelError = 4f;
        terreno.basemapDistance = 160f;
        terreno.treeDistance = 400f;
        terreno.treeBillboardDistance = 400f;
        terreno.detailObjectDistance = 70f;
        terreno.detailObjectDensity = 1f;
        terreno.drawInstanced = true;

        CriarLago(grupo.transform);

        EditorUtility.SetDirty(dados);
        AssetDatabase.SaveAssets();
    }

    /// <summary>Converte o índice de um ponto da grade do terreno para X e Z do mundo.</summary>
    /// <param name="ix">Índice no eixo X.</param>
    /// <param name="iz">Índice no eixo Z.</param>
    /// <param name="resolucao">Quantos pontos a grade tem de cada lado.</param>
    /// <returns>Vetor com (X, Z) no mundo.</returns>
    private static Vector2 PontoDoMundo(int ix, int iz, int resolucao)
    {
        float x = POSICAO_TERRENO.x + (float)ix / (resolucao - 1) * TAMANHO_TERRENO.x;
        float z = POSICAO_TERRENO.z + (float)iz / (resolucao - 1) * TAMANHO_TERRENO.z;
        return new Vector2(x, z);
    }

    /// <summary>Transição suave de 0 a 1 (curva em S).</summary>
    /// <param name="a">Início da transição.</param>
    /// <param name="b">Fim da transição.</param>
    /// <param name="v">Valor avaliado.</param>
    /// <returns>0 antes de "a", 1 depois de "b" e uma curva suave no meio.</returns>
    private static float Suave(float a, float b, float v)
    {
        float t = Mathf.Clamp01((v - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    /// <summary>Ruído de Perlin em várias escalas (fBm), usado para o relevo e as manchas de textura.</summary>
    /// <param name="x">Coordenada X.</param>
    /// <param name="z">Coordenada Z.</param>
    /// <param name="oitavas">Quantas camadas de ruído somar.</param>
    /// <returns>Valor entre 0 e 1.</returns>
    private static float Ruido(float x, float z, int oitavas)
    {
        float soma = 0f, amplitude = 1f, total = 0f, frequencia = 1f;
        for (int i = 0; i < oitavas; i++)
        {
            soma += Mathf.PerlinNoise(x * frequencia + 31.7f * i, z * frequencia + 17.3f * i) * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
            frequencia *= 2f;
        }
        return soma / total;
    }

    /// <summary>Distância de um ponto até um retângulo (0 se o ponto estiver dentro).</summary>
    /// <param name="x">X do ponto.</param>
    /// <param name="z">Z do ponto.</param>
    /// <param name="minX">Menor X do retângulo.</param>
    /// <param name="maxX">Maior X do retângulo.</param>
    /// <param name="minZ">Menor Z do retângulo.</param>
    /// <param name="maxZ">Maior Z do retângulo.</param>
    /// <returns>Distância em metros.</returns>
    private static float DistanciaRetangulo(float x, float z, float minX, float maxX, float minZ, float maxZ)
    {
        float dx = Mathf.Max(minX - x, 0f, x - maxX);
        float dz = Mathf.Max(minZ - z, 0f, z - maxZ);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>X do centro da ravina para um dado Z (ela faz uma curva leve).</summary>
    /// <param name="z">Coordenada Z.</param>
    /// <returns>Coordenada X do fundo da ravina.</returns>
    private static float CentroRavina(float z)
    {
        return -33f + 2.5f * Mathf.Sin(z * 0.045f);
    }

    /// <summary>Raio do lago numa direção (a borda não é um círculo perfeito).</summary>
    /// <param name="angulo">Ângulo em radianos em volta do centro do lago.</param>
    /// <returns>Raio em metros.</returns>
    private static float RaioLago(float angulo)
    {
        return 11f + 1.5f * Mathf.Sin(3f * angulo) + 0.8f * Mathf.Cos(5f * angulo + 1f);
    }

    /// <summary>
    /// Altura do chão (Y do mundo) em qualquer ponto. É aqui que o relevo é "modelado":
    /// área do campus plana (Y = 0), morros fora do vale, ravina e lago escavados.
    /// </summary>
    /// <param name="x">Coordenada X do mundo.</param>
    /// <param name="z">Coordenada Z do mundo.</param>
    /// <returns>Altura em metros.</returns>
    public static float AlturaNoMundo(float x, float z)
    {
        float distanciaCampus = DistanciaRetangulo(x, z, -27f, 27f, -70f, 42f);
        float distanciaVale = DistanciaRetangulo(x, z, -62f, 64f, -92f, 58f);

        float y = 0f;
        // ondulações leves no vale, fora do campus
        y += (Ruido(x * 0.04f, z * 0.04f, 3) - 0.45f) * 1.6f * Suave(0f, 10f, distanciaCampus);
        // morros em volta do vale
        y += Suave(0f, 50f, distanciaVale) * (6f + 18f * Ruido(x * 0.012f, z * 0.012f, 4));
        y += Suave(0f, 15f, distanciaVale) * (Ruido(x * 0.08f, z * 0.08f, 3) - 0.3f) * 2f;

        // ravina (oeste): um corte em V suavizado
        float distanciaRavina = Mathf.Abs(x - CentroRavina(z));
        float profundidade = 4.5f * (1f - Suave(1.5f, 7f, distanciaRavina));
        y -= profundidade * Suave(0f, 4f, distanciaCampus);

        // lago (leste): bacia com margem rasa
        Vector2 relativo = new Vector2(x, z) - CENTRO_LAGO;
        float raio = RaioLago(Mathf.Atan2(relativo.y, relativo.x));
        float distanciaLago = relativo.magnitude;
        if (distanciaLago < raio + 4f)
        {
            float margem = Mathf.Lerp(-0.7f, y, Suave(raio, raio + 4f, distanciaLago));
            float fundo = -0.7f - 2.6f * Suave(raio, raio * 0.25f, distanciaLago);
            y = distanciaLago < raio ? fundo : margem;
        }

        return y;
    }

    /// <summary>Distribui árvores e arbustos nos morros e nas bordas do vale (nunca no campus, no lago ou na ravina).</summary>
    /// <param name="dados">Dados do terreno.</param>
    /// <returns>Posições (X, Z) das árvores, usadas para pintar folhas secas.</returns>
    private static List<Vector2> PlantarArvores(TerrainData dados)
    {
        dados.treePrototypes = new[]
        {
            new TreePrototype { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PASTA_TERRENO + "Arvores/Arvore_Parque.prefab") },
            new TreePrototype { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PASTA_TERRENO + "Arvores/Arbusto_Parque.prefab") }
        };

        System.Random sorteio = new System.Random(2026);
        List<TreeInstance> instancias = new List<TreeInstance>();
        List<Vector2> posicoes = new List<Vector2>();

        for (int tentativa = 0; tentativa < 12000 && instancias.Count < 1100; tentativa++)
        {
            float x = POSICAO_TERRENO.x + 4f + (float)sorteio.NextDouble() * (TAMANHO_TERRENO.x - 8f);
            float z = POSICAO_TERRENO.z + 4f + (float)sorteio.NextDouble() * (TAMANHO_TERRENO.z - 8f);

            if (DistanciaRetangulo(x, z, -27f, 27f, -70f, 42f) < 4f) continue;            // campus
            if (Mathf.Abs(x - CentroRavina(z)) < 8f) continue;                              // ravina
            Vector2 rel = new Vector2(x, z) - CENTRO_LAGO;
            if (rel.magnitude < RaioLago(Mathf.Atan2(rel.y, rel.x)) + 4f) continue;        // lago

            // mais árvores perto das bordas do vale e nos morros
            float chance = 0.35f + 0.65f * Suave(0f, 20f, DistanciaRetangulo(x, z, -40f, 40f, -80f, 50f));
            if (sorteio.NextDouble() > chance) continue;

            bool arbusto = sorteio.NextDouble() < 0.28;
            float escala = arbusto ? 1.4f + (float)sorteio.NextDouble() * 1.4f : 2.0f + (float)sorteio.NextDouble() * 1.8f;
            byte tom = (byte)(200 + sorteio.Next(55));

            instancias.Add(new TreeInstance
            {
                prototypeIndex = arbusto ? 1 : 0,
                position = new Vector3((x - POSICAO_TERRENO.x) / TAMANHO_TERRENO.x, 0f, (z - POSICAO_TERRENO.z) / TAMANHO_TERRENO.z),
                widthScale = escala,
                heightScale = escala * (0.9f + (float)sorteio.NextDouble() * 0.25f),
                rotation = (float)sorteio.NextDouble() * Mathf.PI * 2f,
                color = new Color32(tom, tom, tom, 255),
                lightmapColor = new Color32(255, 255, 255, 255)
            });
            if (!arbusto)
                posicoes.Add(new Vector2(x, z));
        }

        dados.SetTreeInstances(instancias.ToArray(), true);
        return posicoes;
    }

    /// <summary>
    /// Pinta as 10 camadas por regras: grama por padrão, musgo nos morros, rocha onde é íngreme,
    /// areia e lama no lago, seixos no fundo da ravina, cascalho e pedras nos caminhos
    /// e folhas secas embaixo das árvores.
    /// </summary>
    /// <param name="dados">Dados do terreno (o relevo já precisa estar pronto).</param>
    /// <param name="arvores">Posições das árvores.</param>
    private static void PintarTexturas(TerrainData dados, List<Vector2> arvores)
    {
        int res = RESOLUCAO_TEXTURA;
        float[,,] mapa = new float[res, res, CAMADAS.Length];
        float[] p = new float[CAMADAS.Length];

        // folhas secas: marca quais pixels ficam perto de alguma árvore
        float[,] folhas = new float[res, res];
        float tamanhoPixel = TAMANHO_TERRENO.x / (res - 1);
        foreach (Vector2 a in arvores)
        {
            int cx = Mathf.RoundToInt((a.x - POSICAO_TERRENO.x) / tamanhoPixel);
            int cz = Mathf.RoundToInt((a.y - POSICAO_TERRENO.z) / tamanhoPixel);
            for (int dz = -4; dz <= 4; dz++)
                for (int dx = -4; dx <= 4; dx++)
                {
                    int px = cx + dx, pz = cz + dz;
                    if (px < 0 || pz < 0 || px >= res || pz >= res) continue;
                    float d = Mathf.Sqrt(dx * dx + dz * dz) * tamanhoPixel;
                    folhas[pz, px] = Mathf.Max(folhas[pz, px], 1f - Suave(1.5f, 3.8f, d));
                }
        }

        for (int iz = 0; iz < res; iz++)
            for (int ix = 0; ix < res; ix++)
            {
                Vector2 w = PontoDoMundo(ix, iz, res);
                float x = w.x, z = w.y;
                float nx = (float)ix / (res - 1), nz = (float)iz / (res - 1);
                float y = dados.GetInterpolatedHeight(nx, nz) + POSICAO_TERRENO.y;
                float inclinacao = dados.GetSteepness(nx, nz);
                float manchas = Ruido(x * 0.06f, z * 0.06f, 3);

                System.Array.Clear(p, 0, p.Length);
                p[GRAMA] = 1f;
                p[GRAMA_TERRA] = Suave(0.55f, 0.75f, manchas) * 0.9f;
                p[MUSGO] = Suave(1.5f, 6f, y) * (0.6f + 0.6f * manchas);
                p[FOLHAS] = folhas[iz, ix] * 1.2f;

                // ravina
                float dr = Mathf.Abs(x - CentroRavina(z));
                if (dr < 9f && DistanciaRetangulo(x, z, -27f, 27f, -70f, 42f) > 2f)
                {
                    p[SEIXOS] += 3f * (1f - Suave(1.2f, 2.6f, dr));
                    p[LAMA] += 1.2f * (1f - Suave(2f, 4f, dr)) * Suave(1f, 2.5f, dr);
                    p[FOLHAS] += 0.8f * Suave(5f, 6.5f, dr) * (1f - Suave(7f, 9f, dr));
                }

                // lago
                Vector2 rel = new Vector2(x, z) - CENTRO_LAGO;
                float raio = RaioLago(Mathf.Atan2(rel.y, rel.x));
                float dl = rel.magnitude;
                if (dl < raio + 6f)
                {
                    p[LAMA] += 3f * (1f - Suave(raio - 1f, raio + 0.5f, dl));
                    p[AREIA] += 3f * Suave(raio - 1.5f, raio, dl) * (1f - Suave(raio + 3.5f, raio + 6f, dl));
                }

                // encostas íngremes viram rocha
                p[ROCHA] += 4f * Suave(24f, 40f, inclinacao);

                // caminhos e pisos do campus
                bool labirinto = DistanciaRetangulo(x, z, -20.75f, 20.75f, -20.75f, 20.75f) < 0.01f;
                bool trilha = DistanciaRetangulo(x, z, -1.9f, 1.9f, -49f, -20.75f) < 0.01f;
                bool trilhaHorta = DistanciaRetangulo(x, z, 1.9f, 9.5f, -36.5f, -33.5f) < 0.01f;
                bool pracaInicio = new Vector2(x, z + 50f).magnitude < 5.5f;
                bool patioSaida = DistanciaRetangulo(x, z, -20.75f, 20.75f, 20.75f, 27.5f) < 0.01f;
                bool calcadaPredios = DistanciaRetangulo(x, z, -22f, 22f, -64f, -55f) < 0.01f || DistanciaRetangulo(x, z, -22f, 22f, 27.5f, 36f) < 0.01f;
                bool horta = DistanciaRetangulo(x, z, 9.5f, 18.5f, -42f, -28f) < 0.01f;

                if (labirinto || pracaInicio || calcadaPredios)
                    Zerar(p, CALCAMENTO, 1f);
                else if (trilha || trilhaHorta || patioSaida)
                    Zerar(p, CASCALHO, 1f);
                else if (horta)
                {
                    Zerar(p, GRAMA_TERRA, 1f);
                    p[LAMA] = 0.35f;
                }

                float soma = 0f;
                for (int i = 0; i < p.Length; i++) soma += p[i];
                for (int i = 0; i < p.Length; i++) mapa[iz, ix, i] = p[i] / soma;
            }

        dados.SetAlphamaps(0, 0, mapa);
    }

    /// <summary>Zera todas as camadas e deixa só uma com o peso indicado.</summary>
    /// <param name="pesos">Vetor de pesos das camadas.</param>
    /// <param name="camada">Índice da camada que fica.</param>
    /// <param name="valor">Peso da camada que fica.</param>
    private static void Zerar(float[] pesos, int camada, float valor)
    {
        System.Array.Clear(pesos, 0, pesos.Length);
        pesos[camada] = valor;
    }

    /// <summary>Cria a grama (gramíneas) do terreno nas áreas verdes perto do campus.</summary>
    /// <param name="dados">Dados do terreno (as texturas já precisam estar pintadas).</param>
    private static void PlantarGrama(TerrainData dados)
    {
        dados.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
        dados.detailPrototypes = new[]
        {
            new DetailPrototype
            {
                prototypeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/POLYGON city pack/Meshes,textures/props_FBX/grass 2.png"),
                renderMode = DetailRenderMode.GrassBillboard,
                usePrototypeMesh = false,
                minWidth = 0.5f, maxWidth = 0.9f,
                minHeight = 0.35f, maxHeight = 0.65f,
                noiseSpread = 0.3f,
                healthyColor = new Color(0.55f, 0.75f, 0.4f),
                dryColor = new Color(0.62f, 0.66f, 0.38f)
            }
        };

        int res = RESOLUCAO_DETALHE;
        int[,] grama = new int[res, res];
        float[,,] texturas = dados.GetAlphamaps(0, 0, RESOLUCAO_TEXTURA, RESOLUCAO_TEXTURA);
        System.Random sorteio = new System.Random(7);

        for (int iz = 0; iz < res; iz++)
            for (int ix = 0; ix < res; ix++)
            {
                Vector2 w = PontoDoMundo(ix, iz, res);
                if (DistanciaRetangulo(w.x, w.y, -45f, 45f, -80f, 50f) > 0.01f) continue;
                int tx = Mathf.Clamp(Mathf.RoundToInt((float)ix / (res - 1) * (RESOLUCAO_TEXTURA - 1)), 0, RESOLUCAO_TEXTURA - 1);
                int tz = Mathf.Clamp(Mathf.RoundToInt((float)iz / (res - 1) * (RESOLUCAO_TEXTURA - 1)), 0, RESOLUCAO_TEXTURA - 1);
                float verde = texturas[tz, tx, GRAMA] + texturas[tz, tx, MUSGO] + texturas[tz, tx, GRAMA_TERRA] * 0.5f;
                if (verde < 0.6f) continue;
                grama[iz, ix] = sorteio.NextDouble() < verde * 0.75f ? 1 + sorteio.Next(3) : 0;
            }

        dados.SetDetailLayer(0, 0, 0, grama);
    }

    /// <summary>Cria a superfície da água do lago (um plano com material de água que ondula).</summary>
    /// <param name="pai">Grupo do terreno.</param>
    private static void CriarLago(Transform pai)
    {
        GameObject agua = GameObject.CreatePrimitive(PrimitiveType.Plane);
        agua.name = "Agua_Lago";
        Object.DestroyImmediate(agua.GetComponent<Collider>());
        agua.transform.SetParent(pai);
        agua.transform.position = new Vector3(CENTRO_LAGO.x, NIVEL_AGUA, CENTRO_LAGO.y);
        agua.transform.localScale = new Vector3(3.2f, 1f, 3.2f);
        MeshRenderer r = agua.GetComponent<MeshRenderer>();
        r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PASTA_PATIO + "Patio_Agua_Lago.mat");
        r.shadowCastingMode = ShadowCastingMode.Off;
        agua.AddComponent<AguaOndulando>();
    }

    // ------------------------------------------------------------------
    //  Etapa 3: labirinto
    // ------------------------------------------------------------------

    /// <summary>Cria o objeto "Labirinto" com o GeradorLabirinto e constrói as sebes a partir do mapa padrão.</summary>
    public static void ConstruirLabirinto()
    {
        GameObject antigo = GameObject.Find("Labirinto");
        string mapaAtual = null;
        if (antigo != null)
        {
            GeradorLabirinto g = antigo.GetComponent<GeradorLabirinto>();
            if (g != null) mapaAtual = g.mapa;
            Object.DestroyImmediate(antigo);
        }

        GameObject labirinto = new GameObject("Labirinto");
        GeradorLabirinto gerador = labirinto.AddComponent<GeradorLabirinto>();
        if (!string.IsNullOrEmpty(mapaAtual)) gerador.mapa = mapaAtual;
        gerador.materialSebe = AssetDatabase.LoadAssetAtPath<Material>(PASTA_PATIO + "Patio_Sebe.mat");
        gerador.ConstruirSebes();
    }

    // ------------------------------------------------------------------
    //  Etapa 4: parque (postes solares, bancos, lixeiras, horta, placas, limites, prédios)
    // ------------------------------------------------------------------

    /// <summary>Monta todos os objetos de parque dentro e fora do labirinto.</summary>
    public static void MontarParque()
    {
        ApagarRaiz("Cenario_Parque");
        Transform parque = new GameObject("Cenario_Parque").transform;
        Transform postes = Grupo(parque, "Postes_Solares");
        Transform bancos = Grupo(parque, "Bancos");
        Transform lixeiras = Grupo(parque, "Lixeiras_Coleta_Seletiva");
        Transform horta = Grupo(parque, "Horta_e_Composteira");
        Transform plantas = Grupo(parque, "Plantas_e_Arvores");
        Transform placas = Grupo(parque, "Placas");
        Transform limites = Grupo(parque, "Limites");
        Transform predios = Grupo(parque, "Predios_Campus");

        GeradorLabirinto lab = Object.FindFirstObjectByType<GeradorLabirinto>();

        // --- Postes solares dentro do labirinto (nos cruzamentos, encostados num canto)
        int[,] cruzamentos = { { 1, 3 }, { 1, 11 }, { 1, 13 }, { 5, 7 }, { 5, 15 }, { 13, 5 }, { 13, 13 }, { 15, 3 }, { 15, 7 } };
        Vector2[] cantos = { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(-1, 1) };
        for (int i = 0; i < cruzamentos.GetLength(0); i++)
        {
            Vector3 c = lab.PosicaoNoMundo(cruzamentos[i, 0], cruzamentos[i, 1]);
            CriarPosteSolar(postes, c + new Vector3(cantos[i].x * 1.3f, 0f, cantos[i].y * 1.3f), false);
        }
        // poste com mau contato num beco sem saída
        CriarPosteSolar(postes, lab.PosicaoNoMundo(15, 15) + new Vector3(-1.3f, 0f, -1.3f), true);

        // --- Praça central (praça sustentável)
        CriarPosteSolar(postes, new Vector3(-5.3f, 0f, 5.3f), false);
        CriarPosteSolar(postes, new Vector3(5.3f, 0f, 5.3f), false);
        CriarConjuntoLixeiras(lixeiras, new Vector3(0f, 0f, 5.55f), Vector3.back);
        CriarBanco(bancos, new Vector3(-5.35f, 0f, 1.2f), Vector3.right);
        CriarBanco(bancos, new Vector3(5.35f, 0f, 1.2f), Vector3.left);
        Instanciar(PASTA_POLYGON + "Props/Flower mass prefab.prefab", plantas, new Vector3(-1.7f, 0f, 1.3f), 0f, 1f);
        Instanciar(PASTA_POLYGON + "Props/Pot_tree prefab.prefab", plantas, new Vector3(-2.3f, 0f, 3.3f), 30f, 1.6f);
        Instanciar(PASTA_POLYGON + "Props/Pot_tree prefab.prefab", plantas, new Vector3(2.3f, 0f, 3.3f), 80f, 1.6f);
        CriarPlaca(placas, new Vector3(0f, 0f, -2.4f), Vector3.back, "PRAÇA SUSTENTÁVEL",
            "Os postes do parque usam energia solar.\nSepare o lixo nas lixeiras coloridas.");

        // --- Entrada do labirinto
        Vector3 entrada = lab.PosicaoNoMundo(18, 9);
        CriarPosteSolar(postes, entrada + new Vector3(-2.6f, 0f, -1.6f), false);
        CriarPosteSolar(postes, entrada + new Vector3(2.6f, 0f, -1.6f), false);
        CriarPlaca(placas, entrada + new Vector3(-4.3f, 0f, -2.2f), Vector3.back, "LABIRINTO DO PÁTIO", "Encontre a saída do outro lado.");

        // --- Trilha de cascalho do início até a entrada
        for (int i = 0; i < 3; i++)
        {
            float z = -45f + i * 8.5f;
            CriarPosteSolar(postes, new Vector3(i % 2 == 0 ? -2.6f : 2.6f, 0f, z), false);
            CriarBanco(bancos, new Vector3(i % 2 == 0 ? 3.1f : -3.1f, 0f, z + 2f), i % 2 == 0 ? Vector3.left : Vector3.right);
        }
        CriarConjuntoLixeiras(lixeiras, new Vector3(-4.6f, 0f, -47.5f), Vector3.right);
        CriarPlaca(placas, new Vector3(4.2f, 0f, -47.2f), Vector3.left, "PÁTIO DA UVV", "Horta comunitária e composteira à direita.");
        for (int i = 0; i < 4; i++)
            Instanciar(PASTA_POLYGON + "Props/Flower mass prefab.prefab", plantas, new Vector3(-6.5f - (i % 2) * 2f, 0f, -42f + i * 5.5f), i * 70f, 1f);
        float[] zArbustos = { -46f, -41.5f, -27f, -23.6f };
        for (int i = 0; i < zArbustos.Length; i++)
            Instanciar(PASTA_POLYGON + "Props/Bush 2 mass prefab.prefab", plantas, new Vector3(5.2f, 0f, zArbustos[i]), i * 40f, 1f);
        float[,] arvores = { { -15f, -45f }, { -12f, -33f }, { -16.5f, -26f }, { -9f, -52f }, { 9.5f, -52f }, { 16f, -25.5f }, { -17f, -38f } };
        for (int i = 0; i < arvores.GetLength(0); i++)
            Instanciar(PASTA_POLYGON + "Props/Tree prefab.prefab", plantas, new Vector3(arvores[i, 0], 0f, arvores[i, 1]), i * 47f, 1.6f + (i % 3) * 0.2f);

        // --- Horta comunitária e composteira (lado leste do pátio)
        for (int linha = 0; linha < 2; linha++)
            for (int coluna = 0; coluna < 3; coluna++)
            {
                Vector3 pos = new Vector3(11.5f + coluna * 2.4f, 0f, -38.5f + linha * 4.2f);
                Instanciar(PASTA_POLYGON + "Props/wood pot prefab.prefab", horta, pos, 0f, 2f);
                string planta = (linha + coluna) % 2 == 0 ? "Props/Grass_prefab.prefab" : "Props/Flower 1 prefab C.prefab";
                Instanciar(PASTA_POLYGON + planta, horta, pos + new Vector3(0f, 0.72f, -0.5f), coluna * 33f, 1.1f);
                Instanciar(PASTA_POLYGON + planta, horta, pos + new Vector3(0f, 0.72f, 0.5f), coluna * 71f, 1.1f);
            }
        GameObject composteira = Instanciar(PASTA_POLYGON + "Props/wood pot prefab.prefab", horta, new Vector3(17.3f, 0f, -31.5f), 90f, 2.6f);
        composteira.name = "Composteira";
        composteira.transform.localScale = new Vector3(2.6f, 3.2f, 2.0f);
        CriarPosteSolar(postes, new Vector3(9.6f, 0f, -31f), false);
        Instanciar(PASTA_POLYGON + "Props/Bench 2 prefab.prefab", horta, new Vector3(14f, 0f, -30f), 90f, 1f);
        CriarPlaca(placas, new Vector3(10.2f, 0f, -40.8f), Vector3.left, "HORTA + COMPOSTEIRA",
            "Restos de comida viram adubo\npara a horta do campus.");

        // --- Pátio da saída (norte)
        Vector3 saida = lab.PosicaoNoMundo(0, 13);
        CriarPosteSolar(postes, saida + new Vector3(-2.6f, 0f, 2.2f), false);
        CriarPosteSolar(postes, saida + new Vector3(2.6f, 0f, 2.2f), false);
        CriarConjuntoLixeiras(lixeiras, new Vector3(-6f, 0f, 25.6f), Vector3.back);

        // --- Limites: muretas visíveis + paredes invisíveis
        for (float z = -56f; z < -21f; z += 2f)
        {
            Instanciar(PASTA_POLYGON + "Props/Wall 1 prefab.prefab", limites, new Vector3(-21.0f, 0f, z + 2f), 0f, 1f);
            Instanciar(PASTA_POLYGON + "Props/Wall 1 prefab.prefab", limites, new Vector3(21.4f, 0f, z + 2f), 0f, 1f);
        }
        for (float z = 21f; z < 28f; z += 2f)
        {
            Instanciar(PASTA_POLYGON + "Props/Wall 1 prefab.prefab", limites, new Vector3(-21.0f, 0f, z + 2f), 0f, 1f);
            Instanciar(PASTA_POLYGON + "Props/Wall 1 prefab.prefab", limites, new Vector3(21.4f, 0f, z + 2f), 0f, 1f);
        }
        ParedeInvisivel(limites, new Vector3(-21.3f, 3f, -38.5f), new Vector3(0.6f, 6f, 38f));
        ParedeInvisivel(limites, new Vector3(21.3f, 3f, -38.5f), new Vector3(0.6f, 6f, 38f));
        ParedeInvisivel(limites, new Vector3(0f, 3f, -57.5f), new Vector3(44f, 6f, 0.6f));
        ParedeInvisivel(limites, new Vector3(-21.3f, 3f, 24.5f), new Vector3(0.6f, 6f, 8f));
        ParedeInvisivel(limites, new Vector3(21.3f, 3f, 24.5f), new Vector3(0.6f, 6f, 8f));
        ParedeInvisivel(limites, new Vector3(0f, 3f, 28.3f), new Vector3(44f, 6f, 0.6f));

        // --- Prédios do campus (fundo do cenário)
        CriarFileiraPredios(predios, "Bloco_InovaLab", -57f, true, "INOVALAB");
        // porta do InovaLab logo atrás do jogador (de onde ele saiu no fim do Nível 1)
        GameObject porta = Instanciar(PASTA_ESCOLA + "a door.prefab", predios, new Vector3(0.04f, 0f, -56.91f), 0f, 1f);
        porta.name = "Porta_InovaLab";
        GameObject arandela = Instanciar(PASTA_POLYGON + "Lamps/Lamp_3_prefab.prefab", predios, new Vector3(0f, 2.75f, -56.85f), 180f, 1.4f);
        arandela.name = "Luminaria_Porta";
        GameObject luzPorta = new GameObject("Luz_Porta");
        luzPorta.transform.SetParent(arandela.transform);
        luzPorta.transform.position = new Vector3(0f, 2.6f, -56.4f);
        Light luzArandela = luzPorta.AddComponent<Light>();
        luzArandela.type = LightType.Point;
        luzArandela.color = new Color(1f, 0.85f, 0.65f);
        luzArandela.intensity = 4f;
        luzArandela.range = 7f;
        CriarFileiraPredios(predios, "Bloco_Saida", 29.2f, false, "");

        // placa de saída no pátio norte
        CriarPlaca(placas, saida + new Vector3(-3.8f, 0f, 3.2f), Vector3.back, "SAÍDA", "Corredor do bloco logo à frente.");
    }

    /// <summary>Cria um objeto vazio filho de outro, para organizar a Hierarchy.</summary>
    /// <param name="pai">Objeto pai.</param>
    /// <param name="nome">Nome do grupo.</param>
    /// <returns>O Transform do grupo criado.</returns>
    private static Transform Grupo(Transform pai, string nome)
    {
        Transform t = new GameObject(nome).transform;
        t.SetParent(pai);
        return t;
    }

    /// <summary>Instancia um prefab mantendo a ligação com o arquivo original.</summary>
    /// <param name="caminho">Caminho do prefab.</param>
    /// <param name="pai">Objeto pai na cena.</param>
    /// <param name="posicao">Posição no mundo.</param>
    /// <param name="giroY">Rotação em Y, em graus.</param>
    /// <param name="escala">Escala uniforme.</param>
    /// <returns>O objeto criado.</returns>
    private static GameObject Instanciar(string caminho, Transform pai, Vector3 posicao, float giroY, float escala)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null)
        {
            Debug.LogWarning("ConstrutorNivel2: prefab não encontrado: " + caminho);
            return new GameObject("FALTANDO_" + System.IO.Path.GetFileNameWithoutExtension(caminho));
        }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
        go.transform.position = posicao;
        go.transform.rotation = Quaternion.Euler(0f, giroY, 0f);
        go.transform.localScale = Vector3.one * escala;
        return go;
    }

    /// <summary>
    /// Cria um poste solar: o poste do pack, uma lâmpada que brilha, uma Point Light
    /// e um painel solar inclinado para o norte (no Brasil, o sol fica ao norte).
    /// </summary>
    /// <param name="pai">Grupo dos postes.</param>
    /// <param name="posicao">Posição no chão.</param>
    /// <param name="comDefeito">Se verdadeiro, a luz pisca como mau contato.</param>
    private static void CriarPosteSolar(Transform pai, Vector3 posicao, bool comDefeito)
    {
        GameObject poste = Instanciar(PASTA_POLYGON + "Lamps/street lamp 2 prefab.prefab", pai, posicao, 0f, 1f);
        poste.name = comDefeito ? "Poste_Solar_ComDefeito" : "Poste_Solar";

        MeshRenderer corpo = poste.GetComponentInChildren<MeshRenderer>();
        Material[] materiais = corpo.sharedMaterials;
        if (materiais.Length > 1)
            materiais[1] = AssetDatabase.LoadAssetAtPath<Material>(PASTA_PATIO + "Patio_Lampada_Acesa.mat");
        corpo.sharedMaterials = materiais;

        Bounds limites = corpo.bounds;
        float topo = limites.max.y;

        GameObject luzObj = new GameObject("Luz");
        luzObj.transform.SetParent(poste.transform);
        luzObj.transform.position = new Vector3(posicao.x, topo - 0.45f, posicao.z);
        Light luz = luzObj.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.8f, 0.58f);
        luz.intensity = 7f;
        luz.range = 11f;
        luz.shadows = LightShadows.None;

        if (comDefeito)
        {
            LuzPiscante piscante = luzObj.AddComponent<LuzPiscante>();
            piscante.lampada = corpo;
        }

        GameObject painel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        painel.name = "Painel_Solar";
        Object.DestroyImmediate(painel.GetComponent<Collider>());
        painel.transform.SetParent(poste.transform);
        painel.transform.position = new Vector3(posicao.x, topo - 0.22f, posicao.z);
        painel.transform.rotation = Quaternion.Euler(-25f, 0f, 0f);
        painel.transform.localScale = new Vector3(0.75f, 0.035f, 0.5f);
        painel.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PASTA_PATIO + "Patio_Painel_Solar.mat");
    }

    /// <summary>Cria um banco de praça virado para a direção indicada.</summary>
    /// <param name="pai">Grupo dos bancos.</param>
    /// <param name="posicao">Posição no chão.</param>
    /// <param name="frente">Para onde a pessoa sentada olha.</param>
    /// <returns>O banco criado.</returns>
    private static GameObject CriarBanco(Transform pai, Vector3 posicao, Vector3 frente)
    {
        // No modelo do pack a pessoa sentada olha para -X, por isso o +90 graus.
        float giro = Mathf.Atan2(frente.x, frente.z) * Mathf.Rad2Deg + 90f;
        GameObject banco = Instanciar(PASTA_POLYGON + "Props/bench prefab.prefab", pai, posicao, giro, 1.15f);
        banco.name = "Banco";
        return banco;
    }

    /// <summary>
    /// Cria um conjunto de lixeiras de coleta seletiva (papel, plástico, vidro, metal e orgânico),
    /// cada uma com a cor oficial e o nome escrito na frente.
    /// </summary>
    /// <param name="pai">Grupo das lixeiras.</param>
    /// <param name="centro">Centro do conjunto.</param>
    /// <param name="frente">Para onde as lixeiras estão viradas.</param>
    private static void CriarConjuntoLixeiras(Transform pai, Vector3 centro, Vector3 frente)
    {
        string[] nomes = { "Papel", "Plastico", "Vidro", "Metal", "Organico" };
        string[] rotulos = { "PAPEL", "PLÁSTICO", "VIDRO", "METAL", "ORGÂNICO" };
        Transform conjunto = Grupo(pai, "Coleta_Seletiva");
        conjunto.position = centro;
        conjunto.rotation = Quaternion.LookRotation(frente);

        for (int i = 0; i < nomes.Length; i++)
        {
            Vector3 pos = conjunto.TransformPoint(new Vector3((i - 2) * 0.78f, 0f, 0f));
            GameObject lixeira = Instanciar(PASTA_POLYGON + "Props/Bin prefab.prefab", conjunto, pos, conjunto.eulerAngles.y, 1f);
            lixeira.name = "Lixeira_" + nomes[i];
            lixeira.GetComponentInChildren<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(PASTA_PATIO + "Patio_Lixeira_" + nomes[i] + ".mat");

            GameObject texto = new GameObject("Rotulo");
            texto.transform.SetParent(lixeira.transform);
            texto.transform.position = pos + frente.normalized * 0.33f + Vector3.up * 0.62f;
            texto.transform.rotation = Quaternion.LookRotation(-frente);
            TextMeshPro tmp = texto.AddComponent<TextMeshPro>();
            tmp.text = rotulos[i];
            tmp.fontSize = 1.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(0.9f, 0.25f);
        }
    }

    /// <summary>Cria uma placa com dois postes, um título e um texto curto dos dois lados.</summary>
    /// <param name="pai">Grupo das placas.</param>
    /// <param name="posicao">Posição no chão.</param>
    /// <param name="frente">Para onde a frente da placa olha.</param>
    /// <param name="titulo">Título em letras maiores.</param>
    /// <param name="texto">Texto menor embaixo do título.</param>
    private static void CriarPlaca(Transform pai, Vector3 posicao, Vector3 frente, string titulo, string texto)
    {
        Transform placa = Grupo(pai, "Placa_" + titulo.Replace(" ", "_"));
        placa.position = posicao;
        placa.rotation = Quaternion.LookRotation(frente);

        for (int lado = -1; lado <= 1; lado += 2)
        {
            GameObject poste = Instanciar(PASTA_POLYGON + "Props/platt pole prefab.prefab", placa, placa.TransformPoint(new Vector3(lado * 0.7f, 0f, 0f)), 0f, 1f);
            poste.transform.localScale = new Vector3(0.45f, 1.55f, 0.45f);
        }

        // A placa do pack fica no plano YZ (fina em X) e começa na borda; aqui ela é girada para ficar de frente.
        GameObject quadro = Instanciar(PASTA_POLYGON + "Props/platt sign 1 prefab.prefab", placa, placa.TransformPoint(new Vector3(0.8f, 1.0f, 0f)), placa.eulerAngles.y + 90f, 1f);
        quadro.transform.localScale = new Vector3(0.6f, 0.8f, 1.6f);
        foreach (MeshRenderer r in quadro.GetComponentsInChildren<MeshRenderer>())
            r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PASTA_PATIO + "Patio_Placa_Verde.mat");

        for (int lado = 0; lado < 2; lado++)
        {
            // o quadro ocupa de 0 a 0,06 m para a frente; o texto fica um pouco fora dele em cada lado
            Vector3 direcao = lado == 0 ? frente.normalized : -frente.normalized;
            float afastamento = lado == 0 ? 0.09f : 0.03f;
            GameObject tituloObj = new GameObject(lado == 0 ? "Titulo_Frente" : "Titulo_Verso");
            tituloObj.transform.SetParent(placa);
            tituloObj.transform.position = posicao + Vector3.up * 1.62f + direcao * afastamento;
            tituloObj.transform.rotation = Quaternion.LookRotation(-direcao);
            TextMeshPro t1 = tituloObj.AddComponent<TextMeshPro>();
            t1.text = titulo;
            t1.fontSize = 1.5f;
            t1.fontStyle = FontStyles.Bold;
            t1.alignment = TextAlignmentOptions.Center;
            t1.color = new Color(1f, 0.97f, 0.85f);
            t1.rectTransform.sizeDelta = new Vector2(1.5f, 0.3f);

            GameObject textoObj = new GameObject(lado == 0 ? "Texto_Frente" : "Texto_Verso");
            textoObj.transform.SetParent(placa);
            textoObj.transform.position = posicao + Vector3.up * 1.32f + direcao * afastamento;
            textoObj.transform.rotation = Quaternion.LookRotation(-direcao);
            TextMeshPro t2 = textoObj.AddComponent<TextMeshPro>();
            t2.text = texto;
            t2.fontSize = 0.9f;
            t2.alignment = TextAlignmentOptions.Center;
            t2.color = new Color(0.92f, 0.95f, 0.9f);
            t2.rectTransform.sizeDelta = new Vector2(1.5f, 0.35f);
        }
    }

    /// <summary>Cria uma parede invisível (só o collider) para o jogador não sair da área do nível.</summary>
    /// <param name="pai">Grupo dos limites.</param>
    /// <param name="centro">Centro da parede.</param>
    /// <param name="tamanho">Tamanho da caixa.</param>
    private static void ParedeInvisivel(Transform pai, Vector3 centro, Vector3 tamanho)
    {
        GameObject parede = new GameObject("Parede_Invisivel");
        parede.transform.SetParent(pai);
        parede.transform.position = centro;
        parede.AddComponent<BoxCollider>().size = tamanho;
    }

    /// <summary>
    /// Monta uma fileira de 6 módulos de prédio do pack (ponta, 4 do meio e ponta),
    /// formando a fachada de um bloco do campus com cerca de 41 m.
    /// No modelo do pack a fachada (janelas e porta) fica no lado +X, por isso cada módulo é girado.
    /// </summary>
    /// <param name="pai">Grupo dos prédios.</param>
    /// <param name="nome">Nome do bloco.</param>
    /// <param name="zFachada">Z onde fica a fachada.</param>
    /// <param name="fachadaParaNorte">Verdadeiro se a fachada olha para +Z (bloco ao sul do pátio).</param>
    /// <param name="letreiro">Texto grande escrito na fachada (pode ser vazio).</param>
    private static void CriarFileiraPredios(Transform pai, string nome, float zFachada, bool fachadaParaNorte, string letreiro)
    {
        Transform bloco = Grupo(pai, nome);
        string[] modulos = { "Build_G-Left_Prefab", "Build_G-middle_Prefab", "Build_G-middle_Prefab",
                             "Build_G-middle_Prefab", "Build_G-middle_Prefab", "Build_G-right_Prefab" };
        float giro = fachadaParaNorte ? -90f : 90f;
        for (int i = 0; i < modulos.Length; i++)
        {
            // limites do módulo no próprio modelo: X de -3,05 a 10,85 (fachada em 10,85) e Z de -5,8 a 1,0
            float x = fachadaParaNorte ? -20.4f + 1.0f + i * 6.8f : 20.4f - 1.0f - i * 6.8f;
            float z = fachadaParaNorte ? zFachada - 10.85f : zFachada + 10.85f;
            Instanciar(PASTA_POLYGON + "Buildings/" + modulos[i] + ".prefab", bloco, new Vector3(x, 0f, z), giro, 1f);
        }

        if (string.IsNullOrEmpty(letreiro))
            return;

        Vector3 frente = fachadaParaNorte ? Vector3.forward : Vector3.back;
        GameObject texto = new GameObject("Letreiro");
        texto.transform.SetParent(bloco);
        texto.transform.position = new Vector3(0f, 4.2f, zFachada) + frente * 0.15f;
        texto.transform.rotation = Quaternion.LookRotation(-frente);
        TextMeshPro tmp = texto.AddComponent<TextMeshPro>();
        tmp.text = letreiro;
        tmp.fontSize = 9f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.75f, 0.95f, 1f);
        tmp.rectTransform.sizeDelta = new Vector2(16f, 2.5f);
    }

    // ------------------------------------------------------------------
    //  Etapa 5: atmosfera (lua, céu, neblina, pós-processamento, vagalumes, som)
    // ------------------------------------------------------------------

    /// <summary>Configura a noite: luz da lua, céu estrelado, luz ambiente, neblina, pós-processamento, vagalumes e som.</summary>
    public static void MontarAtmosfera()
    {
        ApagarRaiz("Atmosfera");
        ApagarRaiz("Luz_Lua");
        Transform atmosfera = new GameObject("Atmosfera").transform;

        // Lua: a luz vem da mesma direção em que a lua aparece no céu
        Vector3 direcaoLua = new Vector3(Mathf.Sin(35f * Mathf.Deg2Rad) * Mathf.Cos(30f * Mathf.Deg2Rad), Mathf.Sin(30f * Mathf.Deg2Rad), Mathf.Cos(35f * Mathf.Deg2Rad) * Mathf.Cos(30f * Mathf.Deg2Rad));
        GameObject lua = new GameObject("Luz_Lua");
        lua.transform.SetParent(atmosfera);
        lua.transform.rotation = Quaternion.LookRotation(-direcaoLua);
        Light luzLua = lua.AddComponent<Light>();
        luzLua.type = LightType.Directional;
        luzLua.color = new Color(0.62f, 0.72f, 1f);
        luzLua.intensity = 0.65f;
        luzLua.shadows = LightShadows.Soft;
        luzLua.shadowStrength = 0.75f;
        RenderSettings.sun = luzLua;

        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Projeto/Materiais/Ceu/Ceu_Noite.mat");
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f, 0.26f, 0.42f);
        RenderSettings.ambientEquatorColor = new Color(0.16f, 0.18f, 0.28f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.05f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.011f;
        RenderSettings.fogColor = new Color(0.05f, 0.065f, 0.11f);
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 0.7f;

        // Pós-processamento
        GameObject volumeObj = new GameObject("Volume_PosProcessamento");
        volumeObj.transform.SetParent(atmosfera);
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = CriarPerfilPosProcessamento();

        // Vagalumes (partículas)
        CriarVagalumes(atmosfera, "Vagalumes_Patio", new Vector3(0f, 1.4f, -38f), new Vector3(36f, 2.2f, 30f), 9f, 70);
        CriarVagalumes(atmosfera, "Vagalumes_Lago", new Vector3(34f, 1.2f, -36f), new Vector3(22f, 2f, 22f), 6f, 50);
        CriarVagalumes(atmosfera, "Vagalumes_Praca", new Vector3(0f, 1.4f, 1f), new Vector3(11f, 2f, 9f), 3f, 25);
        CriarVagalumes(atmosfera, "Vagalumes_Labirinto", new Vector3(0f, 1.6f, 0f), new Vector3(40f, 2f, 40f), 6f, 50);
        CriarVagalumes(atmosfera, "Vagalumes_Ravina", new Vector3(-33f, 0f, -30f), new Vector3(8f, 3f, 50f), 4f, 40);

        // Som de fundo (grilos) e sons aleatórios — os áudios são colocados depois de baixados
        GameObject somFundo = new GameObject("Som_Ambiente_Noite");
        somFundo.transform.SetParent(atmosfera);
        AudioSource fonte = somFundo.AddComponent<AudioSource>();
        fonte.loop = true;
        fonte.playOnAwake = true;
        fonte.spatialBlend = 0f;
        fonte.volume = 0.45f;
        fonte.clip = CarregarAudio("Assets/_Projeto/Audio/amb_noite_grilos");

        GameObject sons = new GameObject("Sons_Aleatorios");
        sons.transform.SetParent(atmosfera);
        SomAmbienteAleatorio aleatorios = sons.AddComponent<SomAmbienteAleatorio>();
        AudioClip coruja = CarregarAudio("Assets/_Projeto/Audio/sfx_coruja");
        aleatorios.sons = coruja != null ? new[] { coruja } : new AudioClip[0];
    }

    /// <summary>
    /// Carrega um áudio aceitando .wav, .mp3 ou .ogg (o site às vezes entrega em outro formato).
    /// </summary>
    /// <param name="caminhoSemExtensao">Caminho do arquivo sem a extensão, ex.: Assets/_Projeto/Audio/sfx_coruja.</param>
    /// <returns>O AudioClip encontrado ou null.</returns>
    private static AudioClip CarregarAudio(string caminhoSemExtensao)
    {
        foreach (string extensao in new[] { ".wav", ".mp3", ".ogg" })
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(caminhoSemExtensao + extensao);
            if (clip != null)
                return clip;
        }
        return null;
    }

    /// <summary>Cria (ou recria) o perfil de pós-processamento da noite: bloom, tonemapping, cor e vinheta.</summary>
    /// <returns>O perfil salvo em _Projeto/PosProcessamento.</returns>
    private static VolumeProfile CriarPerfilPosProcessamento()
    {
        CriarPasta("Assets/_Projeto/PosProcessamento");
        string caminho = "Assets/_Projeto/PosProcessamento/Nivel2_Noite.asset";
        AssetDatabase.DeleteAsset(caminho);
        VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(perfil, caminho);

        Bloom bloom = perfil.Add<Bloom>(true);
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(1.3f);
        bloom.scatter.Override(0.72f);

        Tonemapping tonemapping = perfil.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);

        ColorAdjustments cor = perfil.Add<ColorAdjustments>(true);
        cor.postExposure.Override(0.8f);
        cor.contrast.Override(12f);
        cor.saturation.Override(-8f);
        cor.colorFilter.Override(new Color(0.92f, 0.96f, 1f));

        Vignette vinheta = perfil.Add<Vignette>(true);
        vinheta.intensity.Override(0.32f);
        vinheta.smoothness.Override(0.45f);

        WhiteBalance balanco = perfil.Add<WhiteBalance>(true);
        balanco.temperature.Override(-12f);

        foreach (VolumeComponent componente in perfil.components)
            AssetDatabase.AddObjectToAsset(componente, perfil);
        EditorUtility.SetDirty(perfil);
        AssetDatabase.SaveAssets();
        return perfil;
    }

    /// <summary>
    /// Cria um sistema de partículas de vagalumes: pontinhos verde-amarelados que aparecem
    /// numa caixa, flutuam devagar (módulo Noise) e piscam (cor ao longo da vida).
    /// </summary>
    /// <param name="pai">Grupo da atmosfera.</param>
    /// <param name="nome">Nome do objeto.</param>
    /// <param name="centro">Centro da área.</param>
    /// <param name="tamanho">Tamanho da área (caixa).</param>
    /// <param name="porSegundo">Quantos vagalumes nascem por segundo.</param>
    /// <param name="maximo">Máximo de vagalumes ao mesmo tempo.</param>
    private static void CriarVagalumes(Transform pai, string nome, Vector3 centro, Vector3 tamanho, float porSegundo, int maximo)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai);
        go.transform.position = centro;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 10f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 1f, 0.3f), new Color(1f, 0.95f, 0.45f));
        main.maxParticles = maximo;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule emissao = ps.emission;
        emissao.rateOverTime = porSegundo;

        ParticleSystem.ShapeModule forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = new Vector3(tamanho.x, tamanho.z, tamanho.y);

        ParticleSystem.NoiseModule ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.55f;
        ruido.frequency = 0.25f;
        ruido.scrollSpeed = 0.15f;
        ruido.octaveCount = 1;

        // pisca: a transparência sobe e desce algumas vezes durante a vida
        ParticleSystem.ColorOverLifetimeModule corVida = ps.colorOverLifetime;
        corVida.enabled = true;
        Gradient pisca = new Gradient();
        pisca.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.15f, 0.3f),
                new GradientAlphaKey(1f, 0.45f), new GradientAlphaKey(0.1f, 0.62f), new GradientAlphaKey(1f, 0.78f),
                new GradientAlphaKey(0f, 1f)
            });
        corVida.color = pisca;

        ParticleSystemRenderer renderizador = go.GetComponent<ParticleSystemRenderer>();
        renderizador.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PASTA_PATIO + "Patio_Vagalume.mat");
        renderizador.renderMode = ParticleSystemRenderMode.Billboard;
        renderizador.shadowCastingMode = ShadowCastingMode.Off;
        renderizador.receiveShadows = false;

        ps.Play();
    }

    // ------------------------------------------------------------------
    //  Etapa 6: gameplay (jogador, objetivo, saída, segredos e gato)
    // ------------------------------------------------------------------

    /// <summary>
    /// Ajusta os objetos de jogo da cena: posição inicial do jogador, câmera com pós-processamento,
    /// textos e contador de segredos, área de saída, prefabs de segredos sorteados nos becos e o gato.
    /// </summary>
    public static void MontarGameplay()
    {
        // o gato fica dentro de "Segredos"; tira ele de lá antes de apagar o grupo antigo
        GameObject gatoAntes = GameObject.Find("Gato_Patio");
        if (gatoAntes != null)
            gatoAntes.transform.SetParent(null);

        foreach (string antigo in new[] { "Patio_Externo", "Postes", "Saida_Nivel", "Segredos" })
            ApagarRaiz(antigo);

        GameObject jogador = GameObject.FindWithTag("Player");
        jogador.transform.position = new Vector3(0f, 0.05f, -50f);
        jogador.transform.rotation = Quaternion.identity;

        GameObject cameraObj = GameObject.Find("Camera_TerceiraPessoa");
        Camera camera = cameraObj.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.farClipPlane = 600f;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        cameraObj.transform.position = new Vector3(0f, 2.2f, -54f);
        cameraObj.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        gm.totalItens = 0;
        gm.rotuloContador = "Segredos";
        gm.saidaAbertaDesdeOInicio = true;
        gm.prefixoAviso = "Segredo encontrado: ";
        gm.proximaCena = "Nivel3";
        gm.textoObjetivo = "Já é noite. Encontre a saída do labirinto do parque.\n<size=70%>Dica: há segredos escondidos pelo caminho.</size>";
        gm.textoTudoEncontrado = "Você achou todos os segredos do pátio! Agora é só sair do labirinto.";
        EditorUtility.SetDirty(gm);

        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (ui != null)
        {
            Transform contador = ui.transform.Find("Texto_Contador");
            if (contador != null)
            {
                ui.textoContador = contador.GetComponent<TextMeshProUGUI>();
                contador.gameObject.SetActive(true);
                EditorUtility.SetDirty(ui);
            }
        }

        GeradorLabirinto lab = Object.FindFirstObjectByType<GeradorLabirinto>();

        // Saída: área invisível logo depois da abertura norte do labirinto
        GameObject saida = new GameObject("Saida_Nivel");
        saida.tag = "Porta";
        saida.transform.position = lab.PosicaoNoMundo(0, 13) + new Vector3(0f, 1.5f, 2.6f);
        BoxCollider areaSaida = saida.AddComponent<BoxCollider>();
        areaSaida.isTrigger = true;
        areaSaida.size = new Vector3(4f, 3f, 1.5f);
        saida.AddComponent<SaidaNivel>();
        GameObject luzSaida = new GameObject("Luz_Saida");
        luzSaida.transform.SetParent(saida.transform);
        luzSaida.transform.localPosition = new Vector3(0f, 1f, 0.5f);
        Light verde = luzSaida.AddComponent<Light>();
        verde.type = LightType.Point;
        verde.color = new Color(0.45f, 1f, 0.55f);
        verde.intensity = 2.5f;
        verde.range = 7f;

        // Segredos
        GameObject segredos = new GameObject("Segredos");
        GameObject brilho = CriarPrefabBrilhoSegredo();
        List<GameObject> prefabs = new List<GameObject>
        {
            CriarPrefabSegredo("Segredo_Livro", PASTA_ESCOLA + "book.prefab", new Vector3(0f, 0.06f, 0f), new Vector3(0f, 0f, 90f),
                "Livro perdido da biblioteca", "Será que tem mais desses por aí?", brilho),
            CriarPrefabSegredo("Segredo_Lanche", PASTA_POLYGON + "Props/burger prefab.prefab", Vector3.zero, Vector3.zero,
                "Lanche esquecido da InovaWeek", "Ainda está quentinho...", brilho),
            CriarPrefabSegredo("Segredo_Notebook", PASTA_ESCOLA + "a laptop.prefab", Vector3.zero, new Vector3(0f, 180f, 0f),
                "Notebook esquecido", "O código ainda está compilando.", brilho)
        };

        int[,] becos = { { 1, 1 }, { 1, 15 }, { 5, 11 }, { 13, 3 }, { 13, 11 }, { 15, 15 }, { 17, 1 } };
        Transform pontos = new GameObject("Pontos_Segredos").transform;
        pontos.SetParent(segredos.transform);
        Transform bancosBecos = new GameObject("Bancos_Becos").transform;
        bancosBecos.SetParent(segredos.transform);
        List<Transform> listaPontos = new List<Transform>();
        string[] mapa = lab.LinhasDoMapa();
        for (int i = 0; i < becos.GetLength(0); i++)
        {
            int l = becos[i, 0], c = becos[i, 1];
            Vector3 centro = lab.PosicaoNoMundo(l, c);
            Vector3 abertura = Vector3.zero;
            if (mapa[l - 1][c] != '#') abertura = Vector3.forward;
            if (mapa[l + 1][c] != '#') abertura = Vector3.back;
            if (mapa[l][c - 1] != '#') abertura = Vector3.left;
            if (mapa[l][c + 1] != '#') abertura = Vector3.right;

            // banco encostado no fundo do beco, olhando para a abertura
            Vector3 posBanco = centro - abertura * 1.1f;
            CriarBanco(bancosBecos, posBanco, abertura);

            Transform ponto = new GameObject("Ponto_" + (i + 1)).transform;
            ponto.SetParent(pontos);
            ponto.position = posBanco + abertura * 0.25f + Vector3.up * 0.47f;
            ponto.rotation = Quaternion.LookRotation(abertura);
            listaPontos.Add(ponto);
        }

        SpawnItens spawn = segredos.AddComponent<SpawnItens>();
        spawn.prefabsItens = prefabs.ToArray();
        spawn.pontosSpawn = listaPontos.ToArray();

        // Gato: patrulha um corredor sem saída no lado leste e também conta como segredo
        GameObject gato = GameObject.Find("Gato_Patio");
        if (gato != null)
        {
            gato.transform.SetParent(segredos.transform);
            GatoPatrulha patrulha = gato.GetComponentInChildren<GatoPatrulha>();
            Vector3 a = lab.PosicaoNoMundo(5, 15) + new Vector3(0f, 0f, -0.6f);
            Vector3 b = lab.PosicaoNoMundo(7, 15) + new Vector3(0f, 0f, -0.9f);
            patrulha.transform.position = a;
            patrulha.pontos[0].position = a;
            patrulha.pontos[1].position = b;

            Segredo segredoGato = patrulha.GetComponent<Segredo>();
            if (segredoGato == null)
            {
                SphereCollider area = patrulha.gameObject.AddComponent<SphereCollider>();
                area.isTrigger = true;
                area.radius = 1.1f;
                area.center = new Vector3(0f, 0.3f, 0f);
                segredoGato = patrulha.gameObject.AddComponent<Segredo>();
            }
            segredoGato.nomeSegredo = "Misty, a gata do campus";
            segredoGato.frase = "Ela sempre aparece quando alguém fica até tarde.";
            segredoGato.efeitoDescoberta = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Projeto/Prefabs/Efeitos/Efeito_Coleta.prefab");
            segredoGato.somDescoberta = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Projeto/Audio/sfx_coleta.wav");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(jogador.scene);
    }

    /// <summary>Cria o prefab de partículas douradas que fica em volta de cada segredo (a "dica" visual).</summary>
    /// <returns>O prefab salvo em Prefabs/Efeitos.</returns>
    private static GameObject CriarPrefabBrilhoSegredo()
    {
        GameObject go = new GameObject("Brilho_Segredo");
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startColor = new Color(1f, 0.85f, 0.45f);
        main.maxParticles = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emissao = ps.emission;
        emissao.rateOverTime = 6f;

        ParticleSystem.ShapeModule forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.35f;

        ParticleSystem.ColorOverLifetimeModule corVida = ps.colorOverLifetime;
        corVida.enabled = true;
        Gradient some = new Gradient();
        some.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        corVida.color = some;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PASTA_PATIO + "Patio_Brilho_Segredo.mat");
        r.shadowCastingMode = ShadowCastingMode.Off;

        CriarPasta("Assets/_Projeto/Prefabs/Efeitos");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/_Projeto/Prefabs/Efeitos/Brilho_Segredo.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// <summary>Monta e salva o prefab de um segredo: o modelo, a área de detecção, o script Segredo e o brilho.</summary>
    /// <param name="nome">Nome do prefab.</param>
    /// <param name="modelo">Caminho do prefab do modelo (livro, lanche...).</param>
    /// <param name="deslocamento">Ajuste de posição do modelo dentro do prefab.</param>
    /// <param name="giro">Rotação do modelo dentro do prefab.</param>
    /// <param name="titulo">Nome mostrado na tela.</param>
    /// <param name="frase">Frase mostrada junto do nome.</param>
    /// <param name="brilho">Prefab do brilho de dica.</param>
    /// <returns>O prefab salvo em Prefabs/Segredos.</returns>
    private static GameObject CriarPrefabSegredo(string nome, string modelo, Vector3 deslocamento, Vector3 giro,
                                                 string titulo, string frase, GameObject brilho)
    {
        GameObject raiz = new GameObject(nome);
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelo), raiz.transform);
        PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        visual.transform.localPosition = deslocamento;
        visual.transform.localRotation = Quaternion.Euler(giro);
        foreach (Collider c in visual.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);

        BoxCollider area = raiz.AddComponent<BoxCollider>();
        area.isTrigger = true;
        area.size = new Vector3(2.2f, 2.5f, 2.2f);
        area.center = new Vector3(0f, 0.6f, 0f);

        GameObject dica = (GameObject)PrefabUtility.InstantiatePrefab(brilho, raiz.transform);
        dica.transform.localPosition = new Vector3(0f, 0.25f, 0f);

        Segredo segredo = raiz.AddComponent<Segredo>();
        segredo.nomeSegredo = titulo;
        segredo.frase = frase;
        segredo.dicaVisual = dica;
        segredo.efeitoDescoberta = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Projeto/Prefabs/Efeitos/Efeito_Coleta.prefab");
        segredo.somDescoberta = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Projeto/Audio/sfx_coleta.wav");

        CriarPasta("Assets/_Projeto/Prefabs/Segredos");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/_Projeto/Prefabs/Segredos/" + nome + ".prefab");
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    // ------------------------------------------------------------------
    //  Utilidades
    // ------------------------------------------------------------------

    /// <summary>Apaga da cena todos os objetos raiz com esse nome (se existirem).</summary>
    /// <param name="nome">Nome do objeto.</param>
    private static void ApagarRaiz(string nome)
    {
        foreach (GameObject raiz in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (raiz.name == nome)
                Object.DestroyImmediate(raiz);
    }

    /// <summary>Cria uma pasta (e as pastas acima dela) se ainda não existir.</summary>
    /// <param name="caminho">Caminho começando em Assets.</param>
    private static void CriarPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho))
            return;
        string pai = System.IO.Path.GetDirectoryName(caminho).Replace('\\', '/');
        CriarPasta(pai);
        AssetDatabase.CreateFolder(pai, System.IO.Path.GetFileName(caminho));
    }

    /// <summary>Carrega um material ou cria um novo com o shader indicado.</summary>
    /// <param name="caminho">Caminho do material.</param>
    /// <param name="shader">Shader usado se o material for criado agora.</param>
    /// <returns>O material.</returns>
    private static Material CarregarOuCriarMaterial(string caminho, Shader shader)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (m != null)
            return m;
        m = new Material(shader);
        AssetDatabase.CreateAsset(m, caminho);
        return m;
    }
}
