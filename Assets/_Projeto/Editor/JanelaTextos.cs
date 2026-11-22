using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Janela "Escape UVV > Textos do nível": junta os textos da cena aberta, separados em abas
/// (legendas, objetivos, mensagens, telas pretas, dicas de tecla, perguntas e placas),
/// cada um com uma explicação de quando ele aparece no jogo.
/// Editou, salve a cena (Ctrl+S). Aceita Ctrl+Z.
/// </summary>
public class JanelaTextos : EditorWindow
{
    private enum Aba { Legendas, Objetivos, Mensagens, TelasPretas, Dicas, Perguntas, ListaLivros, Placas, Menus }

    private static readonly string[] nomesAbas =
    {
        "Legendas", "Objetivos", "Mensagens do celular", "Telas pretas", "Dicas de tecla", "Perguntas do quadro", "Lista de livros", "Placas do cenário", "Menus e botões"
    };

    private static readonly string[] explicacaoAbas =
    {
        "Falas e avisos que aparecem embaixo da tela durante a fase.",
        "Texto do canto de cima da tela que diz o que fazer agora.",
        "Mensagens dos colegas que chegam no canto da tela.",
        "Frases da tela preta: abertura do nível, recomeço e saída.",
        "O que aparece ao lado da tecla E quando o jogador chega perto de algo.",
        "Perguntas do quadro do Nível 3.",
        "Nomes dos livros e os lugares que podem aparecer na lista do Nível 4.",
        "Textos fixos do cenário: placas, etiquetas e telas.",
        "Tela de começo, pausa, botões e lista de controles."
    };

    /// <summary>Aba e explicação de cada campo conhecido (Classe.campo, sem os índices das listas).</summary>
    private static readonly Dictionary<string, (Aba aba, string quando)> conhecidos = new Dictionary<string, (Aba, string)>
    {
        // Geral
        { "GameManager.textoObjetivo", (Aba.Objetivos, "Objetivo no começo do nível") },
        { "GameManager.prefixoAviso", (Aba.Legendas, "Começo do aviso ao pegar um item (antes do nome do item)") },
        { "GameManager.textoPortaLiberada", (Aba.Legendas, "Quando todos os itens foram achados e a saída abre") },
        { "GameManager.textoPortaTrancada", (Aba.Legendas, "Ao tentar sair antes da hora (depois vem \"Faltam X itens\")") },
        { "GameManager.textoTudoEncontrado", (Aba.Legendas, "Quando acha tudo (só se a saída já começa aberta)") },
        { "ItemPegavel.mensagem", (Aba.Legendas, "Ao pegar este item (vazio = \"Você pegou: nome do item\")") },
        { "ItemPegavel.novoObjetivo", (Aba.Objetivos, "Objetivo depois de pegar este item (vazio = não muda)") },
        { "PortaComChave.textoTrancada", (Aba.Legendas, "Ao tentar abrir esta porta antes da hora") },
        { "PortaComChave.textoFaltaSegundoItem", (Aba.Legendas, "Ao tentar abrir quando só falta o segundo item") },
        { "Esconderijo.textoAoEntrar", (Aba.Legendas, "Ao entrar neste esconderijo") },
        { "Segredo.frase", (Aba.Legendas, "Frase que aparece junto com o nome deste item") },
        { "ItemColetavel.nomeItem", (Aba.Legendas, "Nome no aviso ao pegar (vem depois do \"Achei\")") },
        { "Interagivel.textoAcao", (Aba.Dicas, "Ao chegar perto (aparece com a tecla E)") },
        { "MensagensDoNivel.mensagens.remetente", (Aba.Mensagens, "Quem manda") },
        { "MensagensDoNivel.mensagens.texto", (Aba.Mensagens, "Mensagem") },
        { "TelaPreta.textoLugar", (Aba.TelasPretas, "Abertura do nível: nome do lugar no meio da tela") },
        { "TelaPreta.textoNivel", (Aba.TelasPretas, "Abertura do nível: linha pequena embaixo") },
        { "TelaPreta.textoRecomeco", (Aba.TelasPretas, "Ao recomeçar o nível depois de perder") },
        { "SaidaNivel.fraseSaida", (Aba.TelasPretas, "Ao sair do nível") },

        // Nível 2
        { "EsquiloFujao.textoPrimeiraFuga", (Aba.Legendas, "Quando o esquilo foge pela primeira vez") },
        { "EsquiloFujao.textoPresente", (Aba.Legendas, "Quando o esquilo cansa e deixa a bolota") },
        { "GatoCinza.textoDica", (Aba.Dicas, "Ao chegar perto do gato (aparece com a tecla E)") },
        { "GatoCinza.textosCarinho", (Aba.Legendas, "Ao fazer carinho no gato") },
        { "GatoCinza.textoPatada", (Aba.Legendas, "Quando o gato dá uma patada") },
        { "GirassolSegueJogador.textoAviso", (Aba.Legendas, "Quando o girassol começa a seguir o jogador") },

        // Nível 3
        { "QuadroPerguntas.perguntas.enunciado", (Aba.Perguntas, "Pergunta") },
        { "QuadroPerguntas.perguntas.explicacao", (Aba.Perguntas, "Explicação depois de responder") },

        // Nível 4
        { "CatracaFacial.textoMochila", (Aba.Legendas, "Catraca barra o jogador com a mochila") },
        { "CatracaFacial.textoAlarme", (Aba.Legendas, "Catraca apita ao sair com livro sem empréstimo") },
        { "CatracaFacial.textoLendoRosto", (Aba.Legendas, "Enquanto a catraca lê o rosto") },
        { "CatracaFacial.textoEntrada", (Aba.Legendas, "Quando a catraca libera a entrada") },
        { "CatracaFacial.textoSaida", (Aba.Legendas, "Quando a catraca libera a saída") },
        { "GuardaVolumes.textoAcaoGuardar", (Aba.Dicas, "Ao chegar perto com a mochila") },
        { "GuardaVolumes.textoAcaoPegar", (Aba.Dicas, "Ao chegar perto para pegar a mochila") },
        { "ArmarioEsconderijo.textoAcaoEntrar", (Aba.Dicas, "Ao chegar perto do armário") },
        { "ArmarioEsconderijo.textoAcaoSair", (Aba.Dicas, "Dentro do armário") },
        { "ArmarioEsconderijo.textoSemTempo", (Aba.Legendas, "Ao tentar se esconder com a professora olhando") },
        { "QuadroPerguntas.textoCerto", (Aba.Perguntas, "Quando acerta (depois vem a explicação)") },
        { "QuadroPerguntas.textoErrado", (Aba.Perguntas, "Quando erra") },
        { "GuardaVolumes.textoGuardou", (Aba.Legendas, "Ao guardar a mochila no armário") },
        { "GuardaVolumes.textoAindaNao", (Aba.Legendas, "Ao tentar pegar a mochila antes da hora") },
        { "GuardaVolumes.textoPegou", (Aba.Legendas, "Ao pegar a mochila de volta") },
        { "GuardaVolumes.objetivoDepoisDeGuardar", (Aba.Objetivos, "Depois de guardar a mochila") },
        { "GuardaVolumes.objetivoDepoisDePegar", (Aba.Objetivos, "Depois de pegar a mochila de volta") },
        { "TerminalConsulta.textoImpresso", (Aba.Legendas, "Quando a lista sai na impressora (e o computador trava)") },
        { "TerminalConsulta.objetivoDepois", (Aba.Objetivos, "Depois de consultar o computador") },
        { "Livro.titulo", (Aba.ListaLivros, "Nome do livro (na lista e ao pegar)") },
        { "SorteioLivros.livros.lugares.descricao", (Aba.ListaLivros, "Lugar possível") },
        { "EventosBiblioteca.avisoApagao", (Aba.Legendas, "Fala quando a luz cai") },
        { "EventosBiblioteca.objetivoApagao", (Aba.Objetivos, "Depois que a luz cai") },
        { "EventosBiblioteca.textoPortaDepoisDoApagao", (Aba.Legendas, "Porta de saída depois do apagão, se ainda está sem a mochila") },

        // Nível 5
        { "Professora.textoAvistou", (Aba.Legendas, "Quando a professora vê o jogador") },
        { "Professora.textoDesistiu", (Aba.Legendas, "Quando a professora desiste de procurar") },
        { "Professora.textoPortaAbriu", (Aba.Legendas, "Quando a professora ouve a porta de saída") },
        { "Professora.textoPegou", (Aba.TelasPretas, "Quando a professora pega o jogador") },
    };

    /// <summary>Campos que só guardam dados do jogo (não aparecem na tela).</summary>
    private static readonly HashSet<string> ignorados = new HashSet<string>
    {
        "SorteioLivros.livros.chamada", "SorteioLivros.livros.lugarSorteado", "CatShowcase.cats.displayName"
    };

    /// <summary>Textos da interface que os scripts preenchem sozinhos (editar aqui não adianta).</summary>
    private static readonly string[] interfacePreenchida =
    {
        "Texto_Objetivo", "Texto_Contador", "Texto_Acao", "Texto_Tecla", "Texto_Aviso", "Texto_Mensagem",
        "Texto_Remetente", "Texto_Quando", "Texto_Iniciais", "Texto_Pausa_Objetivo"
    };

    private class Campo
    {
        public Object dono;
        public string caminho;
        public Aba aba;
        public string quando;
        public string objeto;
        public int ordem;
        public string chave;
        public List<Object> iguais = new List<Object>();
    }

    /// <summary>Scripts que escolhem a própria dica do E (o campo "textoAcao" deles não aparece no jogo).</summary>
    private static readonly HashSet<string> dicaPropria = new HashSet<string> { "ArmarioEsconderijo", "GuardaVolumes" };

    private readonly List<Campo> campos = new List<Campo>();
    private Aba aba = Aba.Legendas;
    private string busca = "";
    private bool mostrarVazios;
    private Vector2 rolagem;
    private GUIStyle estiloQuando;
    private GUIStyle estiloObjeto;
    private GUIStyle estiloTexto;

    /// <summary>Abre a janela pelo menu Escape UVV e lê a cena aberta.</summary>
    [MenuItem("Escape UVV/Textos do nível")]
    public static void Abrir()
    {
        JanelaTextos janela = GetWindow<JanelaTextos>("Textos do nível");
        janela.minSize = new Vector2(460, 320);
        janela.Recarregar();
    }

    /// <summary>Passa a ouvir a troca de cena e monta a lista.</summary>
    void OnEnable()
    {
        EditorSceneManager.sceneOpened -= AoAbrirCena;
        EditorSceneManager.sceneOpened += AoAbrirCena;
        Recarregar();
    }

    /// <summary>Para de ouvir a troca de cena quando a janela fecha.</summary>
    void OnDisable()
    {
        EditorSceneManager.sceneOpened -= AoAbrirCena;
    }

    /// <summary>Quando outra cena é aberta, monta a lista de novo.</summary>
    /// <param name="cena">Cena que acabou de abrir (não usada).</param>
    /// <param name="modo">Como a cena foi aberta (não usado).</param>
    private void AoAbrirCena(UnityEngine.SceneManagement.Scene cena, OpenSceneMode modo)
    {
        Recarregar();
        Repaint();
    }

    /// <summary>Textos da interface que algum script preenche sozinho durante o jogo.</summary>
    private readonly HashSet<Object> preenchidosPorScript = new HashSet<Object>();

    /// <summary>Procura de novo todos os textos da cena aberta.</summary>
    private void Recarregar()
    {
        campos.Clear();
        int ordem = 0;
        AcharPreenchidosPorScript();

        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null || EditorUtility.IsPersistent(mb))
                continue;

            if (mb is TMP_Text tmp)
            {
                AdicionarPlaca(tmp, ordem++);
                continue;
            }

            if (mb.GetType().Assembly.GetName().Name != "Assembly-CSharp")
                continue;

            SerializedObject so = new SerializedObject(mb);
            SerializedProperty it = so.GetIterator();
            bool entrar = true;
            while (it.NextVisible(entrar))
            {
                entrar = it.propertyType == SerializedPropertyType.Generic;
                if (it.propertyType != SerializedPropertyType.String || it.propertyPath.StartsWith("m_"))
                    continue;

                string chave = Chave(mb, it.propertyPath);
                if (chave == null || ignorados.Contains(chave))
                    continue;
                if (chave == "Interagivel.textoAcao" && dicaPropria.Contains(mb.GetType().Name))
                    continue;

                Aba abaCampo;
                string quando;
                if (conhecidos.TryGetValue(chave, out var info))
                {
                    abaCampo = info.aba;
                    quando = info.quando;
                }
                else if (PareceTextoDeTela(it.propertyPath))
                {
                    abaCampo = it.propertyPath.ToLower().Contains("objetivo") ? Aba.Objetivos : Aba.Legendas;
                    quando = ObjectNames.NicifyVariableName(UltimoNome(it.propertyPath));
                }
                else
                {
                    continue;
                }

                int indice = UltimoIndice(it.propertyPath);
                if (indice >= 0 && abaCampo != Aba.Mensagens && abaCampo != Aba.Perguntas)
                    quando += " " + (indice + 1);

                campos.Add(new Campo
                {
                    dono = mb,
                    caminho = it.propertyPath,
                    aba = abaCampo,
                    quando = quando,
                    objeto = NomeDoDono(mb, so, it.propertyPath),
                    ordem = ordem++,
                    chave = mb.GetType().Name + "|" + chave + "|" + it.stringValue
                });
            }
        }

        AdicionarPrefabsSorteados(ref ordem);
        JuntarRepetidos();

        campos.Sort((a, b) =>
        {
            int c = string.Compare(a.objeto, b.objeto, System.StringComparison.CurrentCulture);
            return c != 0 ? c : a.ordem.CompareTo(b.ordem);
        });
    }

    /// <summary>Placas e telas do cenário (os textos da interface preenchidos por script ficam de fora).</summary>
    private void AdicionarPlaca(TMP_Text tmp, int ordem)
    {
        foreach (string nome in interfacePreenchida)
            if (tmp.name == nome)
                return;
        // Textos que algum script troca durante o jogo (ex.: resultado do quadro) não adianta editar aqui.
        if (preenchidosPorScript.Contains(tmp))
            return;

        bool menu = tmp is TextMeshProUGUI && tmp.canvas != null && tmp.canvas.renderMode != RenderMode.WorldSpace;
        Transform alvo = tmp.transform.parent != null && (tmp.name.StartsWith("Texto") || tmp.name == "Titulo" || tmp.name == "Subtitulo") ? tmp.transform.parent : tmp.transform;

        campos.Add(new Campo
        {
            dono = tmp,
            caminho = "m_text",
            aba = menu ? Aba.Menus : Aba.Placas,
            quando = menu ? DescricaoMenu(tmp) : "Texto escrito",
            objeto = NomeAmigavel(alvo),
            ordem = ordem
        });
    }

    /// <summary>Desenha a janela: abas por tipo de texto e uma caixa editável para cada texto.</summary>
    void OnGUI()
    {
        if (estiloQuando == null)
        {
            estiloQuando = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };
            estiloObjeto = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.55f, 0.55f, 0.55f) } };
            estiloTexto = new GUIStyle(EditorStyles.textArea) { wordWrap = true, fontSize = 13 };
        }

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(EditorSceneManager.GetActiveScene().name, EditorStyles.largeLabel);
            if (GUILayout.Button("Atualizar lista", GUILayout.Width(110)))
                Recarregar();
        }

        aba = (Aba)GUILayout.SelectionGrid((int)aba, nomesAbas, 4);
        EditorGUILayout.HelpBox(explicacaoAbas[(int)aba] + "\nDepois de editar, salve a cena com Ctrl+S.", MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            busca = EditorGUILayout.TextField("Procurar", busca);
            mostrarVazios = GUILayout.Toggle(mostrarVazios, "Mostrar vazios", GUILayout.Width(110));
        }

        rolagem = EditorGUILayout.BeginScrollView(rolagem);
        string ultimoObjeto = null;
        int mostrados = 0;

        foreach (Campo c in campos)
        {
            if (c.dono == null || c.aba != aba)
                continue;

            SerializedObject so = new SerializedObject(c.dono);
            SerializedProperty p = so.FindProperty(c.caminho);
            if (p == null)
                continue;

            // "interagir" é o padrão: o próprio script troca por um texto melhor no jogo.
            bool vazio = string.IsNullOrEmpty(p.stringValue) || c.aba == Aba.Dicas && p.stringValue == "interagir";
            if (!mostrarVazios && vazio)
                continue;

            if (!string.IsNullOrEmpty(busca)
                && p.stringValue.IndexOf(busca, System.StringComparison.OrdinalIgnoreCase) < 0
                && c.objeto.IndexOf(busca, System.StringComparison.OrdinalIgnoreCase) < 0
                && c.quando.IndexOf(busca, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            mostrados++;
            bool novoObjeto = c.objeto != ultimoObjeto;
            if (novoObjeto)
            {
                EditorGUILayout.Space(10);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(c.objeto, estiloObjeto);
                    if (GUILayout.Button("Mostrar na cena", EditorStyles.miniButton, GUILayout.Width(100)))
                    {
                        Selection.activeObject = EditorUtility.IsPersistent(c.dono) ? (Object)((Component)c.dono).transform.root.gameObject : ((Component)c.dono).gameObject;
                        EditorGUIUtility.PingObject(c.dono);
                    }
                }
                ultimoObjeto = c.objeto;
            }
            else
            {
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.LabelField(c.quando, estiloQuando);
            EditorGUI.BeginChangeCheck();
            string novo = EditorGUILayout.TextArea(p.stringValue, estiloTexto);
            if (EditorGUI.EndChangeCheck())
            {
                p.stringValue = novo;
                so.ApplyModifiedProperties();
                foreach (Object outro in c.iguais)
                {
                    if (outro == null)
                        continue;
                    SerializedObject soOutro = new SerializedObject(outro);
                    SerializedProperty pOutro = soOutro.FindProperty(c.caminho);
                    if (pOutro == null)
                        continue;
                    pOutro.stringValue = novo;
                    soOutro.ApplyModifiedProperties();
                }
                if (EditorUtility.IsPersistent(c.dono))
                {
                    EditorUtility.SetDirty(c.dono);
                    AssetDatabase.SaveAssetIfDirty(c.dono);
                }
                else
                {
                    EditorSceneManager.MarkSceneDirty(((Component)c.dono).gameObject.scene);
                }
            }
        }

        if (mostrados == 0)
            EditorGUILayout.HelpBox("Nenhum texto desta aba nesta cena.", MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    // ---------- ajudantes ----------

    /// <summary>
    /// Itens que o nível sorteia (prefabs) não estão na cena até o jogo começar.
    /// Os textos deles entram na lista pelo prefab que a cena usa.
    /// </summary>
    private void AdicionarPrefabsSorteados(ref int ordem)
    {
        HashSet<GameObject> prefabs = new HashSet<GameObject>();
        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null || mb is TMP_Text || mb.GetType().Assembly.GetName().Name != "Assembly-CSharp")
                continue;
            SerializedProperty it = new SerializedObject(mb).GetIterator();
            bool entrar = true;
            while (it.NextVisible(entrar))
            {
                entrar = it.propertyType == SerializedPropertyType.Generic;
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue is GameObject go
                    && EditorUtility.IsPersistent(go) && AssetDatabase.GetAssetPath(go).StartsWith("Assets/_Projeto/Prefabs/"))
                    prefabs.Add(go);
            }
        }

        foreach (GameObject prefab in prefabs)
        {
            foreach (MonoBehaviour mb in prefab.GetComponents<MonoBehaviour>())
            {
                if (mb == null || mb.GetType().Assembly.GetName().Name != "Assembly-CSharp")
                    continue;
                SerializedObject so = new SerializedObject(mb);
                SerializedProperty it = so.GetIterator();
                bool entrar = true;
                while (it.NextVisible(entrar))
                {
                    entrar = false;
                    if (it.propertyType != SerializedPropertyType.String)
                        continue;
                    string chave = mb.GetType().Name + "." + it.propertyPath;
                    if (!conhecidos.TryGetValue(chave, out var info))
                        continue;
                    campos.Add(new Campo
                    {
                        dono = mb,
                        caminho = it.propertyPath,
                        aba = info.aba,
                        quando = info.quando,
                        objeto = "Item sorteado: " + NomeAmigavel(prefab.transform).Replace("Item ", "").Replace("Segredo ", ""),
                        ordem = ordem++
                    });
                }
            }
        }
    }

    /// <summary>
    /// Vários objetos iguais com o mesmo texto (ex.: os 16 armários) viram uma linha só.
    /// Editar essa linha muda o texto em todos eles.
    /// </summary>
    private void JuntarRepetidos()
    {
        Dictionary<string, Campo> primeiro = new Dictionary<string, Campo>();
        List<Campo> final = new List<Campo>();
        foreach (Campo c in campos)
        {
            if (c.chave == null || c.aba == Aba.Placas || c.aba == Aba.Menus || c.aba == Aba.ListaLivros)
            {
                final.Add(c);
                continue;
            }
            if (primeiro.TryGetValue(c.chave, out Campo dono))
            {
                dono.iguais.Add(c.dono);
                continue;
            }
            primeiro[c.chave] = c;
            final.Add(c);
        }
        foreach (Campo c in final)
            if (c.iguais.Count > 0)
                c.objeto += " (" + (c.iguais.Count + 1) + " iguais)";
        campos.Clear();
        campos.AddRange(final);
    }

    /// <summary>Textos de UI ligados a scripts do projeto (o script escreve neles durante o jogo).</summary>
    private void AcharPreenchidosPorScript()
    {
        preenchidosPorScript.Clear();
        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null || mb is TMP_Text || mb.GetType().Assembly.GetName().Name != "Assembly-CSharp")
                continue;
            SerializedProperty it = new SerializedObject(mb).GetIterator();
            bool entrar = true;
            while (it.NextVisible(entrar))
            {
                entrar = it.propertyType == SerializedPropertyType.Generic;
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue is TMP_Text alvo)
                    preenchidosPorScript.Add(alvo);
            }
        }
    }

    /// <summary>Diz que tipo de texto de menu é este (botão, título, lista de teclas...).</summary>
    /// <param name="tmp">Texto da interface.</param>
    /// <returns>Descrição curta mostrada na janela.</returns>
    private static string DescricaoMenu(TMP_Text tmp)
    {
        string pai = tmp.transform.parent != null ? tmp.transform.parent.name : "";
        if (pai.StartsWith("Botao") || tmp.name.StartsWith("Botao"))
            return "Texto do botão";
        if (tmp.name == "Titulo")
            return "Título";
        if (tmp.name == "Controles")
            return "Lista de teclas";
        if (pai.StartsWith("Painel_Intro") || tmp.name == "Texto_Intro")
            return "Texto da tela de começo";
        return "Texto";
    }

    /// <summary>"Classe.campo" sem os índices das listas (ex.: MensagensDoNivel.mensagens.texto).</summary>
    private static string Chave(MonoBehaviour mb, string caminho)
    {
        string semIndices = Regex.Replace(caminho, @"\.Array\.data\[\d+\]", "");
        string primeiro = semIndices.Split('.')[0];

        System.Type tipo = mb.GetType();
        while (tipo != null && tipo != typeof(MonoBehaviour))
        {
            if (tipo.GetField(primeiro, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly) != null)
                return tipo.Name + "." + semIndices;
            tipo = tipo.BaseType;
        }
        return null;
    }

    private static readonly string[] palavrasDeTexto = { "texto", "mensagem", "objetivo", "aviso", "frase", "legenda" };

    /// <summary>Confere pelo nome do campo se ele guarda um texto que aparece no jogo.</summary>
    /// <param name="caminho">Caminho da propriedade serializada.</param>
    /// <returns>Verdadeiro se o nome parece de texto de tela.</returns>
    private static bool PareceTextoDeTela(string caminho)
    {
        string nome = UltimoNome(caminho).ToLower();
        foreach (string palavra in palavrasDeTexto)
            if (nome.Contains(palavra))
                return true;
        return false;
    }

    /// <summary>Pega só o nome do campo no fim de um caminho de propriedade.</summary>
    /// <param name="caminho">Caminho da propriedade serializada.</param>
    /// <returns>Último nome do caminho.</returns>
    private static string UltimoNome(string caminho)
    {
        string[] partes = caminho.Split('.');
        return partes[partes.Length - 1];
    }

    /// <summary>Pega o último índice de lista de um caminho de propriedade.</summary>
    /// <param name="caminho">Caminho da propriedade serializada.</param>
    /// <returns>O índice, ou -1 se o caminho não tem lista.</returns>
    private static int UltimoIndice(string caminho)
    {
        MatchCollection ms = Regex.Matches(caminho, @"\[(\d+)\]");
        return ms.Count > 0 ? int.Parse(ms[ms.Count - 1].Groups[1].Value) : -1;
    }

    /// <summary>Nome claro de quem "fala" o texto, pelo tipo do script (ou pelo objeto).</summary>
    private static readonly Dictionary<string, string> nomesPorScript = new Dictionary<string, string>
    {
        { "GameManager", "Nível (geral)" },
        { "MensagensDoNivel", "Mensagens do celular" },
        { "TelaPreta", "Tela preta" },
        { "SaidaNivel", "Saída do nível" },
        { "EventosBiblioteca", "Apagão da biblioteca" },
        { "CatracaFacial", "Catraca" },
        { "GuardaVolumes", "Armário da mochila" },
        { "TerminalConsulta", "Computador da consulta" },
        { "Professora", "Professora" },
        { "QuadroPerguntas", "Quadro de perguntas" },
    };

    /// <summary>Descobre quem "fala" o texto, para agrupar na janela.</summary>
    /// <param name="mb">Script que guarda o texto.</param>
    /// <param name="so">Versão serializada do script.</param>
    /// <param name="caminho">Caminho da propriedade do texto.</param>
    /// <returns>Nome do dono do texto.</returns>
    private static string NomeDoDono(MonoBehaviour mb, SerializedObject so, string caminho)
    {
        // Na lista sorteada, o grupo é o próprio livro.
        if (mb.GetType().Name == "SorteioLivros")
        {
            Match m = Regex.Match(caminho, @"^livros\.Array\.data\[(\d+)\]");
            if (m.Success)
            {
                SerializedProperty livro = so.FindProperty("livros.Array.data[" + m.Groups[1].Value + "].livro");
                if (livro != null && livro.objectReferenceValue != null)
                {
                    SerializedProperty titulo = new SerializedObject(livro.objectReferenceValue).FindProperty("titulo");
                    if (titulo != null)
                        return "Lugares do livro " + titulo.stringValue;
                }
            }
        }

        if (nomesPorScript.TryGetValue(mb.GetType().Name, out string nome))
            return nome;
        return NomeAmigavel(mb.transform);
    }

    /// <summary>Nome do objeto legível: "Area_Porta_Saida (precisa da chave)" vira "Porta Saida".</summary>
    private static string NomeAmigavel(Transform t)
    {
        string nome = Regex.Replace(t.name, @"\s*\(.*?\)", "");
        if (nome.StartsWith("Area_"))
            nome = nome.Substring(5);
        if (nome.StartsWith("_"))
            nome = nome.Substring(1);
        nome = nome.Replace('_', ' ').Trim();
        if (nome == "GameManager")
            nome = "Nível (geral)";
        if (nome.StartsWith("Botao "))
            nome = "Botão " + nome.Substring(6);
        if (nome == "Painel Intro")
            nome = "Tela de começo";
        return nome;
    }
}
