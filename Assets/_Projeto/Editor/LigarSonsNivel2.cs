using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// FERRAMENTA DE EDITOR (não vai para o jogo): liga os sons novos do Nível 2 sozinha.
///
/// COMO USAR:
///  1. Baixe o som e salve na pasta Assets/_Projeto/Audio com o nome combinado
///     (pode ser .wav, .mp3 ou .ogg):
///       sfx_miado_gato       -> miado da gata Misty
///       sfx_poste_falhando   -> estalo/zumbido dos postes que piscam
///       sfx_portao_abrindo   -> portão de ferro da saída abrindo
///  2. Abra a cena Nivel2.
///  3. Menu "Escape UVV > Nível 2 > Ligar sons novos".
/// A janela no final mostra o que foi trocado. Arquivo que não existe é só ignorado.
/// </summary>
public static class LigarSonsNivel2
{
    private const string PASTA_AUDIO = "Assets/_Projeto/Audio/";

    [MenuItem("Escape UVV/Nível 2/Ligar sons novos")]
    public static void LigarSons()
    {
        if (EditorSceneManager.GetActiveScene().name != "Nivel2")
        {
            EditorUtility.DisplayDialog("Ligar sons novos", "Abra a cena Nivel2 primeiro.", "OK");
            return;
        }

        string relatorio = "";

        AudioClip miado = CarregarAudio("sfx_miado_gato");
        if (miado != null)
            foreach (GatoPatrulha gato in Object.FindObjectsByType<GatoPatrulha>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(gato, "Ligar sons");
                gato.somMiado = miado;
                relatorio += "Miado ligado em " + gato.name + "\n";
            }

        AudioClip poste = CarregarAudio("sfx_poste_falhando");
        if (poste != null)
            foreach (LuzPiscante luz in Object.FindObjectsByType<LuzPiscante>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(luz, "Ligar sons");
                luz.somFalha = poste;
                relatorio += "Som de falha ligado em " + luz.transform.parent.name + "\n";
            }

        AudioClip portao = CarregarAudio("sfx_portao_abrindo");
        if (portao != null)
            foreach (PortaDeslizante porta in Object.FindObjectsByType<PortaDeslizante>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(porta, "Ligar sons");
                porta.somAbrir = portao;
                relatorio += "Som de abrir ligado em " + porta.name + "\n";
            }

        if (relatorio == "")
            relatorio = "Nenhum som novo encontrado em " + PASTA_AUDIO + ".\nConfira o nome do arquivo.";
        else
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Ligar sons novos", relatorio + "\nNão esqueça de salvar a cena (Ctrl+S).", "OK");
    }

    /// <summary>Procura o áudio pelo nome aceitando .wav, .mp3 ou .ogg.</summary>
    /// <param name="nome">Nome do arquivo sem extensão.</param>
    /// <returns>O AudioClip ou null.</returns>
    private static AudioClip CarregarAudio(string nome)
    {
        foreach (string extensao in new[] { ".wav", ".mp3", ".ogg" })
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PASTA_AUDIO + nome + extensao);
            if (clip != null)
                return clip;
        }
        return null;
    }
}
