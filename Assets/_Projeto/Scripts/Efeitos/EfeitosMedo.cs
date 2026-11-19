using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Deixa a tela e o som mais tensos quando a Professora está perto:
///  - escurece as bordas da tela (Vignette do pós-processamento), em vermelho na perseguição;
///  - aumenta o volume e a velocidade do batimento cardíaco;
///  - controla o volume da música de perseguição.
/// "Medo" é um número de 0 (ela longe) a 1 (ela colada no jogador).
/// </summary>
public class EfeitosMedo : MonoBehaviour
{
    public Professora professora;

    [Tooltip("Volume global do pós-processamento da cena (precisa ter Vignette).")]
    public Volume volume;

    [Tooltip("Som de batimento cardíaco em loop (opcional).")]
    public AudioSource batimento;

    [Tooltip("Música que toca durante a perseguição (opcional).")]
    public AudioSource musicaPerseguicao;

    [Tooltip("A partir desta distância (metros) o medo começa a subir.")]
    public float distanciaMedo = 9f;

    [Header("Bordas da tela")]
    public float vinhetaCalma = 0.3f;
    public float vinhetaMaxima = 0.55f;
    public Color corCalma = Color.black;
    public Color corPerseguicao = new Color(0.35f, 0f, 0f);

    [Tooltip("Volume máximo da música de perseguição.")]
    [Range(0f, 1f)] public float volumeMusica = 0.6f;

    private Vignette vinheta;
    private Transform jogador;
    private float medo;

    /// <summary>Pega a Vignette do perfil (uma cópia, para não alterar o arquivo) e o jogador.</summary>
    void Start()
    {
        if (volume != null)
            volume.profile.TryGet(out vinheta);

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
            jogador = obj.transform;

        if (batimento != null)
        {
            batimento.volume = 0f;
            batimento.loop = true;
            batimento.Play();
        }

        if (musicaPerseguicao != null)
        {
            musicaPerseguicao.volume = 0f;
            musicaPerseguicao.loop = true;
        }
    }

    /// <summary>Calcula o medo e aplica na tela e nos sons.</summary>
    void Update()
    {
        if (professora == null || jogador == null)
            return;

        bool perseguindo = professora.EstadoAtual == Professora.Estado.Perseguindo;
        float distancia = Vector3.Distance(professora.transform.position, jogador.position);
        float alvo = 1f - Mathf.Clamp01(distancia / distanciaMedo);
        if (perseguindo)
            alvo = Mathf.Max(alvo, 0.6f);
        if (ArmarioEsconderijo.JogadorDentro)
            alvo *= 0.7f;

        medo = Mathf.MoveTowards(medo, alvo, Time.deltaTime);

        if (vinheta != null)
        {
            vinheta.intensity.value = Mathf.Lerp(vinhetaCalma, vinhetaMaxima, medo);
            vinheta.color.value = Color.Lerp(vinheta.color.value, perseguindo ? corPerseguicao : corCalma, Time.deltaTime * 2f);
        }

        if (batimento != null)
        {
            batimento.volume = medo;
            batimento.pitch = 0.9f + medo * 0.5f;
        }

        if (musicaPerseguicao != null)
        {
            if (perseguindo && !musicaPerseguicao.isPlaying)
                musicaPerseguicao.Play();

            float alvoMusica = perseguindo ? volumeMusica : 0f;
            musicaPerseguicao.volume = Mathf.MoveTowards(musicaPerseguicao.volume, alvoMusica, Time.deltaTime * 0.4f);

            if (!perseguindo && musicaPerseguicao.volume <= 0.001f && musicaPerseguicao.isPlaying)
                musicaPerseguicao.Stop();
        }
    }
}
