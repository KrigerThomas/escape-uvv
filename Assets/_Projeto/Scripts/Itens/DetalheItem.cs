using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mostra a imagem de um item de perto, no meio da tela (ex.: a carteirinha achada no Nível 2).
/// A tela de detalhe é criada pelo próprio script na primeira vez que é usada, então não precisa
/// montar nada na cena. Some sozinha depois de alguns segundos ou quando o jogador aperta E
/// ou clica.
/// </summary>
public class DetalheItem : MonoBehaviour
{
    private static DetalheItem instancia;

    [Tooltip("Tempo máximo que a imagem fica na tela, em segundos.")]
    public float tempoNaTela = 6f;

    [Tooltip("Tempo do fade de entrada e de saída.")]
    public float tempoFade = 0.3f;

    private CanvasGroup grupo;
    private RawImage imagem;
    private RectTransform caixa;
    private Coroutine rotina;

    /// <summary>Mostra a imagem no meio da tela. Cria a tela de detalhe se ela ainda não existe.</summary>
    /// <param name="textura">Imagem do item.</param>
    public static void Mostrar(Texture textura)
    {
        if (textura == null)
            return;

        if (instancia == null)
            instancia = Criar();

        instancia.Abrir(textura);
    }

    /// <summary>Monta o canvas com o fundo escuro, a imagem e a dica de como fechar.</summary>
    private static DetalheItem Criar()
    {
        // usa a mesma fonte do HUD do nível
        TextMeshProUGUI textoDoHud = FindFirstObjectByType<TextMeshProUGUI>();

        GameObject raiz = new GameObject("Tela_Detalhe_Item", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        CanvasScaler escala = raiz.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;

        DetalheItem detalhe = raiz.AddComponent<DetalheItem>();
        detalhe.grupo = raiz.AddComponent<CanvasGroup>();
        detalhe.grupo.alpha = 0f;
        detalhe.grupo.blocksRaycasts = false;

        // fundo escuro para destacar o item
        RectTransform fundo = NovoFilho("Fundo", raiz.transform);
        fundo.anchorMin = Vector2.zero;
        fundo.anchorMax = Vector2.one;
        fundo.offsetMin = Vector2.zero;
        fundo.offsetMax = Vector2.zero;
        fundo.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        detalhe.caixa = NovoFilho("Imagem", raiz.transform);
        detalhe.caixa.localRotation = Quaternion.Euler(0f, 0f, -3f);
        detalhe.imagem = detalhe.caixa.gameObject.AddComponent<RawImage>();

        RectTransform dica = NovoFilho("Dica", raiz.transform);
        dica.anchorMin = dica.anchorMax = new Vector2(0.5f, 0f);
        dica.anchoredPosition = new Vector2(0f, 110f);
        dica.sizeDelta = new Vector2(800f, 50f);
        TextMeshProUGUI texto = dica.gameObject.AddComponent<TextMeshProUGUI>();
        if (textoDoHud != null)
            texto.font = textoDoHud.font;
        texto.text = "E para guardar";
        texto.fontSize = 28f;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = new Color(0.6f, 0.64f, 0.68f);

        return detalhe;
    }

    /// <summary>Cria um filho com RectTransform no centro do pai.</summary>
    private static RectTransform NovoFilho(string nome, Transform pai)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        return (RectTransform)obj.transform;
    }

    /// <summary>Troca a imagem, ajusta a proporção e começa a mostrar.</summary>
    private void Abrir(Texture textura)
    {
        imagem.texture = textura;
        float proporcao = (float)textura.width / textura.height;
        caixa.sizeDelta = new Vector2(450f * proporcao, 450f);

        if (rotina != null)
            StopCoroutine(rotina);
        rotina = StartCoroutine(Mostrando());
    }

    /// <summary>Fade de entrada com um leve zoom, espera o tempo ou uma tecla e some.</summary>
    private IEnumerator Mostrando()
    {
        float t = 0f;
        while (t < tempoFade)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / tempoFade);
            grupo.alpha = k;
            caixa.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, k);
            yield return null;
        }

        float limite = Time.unscaledTime + tempoNaTela;
        while (Time.unscaledTime < limite)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                break;
            caixa.localScale += Vector3.one * (Time.unscaledDeltaTime * 0.006f);
            yield return null;
        }

        float inicio = grupo.alpha;
        t = 0f;
        while (t < tempoFade)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(inicio, 0f, t / tempoFade);
            yield return null;
        }
        grupo.alpha = 0f;
        rotina = null;
    }

    /// <summary>Limpa a referência estática quando a cena troca.</summary>
    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }
}
