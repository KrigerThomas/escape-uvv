using UnityEngine;

/// <summary>
/// Faz a textura de um cubo se repetir a cada X metros, não importa o tamanho do cubo.
/// Sem isso, um piso de 40 m esticaria a mesma imagem de 1 m por todo o comprimento.
///
/// Como usar: coloque este script num cubo (MeshFilter com o Cube padrão) e ajuste
/// "Metros Por Textura" (quantos metros do mundo uma imagem cobre).
/// Com "Alinhar Ao Mundo" marcado, peças vizinhas continuam o desenho umas das outras
/// (os rejuntes do piso e dos azulejos ficam alinhados entre um cubo e o próximo).
///
/// Importante: NÃO marque estes objetos como Static (Batching Static), porque a malha
/// é recriada quando a cena abre e o static batching usaria a malha antiga.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class TexturaPorMetro : MonoBehaviour
{
    [Tooltip("Quantos metros do mundo uma repetição da textura cobre. Ex.: piso de granito = 1,2 (2 x 2 placas de 60 cm).")]
    public float metrosPorTextura = 1f;

    [Tooltip("Marcado: usa a posição no mundo, então cubos vizinhos continuam o desenho. Desmarque em objetos girados.")]
    public bool alinharAoMundo = true;

    [Tooltip("Gira a textura 90 graus (ex.: piso tátil direcional numa faixa que corre no eixo X).")]
    public bool girar90 = false;

    [Tooltip("Desloca o desenho (em metros), para acertar onde começa o rejunte.")]
    public Vector2 deslocamento = Vector2.zero;

    private static Mesh cuboOriginal;
    private Mesh malhaGerada;

    /// <summary>Recria a malha com as coordenadas de textura em metros ao ativar o objeto.</summary>
    void OnEnable()
    {
        Recalcular();
    }

    /// <summary>No Editor, recalcula sempre que um valor do Inspector muda.</summary>
    void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && isActiveAndEnabled)
                Recalcular();
        };
#endif
    }

#if UNITY_EDITOR
    /// <summary>No Editor (fora do Play), recalcula quando o cubo é movido ou redimensionado.</summary>
    void Update()
    {
        if (!Application.isPlaying && transform.hasChanged)
        {
            transform.hasChanged = false;
            Recalcular();
        }
    }
#endif

    /// <summary>Apaga a malha temporária para não sobrar lixo na memória.</summary>
    void OnDisable()
    {
        if (malhaGerada != null)
        {
            GetComponent<MeshFilter>().sharedMesh = PegarCubo();
            if (Application.isPlaying) Destroy(malhaGerada); else DestroyImmediate(malhaGerada);
            malhaGerada = null;
        }
    }

    /// <summary>Encontra a malha do Cube padrão da Unity (é a base de todas as peças).</summary>
    private static Mesh PegarCubo()
    {
        if (cuboOriginal == null)
            cuboOriginal = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        return cuboOriginal;
    }

    /// <summary>
    /// Copia o cubo e troca as coordenadas de textura (UV) de cada face pela posição
    /// do vértice em metros, dividida por metrosPorTextura.
    /// </summary>
    public void Recalcular()
    {
        Mesh baseMesh = PegarCubo();
        if (baseMesh == null || metrosPorTextura <= 0.001f)
            return;

        if (malhaGerada == null)
        {
            malhaGerada = Instantiate(baseMesh);
            malhaGerada.name = "Cubo_TexturaPorMetro";
            malhaGerada.hideFlags = HideFlags.DontSave;
        }

        Vector3[] vertices = baseMesh.vertices;
        Vector3[] normais = baseMesh.normals;
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = alinharAoMundo
                ? transform.TransformPoint(vertices[i])
                : Vector3.Scale(vertices[i], transform.lossyScale);
            Vector3 n = normais[i];

            Vector2 uv;
            if (Mathf.Abs(n.y) > 0.5f)
                uv = new Vector2(p.x, p.z);      // piso e teto
            else if (Mathf.Abs(n.x) > 0.5f)
                uv = new Vector2(p.z, p.y);      // paredes que olham para X
            else
                uv = new Vector2(p.x, p.y);      // paredes que olham para Z

            if (girar90)
                uv = new Vector2(uv.y, uv.x);

            uvs[i] = (uv + deslocamento) / metrosPorTextura;
        }

        malhaGerada.uv = uvs;
        GetComponent<MeshFilter>().sharedMesh = malhaGerada;
    }
}
