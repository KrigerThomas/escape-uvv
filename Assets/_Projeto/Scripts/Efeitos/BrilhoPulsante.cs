using UnityEngine;

/// <summary>
/// Faz um objeto chamar atenção pulsando: a luz fica mais forte e mais fraca devagar
/// e, se houver, a tela (material com emissão) acende junto.
/// Usado no celular do Nível 5, para o jogador perceber o que pegar no começo.
/// </summary>
public class BrilhoPulsante : MonoBehaviour
{
    [Tooltip("Luz que pulsa (opcional).")]
    public Light luz;

    [Tooltip("Renderers que acendem junto (ex.: a tela do celular). Opcional.")]
    public Renderer[] telas;

    public Color corTela = new Color(0.55f, 0.75f, 1f);

    [Tooltip("Intensidade mínima e máxima da luz.")]
    public float luzMinima = 0.2f;
    public float luzMaxima = 1.4f;

    [Tooltip("Força mínima e máxima da emissão da tela.")]
    public float telaMinima = 0.3f;
    public float telaMaxima = 2.2f;

    [Tooltip("Pulsos por segundo.")]
    public float velocidade = 0.8f;

    private MaterialPropertyBlock bloco;

    /// <summary>Calcula o pulso e aplica na luz e na tela.</summary>
    void Update()
    {
        float onda = (Mathf.Sin(Time.time * velocidade * Mathf.PI * 2f) + 1f) * 0.5f;

        if (luz != null)
            luz.intensity = Mathf.Lerp(luzMinima, luzMaxima, onda);

        if (telas == null || telas.Length == 0)
            return;

        if (bloco == null)
            bloco = new MaterialPropertyBlock();

        Color emissao = corTela * Mathf.Lerp(telaMinima, telaMaxima, onda);
        foreach (Renderer r in telas)
        {
            if (r == null)
                continue;
            r.GetPropertyBlock(bloco);
            bloco.SetColor("_EmissionColor", emissao);
            bloco.SetColor("_BaseColor", corTela * 0.4f);
            r.SetPropertyBlock(bloco);
        }
    }
}
