using UnityEngine;

// =====================================================================
//  EsquiloFujao  (Nível 2 - Pátio)
//  Um esquilo que fica no pé das árvores. Quando o jogador chega perto
//  ele chia e corre para a próxima árvore da lista.
//
//  EASTER EGG: depois de fugir algumas vezes (padrão: 4) ele cansa,
//  desiste de fugir e "deixa uma bolota" para o jogador. A partir daí
//  ele fica amigo: só olha e chia quando o jogador chega perto.
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
    public float distanciaSusto = 4.5f;

    [Header("Sons")]
    public AudioClip somChiado;
    [Tooltip("Tempo mínimo e máximo entre chiados quando ele está parado.")]
    public Vector2 intervaloChiado = new Vector2(7f, 14f);

    [Header("Easter egg")]
    public int fugasAteCansar = 4;
    public AudioClip somPresente;
    public GameObject efeitoPresente;
    public string textoPrimeiraFuga = "Um esquilo! Será que dá pra alcançar ele?";
    [TextArea] public string textoPresente = "O esquilo cansou de fugir e te deixou uma bolota. Amizade feita!";

    private enum Estado { Parado, Correndo, Amigo }

    private Estado estado = Estado.Parado;
    private Animator animator;
    private AudioSource audioSource;
    private Transform jogador;
    private int esconderijoAtual;
    private int fugas;
    private bool jogadorEstavaPerto;
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
        audioSource.maxDistance = 20f;
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
                    if (fugas >= fugasAteCansar)
                        VirarAmigo();
                    else
                        Fugir();
                    break;
                }
                PosesEChiados();
                break;

            case Estado.Correndo:
                Correr();
                break;

            case Estado.Amigo:
                if (perto)
                {
                    OlharPara(jogador.position);
                    if (!jogadorEstavaPerto)
                        Chiar();
                }
                jogadorEstavaPerto = perto;
                PosesEChiados();
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
            FicarParado();
        }
    }

    /// <summary>Easter egg: para de fugir, toca o som e o efeito do presente e mostra o aviso.</summary>
    private void VirarAmigo()
    {
        estado = Estado.Amigo;
        jogadorEstavaPerto = true;
        TrocarAnimacao("parado2");

        if (jogador != null)
            OlharPara(jogador.position, true);

        if (efeitoPresente != null)
        {
            GameObject efeito = Instantiate(efeitoPresente, transform.position + Vector3.up * 0.2f, Quaternion.identity);
            Destroy(efeito, 3f);
        }

        if (somPresente != null)
            AudioSource.PlayClipAtPoint(somPresente, transform.position, 0.8f);

        Chiar();
        AvisoNaTela.Mostrar(textoPresente);
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
            Chiar();
        }
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
