using UnityEngine;

/// <summary>
/// Dica "F  Ligar a lanterna" da interface (Nível 4, no apagão).
/// Aparece quando algum script chama LanternaCelular.PedirParaLigar()
/// e só some quando o jogador liga a lanterna.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DicaLanterna : MonoBehaviour
{
    [Tooltip("Segundos para aparecer e sumir.")]
    public float tempoFade = 0.3f;

    private CanvasGroup grupo;

    /// <summary>Começa escondida.</summary>
    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
    }

    /// <summary>Aparece e some suavemente conforme o pedido da lanterna.</summary>
    void Update()
    {
        float alvo = LanternaCelular.PedindoParaLigar && !UIManager.MenuAberto ? 1f : 0f;
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, alvo, Time.unscaledDeltaTime / Mathf.Max(0.01f, tempoFade));
    }
}
