using UnityEngine;

/// <summary>
/// Vai no objeto das camadas de neblina de fora do prédio (Níveis 4 e 5).
/// Cada camada é um plano escuro e meio transparente. Se a câmera chega perto de uma camada
/// ou passa para o lado de fora dela, a camada some aos poucos, para não aparecer um
/// quadrado escuro na frente do jogador.
/// </summary>
public class NeblinaPerto : MonoBehaviour
{
    [Tooltip("A camada começa a sumir quando a câmera fica mais perto que isso, em metros.")]
    public float distanciaSumir = 5f;

    [Tooltip("Mais perto que isso a camada some por completo.")]
    public float distanciaInvisivel = 1.5f;

    [Tooltip("Ponto do lado de dentro de todas as camadas (o meio do prédio). Vazio = o centro deste objeto.")]
    public Transform centro;

    private Renderer[] camadas;
    private Color[] coresOriginais;
    private Bounds[] caixas;
    private MaterialPropertyBlock bloco;
    private Transform cameraPrincipal;

    /// <summary>Guarda as camadas e a cor de cada uma.</summary>
    void Start()
    {
        camadas = GetComponentsInChildren<Renderer>();
        coresOriginais = new Color[camadas.Length];
        caixas = new Bounds[camadas.Length];
        for (int i = 0; i < camadas.Length; i++)
        {
            coresOriginais[i] = camadas[i].sharedMaterial.GetColor("_BaseColor");
            caixas[i] = camadas[i].bounds;
        }
        bloco = new MaterialPropertyBlock();
    }

    /// <summary>Ajusta a transparência de cada camada conforme a câmera.</summary>
    void LateUpdate()
    {
        if (cameraPrincipal == null)
        {
            if (Camera.main == null)
                return;
            cameraPrincipal = Camera.main.transform;
        }

        Vector3 meio = centro != null ? centro.position : transform.position;
        Vector3 posCamera = cameraPrincipal.position;

        for (int i = 0; i < camadas.Length; i++)
        {
            // as camadas são paredes em pé: a normal é o lado mais fino da caixa
            Bounds caixa = caixas[i];
            Vector3 normal = caixa.size.x < caixa.size.z ? Vector3.right : Vector3.forward;
            Vector3 pontoPlano = caixa.center;

            float ladoCamera = Vector3.Dot(posCamera - pontoPlano, normal);
            float ladoMeio = Vector3.Dot(meio - pontoPlano, normal);

            float fator;
            if (Mathf.Sign(ladoCamera) != Mathf.Sign(ladoMeio))
                fator = 0f; // a câmera já está do lado de fora desta camada
            else
                fator = Mathf.InverseLerp(distanciaInvisivel, distanciaSumir, Mathf.Abs(ladoCamera));

            Color cor = coresOriginais[i];
            cor.a *= fator;
            camadas[i].GetPropertyBlock(bloco);
            bloco.SetColor("_BaseColor", cor);
            camadas[i].SetPropertyBlock(bloco);
            camadas[i].enabled = fator > 0.001f;
        }
    }
}
