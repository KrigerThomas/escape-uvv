using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Janela "Escape UVV > Textos do nível": junta num lugar só todos os textos da cena aberta
/// (avisos, objetivos, mensagens, dicas, placas e textos da interface) para editar sem caçar
/// cada objeto na Hierarchy. Mexeu, a cena fica marcada para salvar (Ctrl+S). Aceita Ctrl+Z.
/// </summary>
public class JanelaTextos : EditorWindow
{
    private string busca = "";
    private bool mostrarScripts = true;
    private bool mostrarPlacas = true;
    private Vector2 rolagem;

    private class Campo
    {
        public Object dono;
        public string caminho;
        public string rotulo;
    }

    private readonly List<Campo> campos = new List<Campo>();

    [MenuItem("Escape UVV/Textos do nível")]
    public static void Abrir()
    {
        JanelaTextos janela = GetWindow<JanelaTextos>("Textos do nível");
        janela.minSize = new Vector2(420, 300);
        janela.Recarregar();
    }

    void OnEnable()
    {
        EditorSceneManager.sceneOpened += (cena, modo) => Recarregar();
        Recarregar();
    }

    void OnHierarchyChange()
    {
        Repaint();
    }

    /// <summary>Procura de novo todos os textos da cena aberta.</summary>
    private void Recarregar()
    {
        campos.Clear();

        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null || EditorUtility.IsPersistent(mb))
                continue;

            bool ehTexto = mb is TMP_Text;
            bool ehDoProjeto = mb.GetType().Assembly.GetName().Name == "Assembly-CSharp";
            if (!ehTexto && !ehDoProjeto)
                continue;

            SerializedObject so = new SerializedObject(mb);
            SerializedProperty it = so.GetIterator();
            bool entrar = true;
            while (it.NextVisible(entrar))
            {
                entrar = it.propertyType == SerializedPropertyType.Generic || it.isArray && it.propertyType != SerializedPropertyType.String;
                if (it.propertyType != SerializedPropertyType.String)
                    continue;
                if (ehTexto && it.propertyPath != "m_text")
                    continue;
                // Só campos de texto mostrados ao jogador. Nomes de itens (ex.: "Mochila") ficam de fora,
                // porque mudar um deles quebraria a lógica do nível.
                if (!ehTexto && !EhTextoDoJogador(it.propertyPath))
                    continue;

                campos.Add(new Campo
                {
                    dono = mb,
                    caminho = it.propertyPath,
                    rotulo = ehTexto ? "Texto" : ObjectNames.NicifyVariableName(it.propertyPath.Replace(".Array.data", ""))
                });
            }
        }

        campos.Sort((a, b) => string.Compare(Caminho(((Component)a.dono).transform), Caminho(((Component)b.dono).transform), System.StringComparison.Ordinal));
    }

    void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Cena: " + EditorSceneManager.GetActiveScene().name, EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            busca = EditorGUILayout.TextField("Procurar", busca);
            if (GUILayout.Button("Atualizar", GUILayout.Width(80)))
                Recarregar();
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            mostrarScripts = GUILayout.Toggle(mostrarScripts, "Avisos, objetivos e mensagens", "Button");
            mostrarPlacas = GUILayout.Toggle(mostrarPlacas, "Placas e interface", "Button");
        }
        EditorGUILayout.HelpBox("Edite aqui e salve a cena (Ctrl+S). Textos entre < > (ex.: <b>) são formatação do TextMeshPro.", MessageType.None);

        rolagem = EditorGUILayout.BeginScrollView(rolagem);
        Object ultimoDono = null;
        foreach (Campo c in campos)
        {
            if (c.dono == null)
                continue;

            bool placa = c.dono is TMP_Text;
            if (placa && !mostrarPlacas || !placa && !mostrarScripts)
                continue;

            SerializedObject so = new SerializedObject(c.dono);
            SerializedProperty p = so.FindProperty(c.caminho);
            if (p == null)
                continue;

            string objeto = Caminho(((Component)c.dono).transform);
            if (!string.IsNullOrEmpty(busca)
                && p.stringValue.IndexOf(busca, System.StringComparison.OrdinalIgnoreCase) < 0
                && objeto.IndexOf(busca, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (c.dono != ultimoDono)
            {
                EditorGUILayout.Space(6);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(objeto + (placa ? "" : "  (" + c.dono.GetType().Name + ")"), EditorStyles.miniBoldLabel);
                    if (GUILayout.Button("Mostrar", EditorStyles.miniButton, GUILayout.Width(60)))
                        EditorGUIUtility.PingObject(c.dono);
                }
                ultimoDono = c.dono;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField(c.rotulo, EditorStyles.miniLabel);
            string novo = EditorGUILayout.TextArea(p.stringValue, EditorStyles.textArea);
            if (EditorGUI.EndChangeCheck())
            {
                p.stringValue = novo;
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(((Component)c.dono).gameObject.scene);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private static readonly string[] palavrasDeTexto = { "texto", "mensagem", "objetivo", "aviso", "descricao", "titulo", "frase", "legenda", "remetente" };

    /// <summary>Verdadeiro se o nome do campo indica um texto que aparece na tela.</summary>
    private static bool EhTextoDoJogador(string caminho)
    {
        string nome = caminho.ToLower();
        if (nome.StartsWith("m_") || nome.Contains("resultado"))
            return false;
        foreach (string palavra in palavrasDeTexto)
            if (nome.Contains(palavra))
                return true;
        return false;
    }

    private static string Caminho(Transform t)
    {
        string s = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            s = t.name + " / " + s;
        }
        return s;
    }
}
