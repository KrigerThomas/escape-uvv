using UnityEngine;

// =====================================================================
//  EsquiloFujao  (Nível 2 - Pátio)
//  Um esquilo que fica no pé das árvores. Quando o jogador chega perto
//  ele chia e corre para a próxima árvore da lista.
//
//  EASTER EGG: depois de fugir algumas vezes (padrão: 4) ele some de vez.
//  Mesmo sumido, o chiado dele continua aparecendo de vez em quando
//  perto do último esconderijo (piada com um bug de som que tivemos).
//
//  COMO USAR:
//   1. Coloque este script num objeto vazio; o modelo do esquilo
//      (Animator com o controller "Esquilo" em _Projeto/Animations) fica
//      como filho.
//   2. Preencha "Esconderijos" com objetos vazios no pé das árvores.
//  Estados do Animator usados: parado, parado2 e correr.
// =====================================================================
[RequireComponent(typeof(AudioSource))]
public class EsquiloFujao : MonoBehaviour
{
    [Header("Caminho")]
    [Tooltip("Pontos (pé das árvores) para onde o esquilo foge, em ordem.")]
    public Transform[] esconderijos;
    public float velocidadeCorrida = 4f;
    public float velocidadeGiro = 12f;

    [Header("Susto")]
    [Tooltip("Se o jogador chegar mais perto que isso, o esquilo foge.")]
    public float distanciaSusto = 3f;

    [Header("Sons")]
    public AudioClip somChiado;
    [Tooltip("Só chia sozinho (parado) se o jogador estiver até esta distância.")]
    public float distanciaChiado = 10f;

    [Tooltip("Tempo mínimo e máximo entre chiados quando ele está parado.")]
    public Vector2 intervaloChiado = new Vector2(4f, 8f);

    [Header("Easter egg")]
    [Tooltip("Quantas vezes ele foge antes de sumir de vez.")]
    [UnityEngine.Serialization.FormerlySerializedAs("fugasAteCansar")]
    public int fugasAteSumir = 4;
    [Tooltip("Aviso na primeira vez que o esquilo foge.")]
    public string textoPrimeiraFuga = "Um esquilo aqui pela UVV? Para onde será que ele vai?";
    [Tooltip("Som de folhas quando ele entra no mato e some.")]
    public AudioClip somSumir;
    [Tooltip("Aviso de quando o esquilo some de vez.")]
    [TextArea] public string textoSumiu = "O esquilo sumiu, mas ainda tenho a impressão de que consigo ouvir ele...";

    private enum Estado { Parado, Correndo, Sumido }

    private Estado estado = Estado.Parado;
    private Animator animator;
    private AudioSource audioSource;
    private Transform jogador;
    private int esconderijoAtual;
    private int fugas;
    private float timerChiado;
    private float timerPose;


    /// <summary>Pega os componentes, configura o som 3D e vai para o primeiro esconderijo.</summary>
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 10f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        jogador = AvisoNaTela.AcharJogador();

        if (esconderijos != null && esconderijos.Length > 0 && esconderijos[0] != null)
            transform.position = NoChao(esconderijos[0].position);

        timerChiado = Random.Range(intervaloChiado.x, intervaloChiado.y);
        FicarParado();
    }

    /// <summary>Máquina de estados do esquilo.</summary>
    void Update()
    {
        RepetirAnimacao();

        bool perto = jogador != null
            && AvisoNaTela.DistanciaPlana(transform.position, jogador.position) < distanciaSusto;

        switch (estado)
        {
            case Estado.Parado:
                if (perto)
                {
                    Fugir();
                    break;
                }
                PosesEChiados();
                break;

            case Estado.Correndo:
                Correr();
                break;

            case Estado.Sumido:
                ChiadoFantasma();
                break;
        }
    }

    /// <summary>Chia e corre para o próximo esconderijo.</summary>
    private void Fugir()
    {
        if (esconderijos == null || esconderijos.Length < 2)
            return;

        if (fugas == 0)
            AvisoNaTela.Mostrar(textoPrimeiraFuga);

        fugas++;
        esconderijoAtual = (esconderijoAtual + 1) % esconderijos.Length;
        estado = Estado.Correndo;
        TrocarAnimacao("correr");
        Chiar();
    }

    /// <summary>Move até o esconderijo atual; ao chegar, fica parado.</summary>
    private void Correr()
    {
        Transform alvoT = esconderijos[esconderijoAtual];
        if (alvoT == null)
        {
            FicarParado();
            return;
        }

        Vector3 alvo = NoChao(alvoT.position);
        transform.position = NoChao(Vector3.MoveTowards(transform.position, alvo, velocidadeCorrida * Time.deltaTime));
        OlharPara(alvo);

        if (AvisoNaTela.DistanciaPlana(transform.position, alvo) < 0.05f)
        {
            // Ao chegar, vira para o lado de onde veio o jogador (fica "vigiando")
            if (jogador != null)
                OlharPara(jogador.position, true);
            if (!JogadorVe())
                audioSource.Stop();
            // na última fuga ele entra no mato do pé da árvore e some de vez
            if (fugas >= fugasAteSumir)
                Sumir();
            else
                FicarParado();
        }
    }

    /// <summary>Easter egg: o esquilo entra no mato, some de vez e o aviso aparece.</summary>
    private void Sumir()
    {
        estado = Estado.Sumido;
        audioSource.Stop();
        if (somSumir != null)
            AudioSource.PlayClipAtPoint(somSumir, transform.position, 0.8f);
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        if (animator != null)
            animator.enabled = false;
        timerChiado = Random.Range(intervaloChiado.x, intervaloChiado.y);
        AvisoNaTela.Mostrar(textoSumiu);
    }

    /// <summary>Depois de sumir, o chiado ainda toca de vez em quando se o jogador passar perto.</summary>
    private void ChiadoFantasma()
    {
        timerChiado -= Time.deltaTime;
        if (timerChiado > 0f)
            return;
        timerChiado = Random.Range(intervaloChiado.x, intervaloChiado.y) * 1.5f;
        if (jogador != null && AvisoNaTela.DistanciaPlana(transform.position, jogador.position) < distanciaChiado)
            Chiar();
    }

    /// <summary>Alterna entre as duas poses paradas e chia de vez em quando.</summary>
    private void PosesEChiados()
    {
        timerPose -= Time.deltaTime;
        if (timerPose <= 0f)
        {
            timerPose = Random.Range(4f, 9f);
            TrocarAnimacao(Random.value < 0.6f ? "parado" : "parado2");
        }

        timerChiado -= Time.deltaTime;
        if (timerChiado <= 0f)
        {
            timerChiado = Random.Range(intervaloChiado.x, intervaloChiado.y);
            // Escondido longe no pé da árvore ele fica quieto (antes chiava sem ninguém ver).
            if (JogadorVe())
                Chiar();
        }
    }

    /// <summary>
    /// Confere se o jogador está perto e enxerga o esquilo (sem sebe ou árvore no meio).
    /// Assim ele não chia escondido do outro lado do labirinto.
    /// </summary>
    /// <returns>Verdadeiro se o jogador está a menos de distanciaChiado e com o caminho livre.</returns>
    private bool JogadorVe()
    {
        if (jogador == null || AvisoNaTela.DistanciaPlana(transform.position, jogador.position) > distanciaChiado)
            return false;

        Vector3 de = transform.position + Vector3.up * 0.3f;
        Vector3 ate = jogador.position + Vector3.up * 1.2f;
        if (Physics.Linecast(de, ate, out RaycastHit batida, ~0, QueryTriggerInteraction.Ignore))
            return batida.transform == jogador || batida.transform.IsChildOf(jogador) || batida.transform.IsChildOf(transform);
        return true;
    }

    /// <summary>Volta para o estado parado.</summary>
    private void FicarParado()
    {
        estado = Estado.Parado;
        timerPose = Random.Range(4f, 9f);
        TrocarAnimacao("parado");
    }

    /// <summary>Toca o chiado com um tom um pouco diferente a cada vez.</summary>
    private void Chiar()
    {
        if (somChiado == null)
            return;

        audioSource.pitch = Random.Range(0.9f, 1.15f);
        audioSource.PlayOneShot(somChiado, 0.8f);
    }

    /// <summary>Gira só no eixo Y para olhar um ponto.</summary>
    private void OlharPara(Vector3 alvo, bool instantaneo = false)
    {
        Vector3 direcao = alvo - transform.position;
        direcao.y = 0f;
        if (direcao.sqrMagnitude < 0.0001f)
            return;

        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcao);
        transform.rotation = instantaneo
            ? rotacaoAlvo
            : Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeGiro * Time.deltaTime);
    }

    /// <summary>Mesmo ponto, com o Y no chão do terreno.</summary>
    private Vector3 NoChao(Vector3 ponto)
    {
        ponto.y = AvisoNaTela.AlturaDoChao(ponto);
        return ponto;
    }

    /// <summary>
    /// As animações do FBX do esquilo vêm sem "loop". Quando a animação atual termina,
    /// ela recomeça do início (assim o esquilo não congela no último quadro).
    /// </summary>
    private void RepetirAnimacao()
    {
        if (animator == null || animator.IsInTransition(0))
            return;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.loop && info.normalizedTime >= 1f)
            animator.Play(info.fullPathHash, 0, 0f);
    }

    /// <summary>Troca o estado do Animator com uma transição suave.</summary>
    private void TrocarAnimacao(string nomeEstado)
    {
        if (animator != null)
            animator.CrossFadeInFixedTime(nomeEstado, 0.15f);
    }

    /// <summary>Desenha o caminho entre os esconderijos (linha laranja) e o raio de susto.</summary>
    void OnDrawGizmos()
    {
        if (esconderijos == null || esconderijos.Length < 2) return;
        Gizmos.color = new Color(1f, 0.55f, 0.1f);
        for (int i = 0; i < esconderijos.Length; i++)
        {
            Transform a = esconderijos[i];
            Transform b = esconderijos[(i + 1) % esconderijos.Length];
            if (a != null && b != null) Gizmos.DrawLine(a.position, b.position);
        }
    }
}
