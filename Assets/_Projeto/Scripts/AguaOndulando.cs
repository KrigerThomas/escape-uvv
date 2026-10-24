using UnityEngine;

/// <summary>
/// Dá movimento à superfície do lago: desliza as texturas do material aos poucos,
/// o que faz o reflexo da lua e dos postes tremer como água.
/// No material Lit do URP, a textura de relevo (Normal Map) usa a mesma posição
/// da textura base (_BaseMap), por isso o script desliza a _BaseMap.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class AguaOndulando : MonoBehaviour
{
    [Tooltip("Velocidade, em repetições da textura por segundo, nos eixos X e Y.")]
    public Vector2 velocidade = new Vector2(0.02f, 0.012f);

    [Tooltip("Nome da textura do material que vai deslizar.")]
    public string propriedadeTextura = "_BaseMap";

    private Material material;
    private Vector2 deslocamento;

    /// <summary>Pega uma cópia do material só deste objeto (para não mexer no arquivo do material).</summary>
    void Start()
    {
        material = GetComponent<Renderer>().material;
    }

    /// <summary>Move a textura um pouco a cada quadro.</summary>
    void Update()
    {
        if (material == null)
            return;

        deslocamento += velocidade * Time.deltaTime;
        material.SetTextureOffset(propriedadeTextura, deslocamento);
    }
}
