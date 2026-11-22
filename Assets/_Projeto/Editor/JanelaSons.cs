using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Janela "Escape UVV > Sons do nível": lista todos os sons usados na cena aberta.
/// Para cada som dá para ouvir, ver a forma de onda, trocar o arquivo, mexer no volume
/// de cada lugar onde ele toca e editar o próprio arquivo (volume, cortar o começo
/// para sincronizar, cortar o fim e suavizar o final). O original fica guardado e
/// pode voltar a qualquer momento pelo botão "Voltar ao original".
/// </summary>
public class JanelaSons : EditorWindow
{
    private const string PastaBackup = "Library/BackupSons";

    private enum Aba { Todos, Ambiente, Efeitos }
    private static readonly string[] nomesAbas = { "Todos", "Ambiente e loops", "Efeitos" };

    /// <summary>Um lugar da cena que toca o som.</summary>
    private class Uso
    {
        public Object dono;
        public string caminhoClip;
        public string caminhoVolume;   // vazio = volume fixo no script
        public Object donoVolume;      // às vezes o volume fica em outro componente do mesmo objeto
        public string descricao;
        public bool loop;
    }

    /// <summary>Um arquivo de som e todos os lugares onde ele toca.</summary>
    private class Som
    {
        public AudioClip clip;                              // primeiro arquivo (nome e prévia)
        public List<AudioClip> clips = new List<AudioClip>(); // grupo (ex.: os 10 passos)
        public string titulo;
        public List<Uso> usos = new List<Uso>();
        public bool aberto;
        public bool editando;
        public float ganhoDb;
        public float cortarInicio;
        public float cortarFim;
        public float fadeFim;
        public Texture2D onda;
        public float volumeMedioDb = float.NaN;
    }

    private readonly List<Som> sons = new List<Som>();
    private Aba aba = Aba.Todos;
    private string busca = "";
    private Vector2 rolagem;
    private GUIStyle estiloCinza;
    private GUIStyle estiloTitulo;

    [MenuItem("Escape UVV/Sons do nível")]
    public static void Abrir()
    {
        JanelaSons janela = GetWindow<JanelaSons>("Sons do nível");
        janela.minSize = new Vector2(480, 320);
        janela.Recarregar();
    }

    void OnEnable()
    {
        EditorSceneManager.sceneOpened -= AoAbrirCena;
        EditorSceneManager.sceneOpened += AoAbrirCena;
        Recarregar();
    }

    void OnDisable()
    {
        EditorSceneManager.sceneOpened -= AoAbrirCena;
        PararPrevia();
    }

    private void AoAbrirCena(UnityEngine.SceneManagement.Scene cena, OpenSceneMode modo)
    {
        Recarregar();
        Repaint();
    }

    // ------------------------------------------------------------------
    // Montar a lista
    // ------------------------------------------------------------------

    private void Recarregar()
    {
        Dictionary<AudioClip, Som> porClip = new Dictionary<AudioClip, Som>();
        Dictionary<string, Som> porGrupo = new Dictionary<string, Som>();

        foreach (Component comp in FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (comp == null || EditorUtility.IsPersistent(comp))
                continue;

            bool fonte = comp is AudioSource;
            bool doProjeto = comp is MonoBehaviour && comp.GetType().Assembly.GetName().Name == "Assembly-CSharp";
            if (!fonte && !doProjeto)
                continue;

            SerializedObject so = new SerializedObject(comp);
            SerializedProperty it = so.GetIterator();
            bool entrar = true;
            while (it.NextVisible(entrar))
            {
                entrar = it.propertyType == SerializedPropertyType.Generic;
                if (it.propertyType != SerializedPropertyType.ObjectReference || !(it.objectReferenceValue is AudioClip clip))
                    continue;

                // Lista de sons no mesmo campo (ex.: passos): um grupo só.
                Match lista = Regex.Match(it.propertyPath, @"^(.*)\.Array\.data\[(\d+)\]$");
                Som som;
                if (lista.Success && new SerializedObject(comp).FindProperty(lista.Groups[1].Value).arraySize > 1)
                {
                    string chaveGrupo = comp.GetType().Name + "." + lista.Groups[1].Value;
                    if (!porGrupo.TryGetValue(chaveGrupo, out som))
                    {
                        som = new Som { clip = clip, titulo = ObjectNames.NicifyVariableName(lista.Groups[1].Value) };
                        porGrupo[chaveGrupo] = som;
                    }
                    if (!som.clips.Contains(clip))
                        som.clips.Add(clip);
                    if (lista.Groups[2].Value != "0")
                        continue; // os outros itens da lista usam o mesmo volume e o mesmo lugar
                }
                else if (!porClip.TryGetValue(clip, out som))
                {
                    som = new Som { clip = clip };
                    som.clips.Add(clip);
                    porClip[clip] = som;
                }

                Uso uso = new Uso { dono = comp, caminhoClip = it.propertyPath };
                if (fonte)
                {
                    AudioSource src = (AudioSource)comp;
                    uso.loop = src.loop;
                    uso.descricao = NomeDoObjeto(comp.transform) + " (toca sozinho" + (src.loop ? " em loop" : "") + ")";
                    // Alguns scripts controlam o volume final da fonte (ex.: zumbido que entra aos poucos).
                    uso.donoVolume = comp;
                    uso.caminhoVolume = "m_Volume";
                    foreach (MonoBehaviour irmao in comp.GetComponents<MonoBehaviour>())
                    {
                        if (irmao == null)
                            continue;
                        if (new SerializedObject(irmao).FindProperty("volumeFinal") != null)
                        {
                            uso.donoVolume = irmao;
                            uso.caminhoVolume = "volumeFinal";
                        }
                    }
                }
                else
                {
                    string campo = ObjectNames.NicifyVariableName(UltimoNome(it.propertyPath));
                    if (campo.StartsWith("Som "))
                        campo = campo.Substring(4);
                    campo = Regex.Replace(campo, @" 0$", "").ToLower();
                    string dono = nomesPorScript.TryGetValue(comp.GetType().Name, out string n) ? n : NomeDoObjeto(comp.transform);
                    uso.descricao = dono + " · " + campo;
                    uso.donoVolume = comp;
                    uso.caminhoVolume = AcharVolume(so, it.propertyPath);
                }
                som.usos.Add(uso);
            }
        }

        // Mantém o que já estava aberto ao recarregar.
        Dictionary<AudioClip, Som> antigos = new Dictionary<AudioClip, Som>();
        foreach (Som s in sons)
            if (s.clip != null)
                antigos[s.clip] = s;

        sons.Clear();
        List<Som> todos = new List<Som>(porClip.Values);
        foreach (Som g in porGrupo.Values)
        {
            g.titulo += " (" + g.clips.Count + " arquivos)";
            for (int i = 0; i < g.usos.Count; i++)
                g.usos[i].descricao = Regex.Replace(g.usos[i].descricao, @" 0$", "");
            todos.Add(g);
        }
        foreach (Som s in todos)
        {
            if (antigos.TryGetValue(s.clip, out Som velho))
            {
                s.aberto = velho.aberto;
                s.editando = velho.editando;
                s.onda = velho.onda;
                s.volumeMedioDb = velho.volumeMedioDb;
            }
            sons.Add(s);
        }
        sons.Sort((a, b) => string.Compare(Titulo(a), Titulo(b), System.StringComparison.CurrentCultureIgnoreCase));
    }

    private static string Titulo(Som s) => string.IsNullOrEmpty(s.titulo) ? s.clip.name : s.titulo;

    /// <summary>Procura o campo de volume que acompanha um som no script (ex.: somArmario → volumeArmario).</summary>
    private static string AcharVolume(SerializedObject so, string caminhoClip)
    {
        string nome = Regex.Replace(caminhoClip, @"\.Array\.data\[\d+\]", "");
        nome = nome.Split('.')[0];

        List<string> candidatos = new List<string>();
        string Maiuscula(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
        candidatos.Add("volume" + Maiuscula(nome));
        if (nome.StartsWith("som"))
            candidatos.Add("volume" + nome.Substring(3));
        Match m = Regex.Match(nome, @"[A-Z].*$");
        if (m.Success)
            candidatos.Add("volume" + m.Value);
        candidatos.Add("volumeSons");
        candidatos.Add("volumeSom");
        candidatos.Add("volume");

        foreach (string c in candidatos)
        {
            SerializedProperty p = so.FindProperty(c);
            if (p != null && p.propertyType == SerializedPropertyType.Float)
                return c;
        }

        // Plural e singular (trovoes → volumeTrovao): compara o começo do nome.
        string baseNome = (nome.StartsWith("som") ? nome.Substring(3) : nome).ToLower();
        if (baseNome.Length >= 4)
        {
            SerializedProperty it = so.GetIterator();
            bool entrar = true;
            while (it.NextVisible(entrar))
            {
                entrar = false;
                if (it.propertyType == SerializedPropertyType.Float && it.name.StartsWith("volume") && it.name.Length > 6
                    && it.name.Substring(6).ToLower().StartsWith(baseNome.Substring(0, 4)))
                    return it.name;
            }
        }
        return "";
    }

    // ------------------------------------------------------------------
    // Tela
    // ------------------------------------------------------------------

    void OnGUI()
    {
        if (estiloCinza == null)
        {
            estiloCinza = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }, wordWrap = true };
            estiloTitulo = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        }

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(EditorSceneManager.GetActiveScene().name, EditorStyles.largeLabel);
            if (GUILayout.Button("Parar som", GUILayout.Width(80)))
                PararPrevia();
            if (GUILayout.Button("Atualizar lista", GUILayout.Width(100)))
                Recarregar();
        }

        aba = (Aba)GUILayout.Toolbar((int)aba, nomesAbas);
        EditorGUILayout.HelpBox(
            "Volume: muda só naquele lugar do jogo (salve a cena com Ctrl+S).\n" +
            "Editar o arquivo: muda o som em todo lugar que ele toca. Use \"Cortar começo\" quando o som chega atrasado.",
            MessageType.None);
        busca = EditorGUILayout.TextField("Procurar", busca);

        rolagem = EditorGUILayout.BeginScrollView(rolagem);
        foreach (Som som in sons)
        {
            if (som.clip == null)
                continue;
            bool ehLoop = som.usos.Exists(u => u.loop) || som.clip.length > 12f;
            if (aba == Aba.Ambiente && !ehLoop || aba == Aba.Efeitos && ehLoop)
                continue;
            if (!string.IsNullOrEmpty(busca) && Titulo(som).IndexOf(busca, System.StringComparison.OrdinalIgnoreCase) < 0
                && !som.usos.Exists(u => u.descricao.IndexOf(busca, System.StringComparison.OrdinalIgnoreCase) >= 0))
                continue;

            DesenharSom(som);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DesenharSom(Som som)
    {
        EditorGUILayout.Space(6);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶", GUILayout.Width(28)))
                    TocarPrevia(som.clips[Random.Range(0, som.clips.Count)]);
                som.aberto = EditorGUILayout.Foldout(som.aberto, Titulo(som), true, EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(som.clip.length.ToString("0.0") + " s · " + som.usos.Count + (som.usos.Count == 1 ? " lugar" : " lugares"), estiloCinza, GUILayout.Width(110));
            }

            if (!som.aberto)
                return;

            if (som.onda == null)
                GerarOnda(som);
            if (som.onda != null)
            {
                Rect r = GUILayoutUtility.GetRect(100, 48, GUILayout.ExpandWidth(true));
                GUI.DrawTexture(r, som.onda, ScaleMode.StretchToFill);
                if (!float.IsNaN(som.volumeMedioDb))
                    EditorGUILayout.LabelField("Volume do arquivo: " + DescreverVolume(som.volumeMedioDb), estiloCinza);
            }

            if (som.clips.Count > 1)
            {
                EditorGUILayout.LabelField("Grupo: o jogo sorteia um destes a cada vez. Editar o arquivo muda todos.", estiloCinza);
                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (AudioClip c in som.clips)
                        if (GUILayout.Button(new GUIContent((som.clips.IndexOf(c) + 1).ToString(), c.name), EditorStyles.miniButton, GUILayout.Width(26)))
                            TocarPrevia(c);
                }
            }
            else using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Arquivo", GUILayout.Width(60));
                AudioClip novo = (AudioClip)EditorGUILayout.ObjectField(som.clip, typeof(AudioClip), false);
                if (novo != null && novo != som.clip)
                {
                    TrocarArquivo(som, novo);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Mostrar arquivo", EditorStyles.miniButton, GUILayout.Width(100)))
                    EditorGUIUtility.PingObject(som.clip);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Onde toca", estiloTitulo);
            foreach (Uso uso in som.usos)
                DesenharUso(uso);

            EditorGUILayout.Space(4);
            som.editando = EditorGUILayout.Foldout(som.editando, "Editar o arquivo (vale para todos os lugares)", true);
            if (som.editando)
                DesenharEdicao(som);
        }
    }

    private void DesenharUso(Uso uso)
    {
        if (uso.dono == null)
            return;
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(uso.descricao, GUILayout.MinWidth(160));

            if (!string.IsNullOrEmpty(uso.caminhoVolume) && uso.donoVolume != null)
            {
                SerializedObject so = new SerializedObject(uso.donoVolume);
                SerializedProperty p = so.FindProperty(uso.caminhoVolume);
                if (p != null)
                {
                    EditorGUI.BeginChangeCheck();
                    float v = EditorGUILayout.Slider(p.floatValue, 0f, 1f, GUILayout.Width(170));
                    if (EditorGUI.EndChangeCheck())
                    {
                        p.floatValue = v;
                        so.ApplyModifiedProperties();
                        EditorSceneManager.MarkSceneDirty(((Component)uso.donoVolume).gameObject.scene);
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("volume fixo no script", estiloCinza, GUILayout.Width(170));
            }

            if (GUILayout.Button("Mostrar", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                Selection.activeObject = ((Component)uso.dono).gameObject;
                EditorGUIUtility.PingObject(uso.dono);
            }
        }
    }

    private void DesenharEdicao(Som som)
    {
        EditorGUI.indentLevel++;
        som.ganhoDb = EditorGUILayout.Slider(new GUIContent("Mais alto / mais baixo", "Em decibéis. -6 é mais ou menos a metade do volume."), som.ganhoDb, -18f, 12f);
        som.cortarInicio = EditorGUILayout.Slider(new GUIContent("Cortar começo (s)", "Tira o silêncio ou o atraso do começo. Resolve som que toca atrasado."), som.cortarInicio, 0f, Mathf.Max(0.01f, som.clip.length - 0.05f));
        som.cortarFim = EditorGUILayout.Slider(new GUIContent("Cortar fim (s)", "Deixa o som mais curto."), som.cortarFim, 0f, Mathf.Max(0.01f, som.clip.length - som.cortarInicio - 0.05f));
        som.fadeFim = EditorGUILayout.Slider(new GUIContent("Suavizar o final (s)", "O som vai sumindo em vez de acabar seco."), som.fadeFim, 0f, Mathf.Max(0.01f, som.clip.length - som.cortarInicio - som.cortarFim));
        EditorGUI.indentLevel--;

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(16);
            if (GUILayout.Button("Ouvir como vai ficar"))
                OuvirEditado(som);
            if (GUILayout.Button("Aplicar no arquivo"))
            {
                if (EditorUtility.DisplayDialog("Editar o arquivo de som",
                    "\"" + Titulo(som) + "\" vai mudar em todos os lugares do jogo onde ele toca.\nO original fica guardado e dá para voltar com \"Voltar ao original\".",
                    "Aplicar", "Cancelar"))
                {
                    AplicarEdicao(som);
                    GUIUtility.ExitGUI();
                }
            }
            GUI.enabled = som.clips.Exists(TemBackup);
            if (GUILayout.Button("Voltar ao original"))
            {
                VoltarOriginal(som);
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
        }
    }

    // ------------------------------------------------------------------
    // Prévia
    // ------------------------------------------------------------------

    private static System.Type TipoAudioUtil => typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

    private static void TocarPrevia(AudioClip clip)
    {
        PararPrevia();
        MethodInfo m = TipoAudioUtil?.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        m?.Invoke(null, new object[] { clip, 0, false });
    }

    private static void PararPrevia()
    {
        TipoAudioUtil?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
    }

    private AudioClip previaEditada;

    private void OuvirEditado(Som som)
    {
        AudioClip alvo = som.clips[Random.Range(0, som.clips.Count)];
        if (!LerAmostras(alvo, out float[] dados, out int canais, out int frequencia))
            return;
        float[] novo = Processar(dados, canais, frequencia, som);
        if (previaEditada != null)
            DestroyImmediate(previaEditada);
        previaEditada = AudioClip.Create(alvo.name + " (prévia)", novo.Length / canais, canais, frequencia, false);
        previaEditada.SetData(novo, 0);
        TocarPrevia(previaEditada);
    }

    // ------------------------------------------------------------------
    // Edição do arquivo
    // ------------------------------------------------------------------

    /// <summary>Lê o som inteiro (descomprime por um instante se precisar).</summary>
    private static bool LerAmostras(AudioClip clip, out float[] dados, out int canais, out int frequencia)
    {
        dados = null;
        canais = clip.channels;
        frequencia = clip.frequency;
        string caminho = AssetDatabase.GetAssetPath(clip);
        AudioImporter imp = AssetImporter.GetAtPath(caminho) as AudioImporter;
        AudioImporterSampleSettings antes = imp != null ? imp.defaultSampleSettings : default;
        bool trocou = false;

        if (imp != null && antes.loadType != AudioClipLoadType.DecompressOnLoad)
        {
            AudioImporterSampleSettings temp = antes;
            temp.loadType = AudioClipLoadType.DecompressOnLoad;
            imp.defaultSampleSettings = temp;
            imp.SaveAndReimport();
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(caminho);
            trocou = true;
        }

        clip.LoadAudioData();
        dados = new float[clip.samples * clip.channels];
        bool ok = clip.GetData(dados, 0);

        if (trocou)
        {
            imp.defaultSampleSettings = antes;
            imp.SaveAndReimport();
        }

        if (!ok)
            EditorUtility.DisplayDialog("Sons do nível", "Não consegui ler \"" + clip.name + "\".", "Ok");
        return ok;
    }

    private static float[] Processar(float[] dados, int canais, int frequencia, Som som)
    {
        int quadros = dados.Length / canais;
        int inicio = Mathf.Clamp(Mathf.RoundToInt(som.cortarInicio * frequencia), 0, quadros - 1);
        int fim = Mathf.Clamp(quadros - Mathf.RoundToInt(som.cortarFim * frequencia), inicio + 1, quadros);
        int n = fim - inicio;
        float ganho = Mathf.Pow(10f, som.ganhoDb / 20f);
        int fade = Mathf.Min(n, Mathf.RoundToInt(som.fadeFim * frequencia));
        int fadeEntrada = som.cortarInicio > 0f ? Mathf.Min(n, Mathf.RoundToInt(0.005f * frequencia)) : 0; // evita estalo no corte

        float[] saida = new float[n * canais];
        for (int q = 0; q < n; q++)
        {
            float env = ganho;
            if (fade > 0 && q >= n - fade)
                env *= (n - q) / (float)fade;
            if (fadeEntrada > 0 && q < fadeEntrada)
                env *= q / (float)fadeEntrada;
            for (int c = 0; c < canais; c++)
                saida[q * canais + c] = Mathf.Clamp(dados[(inicio + q) * canais + c] * env, -1f, 1f);
        }
        return saida;
    }

    private void AplicarEdicao(Som som)
    {
        foreach (AudioClip c in new List<AudioClip>(som.clips))
            AplicarEdicaoNoArquivo(c, som);

        som.ganhoDb = 0f;
        som.cortarInicio = 0f;
        som.cortarFim = 0f;
        som.fadeFim = 0f;
        som.onda = null;
        Recarregar();
    }

    private void AplicarEdicaoNoArquivo(AudioClip clip, Som som)
    {
        if (!LerAmostras(clip, out float[] dados, out int canais, out int frequencia))
            return;

        string caminho = AssetDatabase.GetAssetPath(clip);
        string guid = AssetDatabase.AssetPathToGUID(caminho);
        Directory.CreateDirectory(PastaBackup);
        string backup = Path.Combine(PastaBackup, guid + ".wav");
        if (!File.Exists(backup))
            EscreverWav(backup, dados, canais, frequencia);

        float[] novo = Processar(dados, canais, frequencia, som);
        string caminhoWav = Path.ChangeExtension(caminho, ".wav");
        bool mudouExtensao = caminhoWav != caminho;

        if (mudouExtensao && File.Exists(caminhoWav))
        {
            EditorUtility.DisplayDialog("Sons do nível", "Já existe um arquivo \"" + Path.GetFileName(caminhoWav) + "\" nessa pasta.", "Ok");
            return;
        }

        EscreverWav(caminhoWav, novo, canais, frequencia);

        if (mudouExtensao)
            TrocarMp3PorWav(caminho, caminhoWav, backup);
        else
            AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);
    }

    private void VoltarOriginal(Som som)
    {
        foreach (AudioClip c in som.clips)
        {
            string caminho = AssetDatabase.GetAssetPath(c);
            string backup = Path.Combine(PastaBackup, AssetDatabase.AssetPathToGUID(caminho) + ".wav");
            if (!File.Exists(backup) || !caminho.EndsWith(".wav"))
                continue;
            File.Copy(backup, caminho, true);
            AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);
        }
        som.onda = null;
        Recarregar();
    }

    private static bool TemBackup(AudioClip clip)
    {
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip));
        return File.Exists(Path.Combine(PastaBackup, guid + ".wav"));
    }

    /// <summary>
    /// Um .mp3 editado vira .wav. As cenas e prefabs passam a apontar para o .wav
    /// e o .mp3 é apagado (o original continua no backup).
    /// </summary>
    private static void TrocarMp3PorWav(string caminhoMp3, string caminhoWav, string backupAntigo)
    {
        string cenaAberta = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();

        AssetDatabase.ImportAsset(caminhoWav, ImportAssetOptions.ForceUpdate);
        string guidVelho = AssetDatabase.AssetPathToGUID(caminhoMp3);
        string guidNovo = AssetDatabase.AssetPathToGUID(caminhoWav);

        foreach (string arquivo in Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories))
        {
            if (!(arquivo.EndsWith(".unity") || arquivo.EndsWith(".prefab") || arquivo.EndsWith(".asset")))
                continue;
            string texto = File.ReadAllText(arquivo);
            if (!texto.Contains(guidVelho))
                continue;
            File.WriteAllText(arquivo, texto.Replace("guid: " + guidVelho, "guid: " + guidNovo));
        }

        File.Copy(backupAntigo, Path.Combine(PastaBackup, guidNovo + ".wav"), true);
        AssetDatabase.DeleteAsset(caminhoMp3);
        AssetDatabase.Refresh();
        if (!string.IsNullOrEmpty(cenaAberta))
            EditorSceneManager.OpenScene(cenaAberta);
    }

    private static void EscreverWav(string caminho, float[] dados, int canais, int frequencia)
    {
        using (BinaryWriter w = new BinaryWriter(File.Create(caminho)))
        {
            int bytesDados = dados.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + bytesDados);
            w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);
            w.Write((short)canais);
            w.Write(frequencia);
            w.Write(frequencia * canais * 2);
            w.Write((short)(canais * 2));
            w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(bytesDados);
            foreach (float f in dados)
                w.Write((short)Mathf.RoundToInt(Mathf.Clamp(f, -1f, 1f) * 32767f));
        }
    }

    /// <summary>Troca o arquivo em todos os lugares desta cena que usam o som.</summary>
    private void TrocarArquivo(Som som, AudioClip novo)
    {
        foreach (Uso uso in som.usos)
        {
            if (uso.dono == null)
                continue;
            SerializedObject so = new SerializedObject(uso.dono);
            SerializedProperty p = so.FindProperty(uso.caminhoClip);
            if (p == null)
                continue;
            p.objectReferenceValue = novo;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(((Component)uso.dono).gameObject.scene);
        }
        Recarregar();
    }

    // ------------------------------------------------------------------
    // Forma de onda
    // ------------------------------------------------------------------

    private void GerarOnda(Som som)
    {
        if (!LerAmostras(som.clip, out float[] dados, out int canais, out int frequencia) || dados.Length == 0)
            return;

        const int largura = 512, altura = 48;
        Texture2D tex = new Texture2D(largura, altura, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
        Color fundo = new Color(0.16f, 0.16f, 0.16f), cor = new Color(0.95f, 0.7f, 0.24f);
        Color[] px = new Color[largura * altura];
        for (int i = 0; i < px.Length; i++)
            px[i] = fundo;

        int quadros = dados.Length / canais;
        double soma = 0;
        for (int x = 0; x < largura; x++)
        {
            int a = (int)((long)quadros * x / largura), b = (int)((long)quadros * (x + 1) / largura);
            float pico = 0f;
            for (int q = a; q < b; q++)
                for (int c = 0; c < canais; c++)
                {
                    float v = Mathf.Abs(dados[q * canais + c]);
                    if (v > pico)
                        pico = v;
                }
            int h = Mathf.Clamp(Mathf.RoundToInt(pico * (altura / 2)), 0, altura / 2);
            for (int y = altura / 2 - h; y <= altura / 2 + h && y < altura; y++)
                px[y * largura + x] = cor;
        }
        for (int i = 0; i < dados.Length; i++)
            soma += dados[i] * dados[i];

        tex.SetPixels(px);
        tex.Apply();
        som.onda = tex;
        som.volumeMedioDb = 10f * Mathf.Log10((float)(soma / dados.Length) + 1e-10f);
    }

    private static string DescreverVolume(float db)
    {
        if (db > -14f) return "muito alto";
        if (db > -20f) return "alto";
        if (db > -28f) return "normal";
        if (db > -36f) return "baixo";
        return "muito baixo";
    }

    // ------------------------------------------------------------------
    // Nomes
    // ------------------------------------------------------------------

    private static readonly Dictionary<string, string> nomesPorScript = new Dictionary<string, string>
    {
        { "GameManager", "Nível (geral)" },
        { "UIManager", "Interface" },
        { "CatracaFacial", "Catraca" },
        { "EventosBiblioteca", "Apagão da biblioteca" },
        { "ListaLivros", "Lista de livros" },
        { "TerminalConsulta", "Computador da consulta" },
        { "GuardaVolumes", "Armário da mochila" },
        { "PassosJogador", "Jogador" },
        { "LanternaCelular", "Lanterna" },
        { "SomAmbienteAleatorio", "Sons aleatórios" },
        { "Professora", "Professora" },
        { "ArmarioEsconderijo", "Armário esconderijo" },
        { "QuadroPerguntas", "Quadro do Abrantes" },
    };

    private static string UltimoNome(string caminho)
    {
        string limpo = Regex.Replace(caminho, @"\.Array\.data\[(\d+)\]", " $1");
        string[] partes = limpo.Split('.');
        return partes[partes.Length - 1];
    }

    private static string NomeDoObjeto(Transform t)
    {
        string nome = Regex.Replace(t.name, @"\s*\(.*?\)", "");
        if (nome.StartsWith("Area_"))
            nome = nome.Substring(5);
        if (nome.StartsWith("_"))
            nome = nome.Substring(1);
        nome = nome.Replace('_', ' ').Trim();
        if (nome == "GameManager")
            nome = "Nível (geral)";
        return nome;
    }
}
