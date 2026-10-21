using UnityEngine;

// =====================================================================
//  GatoPatrulha  (Nível 2 - Pátio)
//  O gatinho anda de um ponto ao outro, para de vez em quando para miar
//  e também mia quando o jogador chega perto.
//
//  COMO USAR:
//   1. Coloque este script no objeto do gato (precisa ter um Animator
//      usando o controller "Gato" em _Projeto/Animations).
//   2. Arraste os pontos de patrulha (objetos vazios) no campo "Pontos".
//   3. Arraste um som de miado no campo "Som Miado".
//  Os estados do Animator precisam se chamar: andar, parado e miar.
// =====================================================================
[RequireComponent(typeof(AudioSource))]
public class GatoPatrulha : MonoBehaviour
{
    [Header("Caminho")]
    [Tooltip("Pontos por onde o gato passa, em ordem. Ele volta ao primeiro no final.")]
    public Transform[] pontos;
    public float velocidade = 0.6f;
    public float velocidadeGiro = 5f;

    [Header("Miado")]
    public AudioClip somMiado;
    [Tooltip("Segundos entre um miado e outro enquanto anda.")]
    public float intervaloMiado = 6f;
    [Tooltip("Tempo que o gato fica parado miando.")]
    public float duracaoMiado = 1.2f;

    [Header("Jogador")]
    [Tooltip("Se o jogador (tag Player) chegar mais perto que isso, o gato mia.")]
    public float distanciaParaMiar = 2.5f;

    private Animator animator;
    private AudioSource audioSource;
    private Transform jogador;
    private int pontoAtual = 0;
    private float timerMiado;
    private float timerParado;
    private bool jogadorEstavaPerto = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;   // som 3D: fica mais alto perto do gato
        audioSource.playOnAwake = false;

        GameObject objJogador = GameObject.FindWithTag("Player");
        if (objJogador != null) jogador = objJogador.transform;

        timerMiado = intervaloMiado;
        TrocarAnimacao("andar");
    }

    void Update()
    {
        // 1) Se está parado miando, só espera o tempo acabar
        if (timerParado > 0f)
        {
            timerParado -= Time.deltaTime;
            if (timerParado <= 0f) TrocarAnimacao("andar");
            return;
        }

        // 2) Mia quando o jogador chega perto (uma vez por aproximação)
        if (jogador != null)
        {
            bool perto = Vector3.Distance(transform.position, jogador.position) < distanciaParaMiar;
            if (perto && !jogadorEstavaPerto)
            {
                jogadorEstavaPerto = true;
                OlharPara(jogador.position, true);
                Miar();
                return;
            }
            if (!perto) jogadorEstavaPerto = false;
        }

        // 3) Mia sozinho de tempos em tempos
        timerMiado -= Time.deltaTime;
        if (timerMiado <= 0f)
        {
            Miar();
            return;
        }

        // 4) Anda até o próximo ponto
        Andar();
    }

    void Andar()
    {
        if (pontos == null || pontos.Length == 0) return;

        Vector3 alvo = pontos[pontoAtual].position;
        alvo.y = transform.position.y; // não sobe nem desce

        transform.position = Vector3.MoveTowards(transform.position, alvo, velocidade * Time.deltaTime);
        OlharPara(alvo, false);

        // Chegou? Vai para o próximo ponto
        if (Vector3.Distance(transform.position, alvo) < 0.05f)
            pontoAtual = (pontoAtual + 1) % pontos.Length;
    }

    void Miar()
    {
        TrocarAnimacao("miar");
        if (somMiado != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.15f); // cada miado um pouco diferente
            audioSource.PlayOneShot(somMiado);
        }
        timerParado = duracaoMiado;
        timerMiado = intervaloMiado;
    }

    void OlharPara(Vector3 alvo, bool instantaneo)
    {
        Vector3 direcao = alvo - transform.position;
        direcao.y = 0f;
        if (direcao.sqrMagnitude < 0.0001f) return;

        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcao);
        transform.rotation = instantaneo
            ? rotacaoAlvo
            : Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeGiro * Time.deltaTime);
    }

    void TrocarAnimacao(string estado)
    {
        if (animator != null) animator.CrossFade(estado, 0.15f);
    }

    // Desenha o caminho na Scene View (linha amarela) para facilitar a edição
    void OnDrawGizmos()
    {
        if (pontos == null || pontos.Length < 2) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < pontos.Length; i++)
        {
            if (pontos[i] == null || pontos[(i + 1) % pontos.Length] == null) continue;
            Gizmos.DrawLine(pontos[i].position, pontos[(i + 1) % pontos.Length].position);
        }
    }
}
