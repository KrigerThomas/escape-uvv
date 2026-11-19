using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Espelho de verdade: uma segunda câmera fica "atrás" do espelho, na posição
// refletida da câmera do jogador, e desenha a cena numa textura. O material
// do espelho (shader EscapeUVV/Espelho) mostra essa textura.
// Para economizar, só desenha quando o jogador está perto e o espelho aparece na tela.
[RequireComponent(typeof(Renderer))]
public class Espelho : MonoBehaviour
{
    [Tooltip("Direção para onde o espelho está virado (no espaço do próprio objeto).")]
    public Vector3 frenteLocal = Vector3.right;

    [Tooltip("Distância máxima da câmera para o reflexo ser desenhado.")]
    public float distanciaMaxima = 12f;

    [Tooltip("1 = resolução da tela, 0.5 = metade (mais leve).")]
    [Range(0.25f, 1f)] public float qualidade = 0.6f;

    private Renderer rend;
    private Camera camReflexo;
    private RenderTexture textura;
    private Camera camPrincipal;
    private static readonly int ID_TEXTURA = Shader.PropertyToID("_ReflexoTex");

    void Start()
    {
        rend = GetComponent<Renderer>();
        GameObject g = new GameObject("Camera_Reflexo_" + name);
        g.hideFlags = HideFlags.DontSave;
        camReflexo = g.AddComponent<Camera>();
        camReflexo.enabled = false;
        var dados = g.AddComponent<UniversalAdditionalCameraData>();
        dados.renderPostProcessing = false;
        dados.requiresDepthTexture = false;
        dados.requiresColorTexture = false;
        RenderPipelineManager.beginCameraRendering += AntesDeDesenhar;
    }

    void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= AntesDeDesenhar;
        if (camReflexo != null) Destroy(camReflexo.gameObject);
        if (textura != null) textura.Release();
    }

    void LateUpdate()
    {
        camPrincipal = Camera.main;
        bool ativo = camPrincipal != null && rend.isVisible &&
                     Vector3.Distance(camPrincipal.transform.position, transform.position) < distanciaMaxima;
        camReflexo.enabled = ativo;
        if (!ativo) return;

        int w = Mathf.Max(64, (int)(camPrincipal.pixelWidth * qualidade));
        int h = Mathf.Max(64, (int)(camPrincipal.pixelHeight * qualidade));
        if (textura == null || textura.width != w || textura.height != h)
        {
            if (textura != null) textura.Release();
            textura = new RenderTexture(w, h, 24, RenderTextureFormat.DefaultHDR) { name = "Reflexo_" + name };
            camReflexo.targetTexture = textura;
            rend.material.SetTexture(ID_TEXTURA, textura);
        }

        camReflexo.CopyFrom(camPrincipal);
        camReflexo.targetTexture = textura;
        camReflexo.depth = camPrincipal.depth - 10;
        camReflexo.enabled = true;
    }

    // Chamado logo antes de cada câmera desenhar: posiciona a câmera do reflexo
    // já com a posição final da câmera do jogador (sem atraso de um quadro).
    private void AntesDeDesenhar(ScriptableRenderContext contexto, Camera cam)
    {
        if (cam != camReflexo || camPrincipal == null) return;

        Vector3 normal = transform.TransformDirection(frenteLocal).normalized;
        Vector3 ponto = rend.bounds.center + normal * (rend.bounds.extents.x * Mathf.Abs(normal.x) + rend.bounds.extents.z * Mathf.Abs(normal.z));

        Transform cp = camPrincipal.transform;
        Vector3 pos = cp.position - 2f * Vector3.Dot(cp.position - ponto, normal) * normal;
        Vector3 frente = Vector3.Reflect(cp.forward, normal);
        Vector3 cima = Vector3.Reflect(cp.up, normal);
        camReflexo.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(frente, cima));

        // Corta tudo que está atrás do espelho (a parede e o corredor)
        camReflexo.ResetProjectionMatrix();
        camReflexo.projectionMatrix = camPrincipal.projectionMatrix;
        Matrix4x4 m = camReflexo.worldToCameraMatrix;
        Vector3 pCam = m.MultiplyPoint(ponto + normal * 0.01f);
        Vector3 nCam = m.MultiplyVector(normal).normalized;
        Vector4 plano = new Vector4(nCam.x, nCam.y, nCam.z, -Vector3.Dot(pCam, nCam));
        camReflexo.projectionMatrix = camReflexo.CalculateObliqueMatrix(plano);
    }
}
