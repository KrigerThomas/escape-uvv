using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Servidor HTTP local embutido no Unity Editor que atua como Bridge para o MCP.
/// Escuta na porta 8080 e executa ações na Main Thread da Unity de forma segura.
/// </summary>
[InitializeOnLoad]
public static class UnityMcpBridge
{
    private static HttpListener listener;
    private static readonly ConcurrentQueue<Action> acoesMainThread = new ConcurrentQueue<Action>();
    private const int Porta = 8080;
    private static bool rodando = false;
    private static readonly List<string> logsRecentes = new List<string>();

    static UnityMcpBridge()
    {
        EditorApplication.update += AtualizarMainThread;
        Application.logMessageReceived += AoReceberLog;
        IniciarServidor();
    }

    private static void AoReceberLog(string log, string stackTrace, LogType type)
    {
        lock (logsRecentes)
        {
            logsRecentes.Add($"[{type}] {log}");
            if (logsRecentes.Count > 30)
                logsRecentes.RemoveAt(0);
        }
    }

    private static void IniciarServidor()
    {
        if (rodando) return;

        try
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{Porta}/");
            listener.Prefixes.Add($"http://localhost:{Porta}/");
            listener.Start();
            rodando = true;
            listener.BeginGetContext(AoReceberRequisicao, listener);
            Debug.Log($"<color=cyan>[Unity MCP Bridge]</color> Ativo em http://localhost:{Porta}/");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Unity MCP Bridge] Aviso: {e.Message}");
        }
    }

    private static void AtualizarMainThread()
    {
        while (acoesMainThread.TryDequeue(out Action acao))
        {
            try { acao?.Invoke(); } catch (Exception ex) { Debug.LogError($"[Unity MCP Bridge] Erro: {ex.Message}"); }
        }
    }

    private static void AoReceberRequisicao(IAsyncResult result)
    {
        if (!rodando || listener == null) return;

        HttpListenerContext context = null;
        try
        {
            context = listener.EndGetContext(result);
        }
        catch { return; }
        finally
        {
            if (rodando && listener.IsListening)
                listener.BeginGetContext(AoReceberRequisicao, listener);
        }

        if (context == null) return;

        string path = context.Request.Url.AbsolutePath.ToLower();
        string method = context.Request.HttpMethod;

        if (path == "/ping")
        {
            ResponderJson(context, "{\"status\":\"ok\",\"unity_version\":\"" + Application.unityVersion + "\"}");
            return;
        }

        if (path == "/console")
        {
            string logs;
            lock (logsRecentes) { logs = string.Join("\n", logsRecentes); }
            ResponderJson(context, "{\"logs\":" + JsonUtility.ToJson(logs) + "}");
            return;
        }

        acoesMainThread.Enqueue(() =>
        {
            try
            {
                if (path == "/hierarchy")
                {
                    List<string> lista = new List<string>();
                    var raiz = SceneManager.GetActiveScene().GetRootGameObjects();
                    foreach (var go in raiz) ListarHierarquia(go.transform, 0, lista);
                    ResponderJson(context, "{\"hierarchy\":[" + string.Join(",", lista) + "]}");
                }
                else if (path == "/menu" && method == "POST")
                {
                    using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                    string body = reader.ReadToEnd();
                    string menuPath = ExtrairValorJson(body, "menu");
                    bool ok = EditorApplication.ExecuteMenuItem(menuPath);
                    ResponderJson(context, "{\"executed\":" + (ok ? "true" : "false") + "}");
                }
                else
                {
                    ResponderJson(context, "{\"error\":\"Endpoint desconhecido\"}", 404);
                }
            }
            catch (Exception ex)
            {
                ResponderJson(context, "{\"error\":\"" + ex.Message + "\"}", 500);
            }
        });
    }

    private static void ListarHierarquia(Transform t, int nivel, List<string> lista)
    {
        string indent = new string('-', nivel * 2);
        lista.Add("\"" + indent + t.gameObject.name + "\"");
        for (int i = 0; i < t.childCount; i++)
            ListarHierarquia(t.GetChild(i), nivel + 1, lista);
    }

    private static string ExtrairValorJson(string json, string chave)
    {
        int idx = json.IndexOf("\"" + chave + "\"");
        if (idx == -1) return "";
        int doisPontos = json.IndexOf(':', idx);
        if (doisPontos == -1) return "";
        int aspasIni = json.IndexOf('"', doisPontos);
        if (aspasIni == -1) return "";
        int aspasFim = json.IndexOf('"', aspasIni + 1);
        if (aspasFim == -1) return "";
        return json.Substring(aspasIni + 1, aspasFim - aspasIni - 1);
    }

    private static void ResponderJson(HttpListenerContext ctx, string json, int statusCode = 200)
    {
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }
        catch { }
    }
}
