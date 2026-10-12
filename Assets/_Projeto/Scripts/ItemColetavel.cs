using UnityEngine;

/// <summary>
/// Item perdido que gira no lugar. Quando o jogador encosta, ele some,
/// solta um brilho, toca um som e avisa o GameManager.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ItemColetavel : MonoBehaviour
{
    [Tooltip("Nome mostrado na tela quando o item é coletado.")]
    public string nomeItem = "Item";

    [Header("Animação")]
    [Tooltip("Graus por segundo que o item gira em volta do eixo Y.")]
    public float velocidadeGiro = 90f;

    [Tooltip("Quanto o item sobe e desce, em metros.")]
    public float alturaFlutuacao = 0.08f;

    [Header("Efeitos")]
    [Tooltip("Partícula criada no lugar do item ao ser coletado.")]
    public GameObject efeitoColeta;

    [Tooltip("Som tocado ao coletar.")]
    public AudioClip somColeta;

    [Range(0f, 1f)] public float volume = 0.8f;

    private Vector3 posicaoInicial;
    private bool coletado;

    /// <summary>Guarda a posição inicial e garante que o collider seja um trigger.</summary>
    void Start()
    {
        posicaoInicial = transform.position;
        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>Gira o item com Transform.Rotate e faz ele flutuar.</summary>
    void Update()
    {
        transform.Rotate(0f, velocidadeGiro * Time.deltaTime, 0f, Space.World);

        Vector3 pos = posicaoInicial;
        pos.y += Mathf.Sin(Time.time * 2f) * alturaFlutuacao;
        transform.position = pos;
    }

    /// <summary>Detecta o jogador (tag Player) e faz a coleta.</summary>
    /// <param name="outro">Collider que entrou no trigger.</param>
    void OnTriggerEnter(Collider outro)
    {
        if (coletado || !outro.CompareTag("Player"))
            return;

        Coletar();
    }

    /// <summary>Toca o efeito e o som, avisa o GameManager e remove o item da cena.</summary>
    private void Coletar()
    {
        coletado = true;

        if (efeitoColeta != null)
        {
            GameObject efeito = Instantiate(efeitoColeta, transform.position, Quaternion.identity);
            Destroy(efeito, 3f);
        }

        if (somColeta != null)
            AudioSource.PlayClipAtPoint(somColeta, transform.position, volume);

        if (GameManager.Instancia != null)
            GameManager.Instancia.ColetarItem(nomeItem);

        Destroy(gameObject);
    }
}
