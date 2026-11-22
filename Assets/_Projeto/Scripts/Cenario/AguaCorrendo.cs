using UnityEngine;

/// <summary>
/// Faz a água da fonte do Nível 2 parecer que está caindo: a textura da água
/// desliza sem parar pela malha. É bem mais leve que tocar a simulação da água
/// quadro a quadro, que deixava o nível mais lento.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class AguaCorrendo : MonoBehaviour
{
    [Tooltip("Velocidade e direção em que a textura desliza (por segundo).")]
    public Vector2 velocidade = new Vector2(0f, -0.6f);

    private Renderer rend;
    private MaterialPropertyBlock bloco;
    private Vector4 escalaOriginal;
    private Vector2 deslocamento;

    /// <summary>Guarda a escala e o deslocamento originais da textura.</summary>
    void Start()
    {
        rend = GetComponent<Renderer>();
        bloco = new MaterialPropertyBlock();
        escalaOriginal = rend.sharedMaterial.GetVector("_BaseMap_ST");
    }

    /// <summary>Move a textura um pouco a cada quadro, só quando a fonte aparece na tela.</summary>
    void Update()
    {
        if (!rend.isVisible)
            return;

        deslocamento += velocidade * Time.deltaTime;
        deslocamento.x %= 1f;
        deslocamento.y %= 1f;

        rend.GetPropertyBlock(bloco);
        bloco.SetVector("_BaseMap_ST", new Vector4(escalaOriginal.x, escalaOriginal.y,
            escalaOriginal.z + deslocamento.x, escalaOriginal.w + deslocamento.y));
        rend.SetPropertyBlock(bloco);
    }
}
