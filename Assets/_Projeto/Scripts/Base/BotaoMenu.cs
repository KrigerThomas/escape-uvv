using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Visual dos botões de menu (pausa, abertura, fim de jogo): texto cinza parado,
/// e branco com um traço âmbar ao lado quando o mouse passa por cima ou o botão está selecionado.
/// Coloque junto do componente Button.
/// </summary>
public class BotaoMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Tooltip("Texto do botão.")]
    public TMP_Text texto;

    [Tooltip("Traço que aparece ao lado do botão em destaque.")]
    public GameObject marcador;

    private bool mouseEmCima;
    private bool selecionado;

    /// <summary>Começa no estado normal (sem destaque).</summary>
    void OnEnable()
    {
        mouseEmCima = false;
        selecionado = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        Atualizar();
    }

    /// <summary>O mouse entrou no botão.</summary>
    /// <param name="dados">Dados do evento (não usados).</param>
    public void OnPointerEnter(PointerEventData dados) { mouseEmCima = true; Atualizar(); }

    /// <summary>O mouse saiu do botão.</summary>
    /// <param name="dados">Dados do evento (não usados).</param>
    public void OnPointerExit(PointerEventData dados) { mouseEmCima = false; Atualizar(); }

    /// <summary>O botão foi selecionado (teclado ou ao abrir o menu).</summary>
    /// <param name="dados">Dados do evento (não usados).</param>
    public void OnSelect(BaseEventData dados) { selecionado = true; Atualizar(); }

    /// <summary>O botão deixou de estar selecionado.</summary>
    /// <param name="dados">Dados do evento (não usados).</param>
    public void OnDeselect(BaseEventData dados) { selecionado = false; Atualizar(); }

    /// <summary>Aplica a cor do texto e liga ou desliga o traço conforme o estado.</summary>
    private void Atualizar()
    {
        bool destaque = mouseEmCima || selecionado;
        if (texto != null)
            texto.color = destaque ? Paleta.Papel : Paleta.Cinza;
        if (marcador != null)
            marcador.SetActive(destaque);
    }
}
