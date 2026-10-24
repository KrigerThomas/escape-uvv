using UnityEngine;

/// <summary>
/// Faz uma luz falhar, como um poste com mau contato: na maior parte do tempo fica acesa,
/// mas de vez em quando pisca algumas vezes seguidas. O brilho do material da lâmpada
/// (Emission) acompanha a luz.
/// </summary>
[RequireComponent(typeof(Light))]
public class LuzPiscante : MonoBehaviour
{
    [Tooltip("Renderer da lâmpada, para apagar o brilho junto com a luz (opcional).")]
    public Renderer lampada;

    [Tooltip("Menor e maior tempo, em segundos, entre uma falha e outra.")]
    public Vector2 intervaloEntreFalhas = new Vector2(2f, 6f);

    [Tooltip("Quantas piscadas, no máximo, cada falha tem.")]
    public int piscadasMaximas = 4;

    private Light luz;
    private float intensidadeNormal;
    private Color emissaoNormal;
    private MaterialPropertyBlock bloco;
    private float proximaFalha;
    private int piscadasRestantes;
    private float trocaEstado;
    private bool acesa = true;

    /// <summary>Guarda a intensidade e o brilho originais e agenda a primeira falha.</summary>
    void Start()
    {
        luz = GetComponent<Light>();
        intensidadeNormal = luz.intensity;
        bloco = new MaterialPropertyBlock();

        if (lampada != null && lampada.sharedMaterial != null && lampada.sharedMaterial.HasProperty("_EmissionColor"))
            emissaoNormal = lampada.sharedMaterial.GetColor("_EmissionColor");

        proximaFalha = Time.time + Random.Range(intervaloEntreFalhas.x, intervaloEntreFalhas.y);
    }

    /// <summary>Começa uma falha na hora sorteada e alterna aceso/apagado durante ela.</summary>
    void Update()
    {
        if (piscadasRestantes <= 0)
        {
            if (Time.time < proximaFalha)
                return;

            piscadasRestantes = Random.Range(1, piscadasMaximas + 1) * 2;
            trocaEstado = Time.time;
        }

        if (Time.time < trocaEstado)
            return;

        acesa = !acesa;
        AplicarEstado(acesa);
        piscadasRestantes--;
        trocaEstado = Time.time + Random.Range(0.04f, 0.18f);

        if (piscadasRestantes <= 0)
        {
            acesa = true;
            AplicarEstado(true);
            proximaFalha = Time.time + Random.Range(intervaloEntreFalhas.x, intervaloEntreFalhas.y);
        }
    }

    /// <summary>Liga ou desliga a luz e o brilho da lâmpada.</summary>
    /// <param name="ligada">Verdadeiro para acender, falso para apagar.</param>
    private void AplicarEstado(bool ligada)
    {
        luz.intensity = ligada ? intensidadeNormal : intensidadeNormal * 0.05f;

        if (lampada == null)
            return;

        lampada.GetPropertyBlock(bloco);
        bloco.SetColor("_EmissionColor", ligada ? emissaoNormal : Color.black);
        lampada.SetPropertyBlock(bloco);
    }
}
