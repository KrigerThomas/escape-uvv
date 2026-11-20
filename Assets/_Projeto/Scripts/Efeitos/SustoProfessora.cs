using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Susto quando a Professora pega o jogador (Nível 5), no estilo de jogos como Fears to Fathom:
/// a câmera vira de uma vez para o rosto dela, bem de perto, a tela treme com chiado de estática
/// e um tom vermelho, o som explode e, no fim, corta seco para a tela preta.
/// </summary>
public class SustoProfessora : MonoBehaviour
{
    /// <summary>Acesso rápido ao susto da cena.</summary>
    public static SustoProfessora Instancia { get; private set; }

    public AudioClip somSusto;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Tela")]
    [Tooltip("Imagem cheia de chiado (estática) por cima de tudo.")]
    public RawImage estatica;

    [Tooltip("Camada vermelha por cima de tudo.")]
    public Image vermelho;

    [Header("Câmera")]
    [Tooltip("Segundos do susto antes de cortar para o preto.")]
    public float duracao = 1.1f;

    [Tooltip("Força do tremor da câmera.")]
    public float tremor = 0.05f;

    [Tooltip("Distância da câmera até o rosto dela.")]
    public float distanciaRosto = 0.65f;

    [Tooltip("Altura do rosto, se o modelo não tiver o osso da cabeça.")]
    public float alturaRosto = 1.55f;

    private Texture2D ruido;
    private Color32[] pixels;

    /// <summary>Registra a instância e deixa as camadas desligadas.</summary>
    void Awake()
    {
        Instancia = this;
        if (estatica != null)
            estatica.gameObject.SetActive(false);
        if (vermelho != null)
            vermelho.gameObject.SetActive(false);
    }

    /// <summary>Começa o susto. frase = texto da tela preta no fim.</summary>
    public void Disparar(Transform professora, string frase)
    {
        StartCoroutine(Rotina(professora, frase));
    }

    private IEnumerator Rotina(Transform professora, string frase)
    {
        Camera cam = Camera.main;
        CameraFollow segue = cam != null ? cam.GetComponent<CameraFollow>() : null;
        if (segue != null)
            segue.enabled = false;

        Animator anim = professora.GetComponentInChildren<Animator>();
        Transform cabeca = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
        Vector3 rosto = cabeca != null ? cabeca.position + Vector3.up * 0.09f : professora.position + Vector3.up * alturaRosto;
        Vector3 posicaoCamera = rosto + professora.forward * distanciaRosto;
        float campoOriginal = cam != null ? cam.fieldOfView : 60f;

        // Luz fraca de baixo para cima no rosto dela (no escuro não daria para ver nada).
        GameObject luzRosto = new GameObject("Luz_Susto");
        luzRosto.transform.position = posicaoCamera + Vector3.down * 0.45f;
        Light luz = luzRosto.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.85f, 0.9f, 1f);
        luz.range = 1.8f;
        luz.intensity = 0.8f;

        UIManager.TocarSom2D(somSusto, volume);
        PrepararRuido();
        if (estatica != null)
            estatica.gameObject.SetActive(true);
        if (vermelho != null)
            vermelho.gameObject.SetActive(true);

        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float forca = 1f - 0.5f * (t / duracao);

            if (cam != null)
            {
                cam.transform.position = posicaoCamera + Random.insideUnitSphere * tremor * forca;
                cam.transform.LookAt(rosto + Random.insideUnitSphere * 0.03f);
                cam.fieldOfView = Mathf.Lerp(campoOriginal, 38f, Mathf.Clamp01(t * 6f));
            }

            luz.intensity = Random.value < 0.2f ? 0.2f : 0.8f;
            AtualizarRuido();
            if (estatica != null)
                estatica.color = new Color(1f, 1f, 1f, Random.value < 0.15f ? Random.Range(0.3f, 0.5f) : Random.Range(0.04f, 0.14f));
            if (vermelho != null)
                vermelho.color = new Color(0.6f, 0f, 0f, 0.1f + 0.08f * Mathf.Sin(t * 40f));

            yield return null;
        }

        Destroy(luzRosto);
        if (estatica != null)
            estatica.gameObject.SetActive(false);
        if (vermelho != null)
            vermelho.gameObject.SetActive(false);
        if (cam != null)
            cam.fieldOfView = campoOriginal;

        if (TelaPreta.Instancia != null)
            TelaPreta.Instancia.EscurecerNaHora(frase);
    }

    /// <summary>Cria a textura pequena que vira o chiado.</summary>
    private void PrepararRuido()
    {
        if (ruido != null)
            return;
        ruido = new Texture2D(160, 90, TextureFormat.RGBA32, false);
        ruido.filterMode = FilterMode.Point;
        pixels = new Color32[ruido.width * ruido.height];
        if (estatica != null)
            estatica.texture = ruido;
    }

    /// <summary>Sorteia cinzas novos a cada quadro (o chiado mexe).</summary>
    private void AtualizarRuido()
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            byte v = (byte)Random.Range(0, 256);
            pixels[i] = new Color32(v, v, v, 255);
        }
        ruido.SetPixels32(pixels);
        ruido.Apply(false);
    }

    /// <summary>Limpa a referência ao trocar de cena.</summary>
    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }
}
