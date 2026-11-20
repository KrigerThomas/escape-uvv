using System.Collections;
using UnityEngine;

/// <summary>
/// Tempestade lá fora: de tempos em tempos um relâmpago (a luz pisca forte duas ou três vezes
/// e a neblina clareia junto) e, logo depois, um trovão. Quanto mais longe o raio, maior a espera
/// entre o clarão e o som. Outros scripts podem pedir um relâmpago na hora com Relampago(true).
/// </summary>
public class Tempestade : MonoBehaviour
{
    /// <summary>Acesso rápido à tempestade da cena.</summary>
    public static Tempestade Instancia { get; private set; }

    [Header("Clarão")]
    [Tooltip("Luz direcional que só acende no relâmpago (com sombra, para entrar só pelas janelas).")]
    public Light luzRelampago;

    public float intensidadeRelampago = 2.5f;

    [Tooltip("Cor que a neblina ganha durante o clarão.")]
    public Color corNeblinaClarao = new Color(0.32f, 0.35f, 0.42f);

    [Header("Trovão")]
    public AudioClip[] trovoes;

    [Range(0f, 1f)] public float volumeTrovao = 0.55f;

    [Header("Frequência")]
    [Tooltip("Menor e maior tempo entre dois relâmpagos, em segundos.")]
    public Vector2 intervalo = new Vector2(14f, 32f);

    [Tooltip("Desmarque para parar os relâmpagos automáticos.")]
    public bool ativa = true;

    private AudioSource fonte;
    private Color corNeblina;
    private float proximo;

    /// <summary>Registra a instância e cria a fonte do trovão.</summary>
    void Awake()
    {
        Instancia = this;
        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
    }

    /// <summary>Guarda a cor da neblina e agenda o primeiro relâmpago.</summary>
    void Start()
    {
        corNeblina = RenderSettings.fogColor;
        if (luzRelampago != null)
            luzRelampago.enabled = false;
        Agendar(5f);
    }

    /// <summary>Quando chega a hora, solta um relâmpago longe.</summary>
    void Update()
    {
        if (!ativa || Time.time < proximo)
            return;
        Relampago(false);
        Agendar(0f);
    }

    /// <summary>Relâmpago com trovão. Perto: o trovão vem quase junto e mais alto.</summary>
    public void Relampago(bool perto)
    {
        StartCoroutine(Clarao(perto));
    }

    private IEnumerator Clarao(bool perto)
    {
        int piscadas = Random.Range(2, 4);
        for (int i = 0; i < piscadas; i++)
        {
            float forca = Random.Range(0.6f, 1f) * (perto ? 1.3f : 1f);
            if (luzRelampago != null)
            {
                luzRelampago.intensity = intensidadeRelampago * forca;
                luzRelampago.enabled = true;
            }
            RenderSettings.fogColor = Color.Lerp(corNeblina, corNeblinaClarao, forca);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.09f));

            if (luzRelampago != null)
                luzRelampago.enabled = false;
            RenderSettings.fogColor = corNeblina;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.14f));
        }

        yield return new WaitForSeconds(perto ? 0.1f : Random.Range(0.6f, 2.2f));

        if (trovoes != null && trovoes.Length > 0)
        {
            fonte.pitch = Random.Range(0.9f, 1.05f);
            fonte.PlayOneShot(trovoes[Random.Range(0, trovoes.Length)], perto ? 1f : volumeTrovao * Random.Range(0.6f, 1f));
        }
    }

    /// <summary>Sorteia quando vem o próximo relâmpago.</summary>
    private void Agendar(float minimo)
    {
        proximo = Time.time + Mathf.Max(minimo, Random.Range(intervalo.x, intervalo.y));
    }

    /// <summary>Volta a neblina ao normal ao sair da cena.</summary>
    void OnDestroy()
    {
        RenderSettings.fogColor = corNeblina;
        if (Instancia == this)
            Instancia = null;
    }
}
