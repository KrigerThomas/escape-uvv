using UnityEngine;

/// <summary>
/// Câmera em terceira pessoa que orbita ao redor do jogador com o mouse.
/// Se houver uma parede entre o jogador e a câmera, a câmera se aproxima para não atravessá-la.
/// A tecla Esc libera o cursor (útil para clicar nos botões) e um clique com o botão esquerdo trava o cursor de novo.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Alvo")]
    [Tooltip("Quem a câmera segue. Se vazio, procura o objeto com a tag Player.")]
    public Transform alvo;

    [Tooltip("Ponto de mira em relação ao alvo. Use o eixo Y para mirar na altura da cabeça.")]
    public Vector3 deslocamentoAlvo = new Vector3(0f, 0.8f, 0f);

    [Header("Órbita")]
    [Tooltip("Distância normal entre a câmera e o alvo, em metros.")]
    public float distancia = 4f;

    [Tooltip("Sensibilidade do mouse.")]
    public float sensibilidade = 3f;

    [Tooltip("Inclinação mínima da câmera (valor negativo olha para cima).")]
    public float anguloMinimo = -15f;

    [Tooltip("Inclinação máxima da câmera (valor positivo olha para baixo).")]
    public float anguloMaximo = 60f;

    [Header("Colisão da câmera")]
    [Tooltip("Raio da esfera usada para detectar paredes entre o alvo e a câmera.")]
    public float raioColisao = 0.3f;

    [Tooltip("Camadas que a câmera não pode atravessar.")]
    public LayerMask camadasObstaculo = ~0;

    private float yaw;
    private float pitch;
    private float distanciaAtual;

    /// <summary>Procura o jogador e começa olhando na mesma direção que ele.</summary>
    void Start()
    {
        if (alvo == null)
        {
            GameObject jogador = GameObject.FindGameObjectWithTag("Player");
            if (jogador != null)
                alvo = jogador.transform;
        }

        yaw = alvo != null ? alvo.eulerAngles.y : transform.eulerAngles.y;
        pitch = 15f;
        distanciaAtual = distancia;
    }

    /// <summary>Esc solta o cursor do mouse e um clique prende o cursor de volta (fora de menus).</summary>
    void Update()
    {
        if (UIManager.MenuAberto)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TravarCursor(false);
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            TravarCursor(true);
    }

    /// <summary>
    /// Gira a câmera com o mouse, calcula a distância livre até a parede mais próxima
    /// e posiciona a câmera atrás do alvo, olhando para ele.
    /// </summary>
    void LateUpdate()
    {
        if (alvo == null)
            return;

        if (Cursor.lockState == CursorLockMode.Locked && !UIManager.MenuAberto)
        {
            yaw += Input.GetAxis("Mouse X") * sensibilidade;
            pitch -= Input.GetAxis("Mouse Y") * sensibilidade;
            pitch = Mathf.Clamp(pitch, anguloMinimo, anguloMaximo);
        }

        Quaternion rotacao = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivo = alvo.position + deslocamentoAlvo;
        Vector3 direcao = rotacao * Vector3.back;

        float livre = DistanciaLivre(pivo, direcao);
        if (livre < distanciaAtual)
            distanciaAtual = livre;
        else
            distanciaAtual = Mathf.Lerp(distanciaAtual, livre, 6f * Time.unscaledDeltaTime);

        transform.position = pivo + direcao * distanciaAtual;
        transform.rotation = rotacao;
    }

    /// <summary>
    /// Mede até onde a câmera pode ir sem atravessar uma parede.
    /// </summary>
    /// <param name="pivo">Ponto de onde a câmera "sai" (a cabeça do jogador).</param>
    /// <param name="direcao">Direção para trás do jogador, de onde a câmera olha.</param>
    /// <returns>A distância livre, nunca menor que 0,8 m e nunca maior que a distância normal.</returns>
    private float DistanciaLivre(Vector3 pivo, Vector3 direcao)
    {
        float menor = distancia;
        RaycastHit[] batidas = Physics.SphereCastAll(pivo, raioColisao, direcao, distancia, camadasObstaculo, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < batidas.Length; i++)
        {
            if (batidas[i].transform.IsChildOf(alvo))
                continue;

            if (batidas[i].distance > 0f && batidas[i].distance < menor)
                menor = batidas[i].distance;
        }

        return Mathf.Max(menor, 0.8f);
    }

    /// <summary>Trava ou solta o cursor do mouse.</summary>
    /// <param name="travar">Verdadeiro para prender e esconder o cursor, falso para liberar.</param>
    private void TravarCursor(bool travar)
    {
        Cursor.lockState = travar ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !travar;
    }
}
