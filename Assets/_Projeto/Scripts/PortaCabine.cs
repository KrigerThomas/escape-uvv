using UnityEngine;

// Porta de cabine do banheiro: abre sozinha quando o jogador chega perto
// e fecha quando ele se afasta. Fica no objeto da DOBRADIÇA (o pivô da porta).
public class PortaCabine : MonoBehaviour
{
    [Tooltip("Negativo abre para dentro da cabine, positivo abre para fora")]
    public float anguloAbertura = -95f;
    public float distanciaParaAbrir = 1.5f;
    public float velocidade = 2.5f;
    public AudioClip somAbrir;

    private Quaternion rotacaoFechada;
    private Transform jogador;
    private AudioSource fonte;
    private float abertura;     // 0 = fechada, 1 = aberta
    private bool estavaPerto;
    private Vector3 centroPorta;

    void Start()
    {
        rotacaoFechada = transform.localRotation;
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jogador = j.transform;
        // centro da porta fechada (para medir a distância do jogador)
        Renderer r = GetComponentInChildren<Renderer>();
        centroPorta = r != null ? r.bounds.center : transform.position;
        if (somAbrir != null)
        {
            fonte = gameObject.AddComponent<AudioSource>();
            fonte.spatialBlend = 1f;
            fonte.volume = 0.5f;
            fonte.maxDistance = 12f;
        }
    }

    void Update()
    {
        if (jogador == null) return;
        Vector3 d = jogador.position - centroPorta;
        d.y = 0f;
        bool perto = d.magnitude < distanciaParaAbrir;

        if (perto && !estavaPerto && fonte != null) fonte.PlayOneShot(somAbrir);
        estavaPerto = perto;

        abertura = Mathf.MoveTowards(abertura, perto ? 1f : 0f, velocidade * Time.deltaTime);
        float angulo = Mathf.SmoothStep(0f, 1f, abertura) * anguloAbertura;
        transform.localRotation = rotacaoFechada * Quaternion.Euler(0f, angulo, 0f);
    }
}
