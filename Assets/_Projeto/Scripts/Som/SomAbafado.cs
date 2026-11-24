using UnityEngine;

/// <summary>
/// Deixa um som 3D mais baixo e abafado quando há uma parede ou uma sebe
/// entre ele e a câmera. Vai no mesmo objeto do AudioSource (gato, esquilo, fonte...).
/// Funciona com um raio da câmera até o som: se bater em alguma coisa no caminho, o som abafa.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SomAbafado : MonoBehaviour
{
    [Tooltip("Volume que sobra quando tem algo no caminho (0 a 1).")]
    [Range(0f, 1f)] public float volumeAtras = 0.35f;

    [Tooltip("Frequência de corte do abafado (menor = mais abafado).")]
    public float corteAtras = 900f;

    [Tooltip("Trecho perto do som que não conta como obstáculo (ex.: a borda da fonte em volta do som da água).")]
    public float margem = 0.3f;

    [Tooltip("Colisores que não contam como obstáculo (ex.: a colisão da própria fonte).")]
    public Collider[] ignorar;

    [Tooltip("Quantas vezes por segundo o caminho é conferido.")]
    public float checagensPorSegundo = 8f;

    [Tooltip("Rapidez com que o som muda entre aberto e abafado.")]
    public float suavidade = 6f;

    private AudioSource fonte;
    private AudioLowPassFilter filtro;
    private Transform ouvinte;
    private float volumeNormal;
    private float alvo = 1f;
    private float atual = 1f;
    private float proximaChecagem;

    /// <summary>Guarda o volume original e cria o filtro de abafado.</summary>
    void Start()
    {
        fonte = GetComponent<AudioSource>();
        volumeNormal = fonte.volume;
        if (ignorar == null)
            ignorar = new Collider[0];
        filtro = GetComponent<AudioLowPassFilter>();
        if (filtro == null)
            filtro = gameObject.AddComponent<AudioLowPassFilter>();
        filtro.cutoffFrequency = 22000f;
    }

    /// <summary>Confere de tempos em tempos se tem algo entre a câmera e o som e ajusta aos poucos.</summary>
    void Update()
    {
        if (ouvinte == null)
        {
            if (Camera.main == null)
                return;
            ouvinte = Camera.main.transform;
        }

        if (Time.time >= proximaChecagem)
        {
            proximaChecagem = Time.time + 1f / Mathf.Max(1f, checagensPorSegundo);
            alvo = TemAlgoNoCaminho() ? 0f : 1f;
        }

        atual = Mathf.MoveTowards(atual, alvo, suavidade * Time.deltaTime);
        fonte.volume = volumeNormal * Mathf.Lerp(volumeAtras, 1f, atual);
        filtro.cutoffFrequency = Mathf.Lerp(corteAtras, 22000f, atual * atual);
    }

    /// <summary>Verdadeiro se o raio da câmera até o som bate em algo que não é o próprio som nem o jogador.</summary>
    private bool TemAlgoNoCaminho()
    {
        Vector3 direcao = transform.position - ouvinte.position;
        float distancia = direcao.magnitude;
        if (distancia < 0.5f)
            return false;

        RaycastHit[] batidas = Physics.RaycastAll(ouvinte.position, direcao / distancia, distancia - margem, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit batida in batidas)
        {
            if (batida.transform.IsChildOf(transform) || transform.IsChildOf(batida.transform))
                continue;
            if (batida.collider.CompareTag("Player") || System.Array.IndexOf(ignorar, batida.collider) >= 0)
                continue;
            return true;
        }
        return false;
    }
}
