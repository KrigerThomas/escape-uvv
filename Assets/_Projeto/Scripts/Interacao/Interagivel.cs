using UnityEngine;

/// <summary>
/// Base de tudo que o jogador usa apertando E: pegar um item, abrir uma porta,
/// entrar num armário, usar a catraca etc.
///
/// Como funciona: o objeto tem um Collider marcado como Is Trigger (a "área de alcance").
/// Quando o jogador entra nessa área, aparece a dica "Aperte E para ..." na tela.
/// Apertando E, o script filho decide o que acontece no método Interagir().
///
/// Para criar uma interação nova, faça uma classe que herda de Interagivel e escreva Interagir().
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Interagivel : MonoBehaviour
{
    /// <summary>A interação mais recente em que o jogador entrou (a que responde ao E).</summary>
    public static Interagivel Atual { get; private set; }

    [Header("Interação")]
    [Tooltip("Texto depois de \"Aperte E para\". Ex.: pegar a chave, abrir a porta.")]
    public string textoAcao = "interagir";

    [Tooltip("Tecla usada para interagir.")]
    public KeyCode tecla = KeyCode.E;

    /// <summary>Garante que o collider seja uma área de alcance (trigger).</summary>
    protected virtual void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>Lê a tecla quando esta é a interação atual.</summary>
    protected virtual void Update()
    {
        if (Atual != this || UIManager.MenuAberto)
            return;

        if (Input.GetKeyDown(tecla) && PodeInteragir())
        {
            Interagir();
            AtualizarDica();
        }
    }

    /// <summary>O jogador entrou no alcance: vira a interação atual e mostra a dica.</summary>
    void OnTriggerEnter(Collider outro)
    {
        if (!outro.CompareTag("Player"))
            return;

        Atual = this;
        AtualizarDica();
    }

    /// <summary>Se o jogador continua aqui e nenhuma outra interação está ativa, volta a ser a atual.</summary>
    void OnTriggerStay(Collider outro)
    {
        if (Atual == null && outro.CompareTag("Player"))
        {
            Atual = this;
            AtualizarDica();
        }
    }

    /// <summary>O jogador saiu do alcance: some a dica.</summary>
    void OnTriggerExit(Collider outro)
    {
        if (!outro.CompareTag("Player") || Atual != this)
            return;

        Atual = null;
        EsconderDica();
    }

    /// <summary>Se o objeto some (ex.: item pego), apaga a dica.</summary>
    protected virtual void OnDisable()
    {
        if (Atual == this)
        {
            Atual = null;
            EsconderDica();
        }
    }

    /// <summary>Diz se dá para usar agora. Por padrão, sempre.</summary>
    public virtual bool PodeInteragir()
    {
        return true;
    }

    /// <summary>O que acontece ao apertar E. Cada script filho escreve o seu.</summary>
    public abstract void Interagir();

    /// <summary>Texto da dica. Os filhos podem trocar conforme a situação.</summary>
    protected virtual string TextoDica()
    {
        return textoAcao;
    }

    /// <summary>Mostra ou esconde a dica "Aperte E para ...".</summary>
    protected void AtualizarDica()
    {
        if (Atual != this || UI == null)
            return;

        if (PodeInteragir())
            UI.MostrarDica("Aperte <b>" + tecla + "</b> para " + TextoDica());
        else
            UI.EsconderDica();
    }

    /// <summary>Esconde a dica da tela.</summary>
    protected void EsconderDica()
    {
        if (UI != null)
            UI.EsconderDica();
    }

    /// <summary>Mostra um aviso curto no meio da tela.</summary>
    protected void Avisar(string texto)
    {
        if (UI != null && !string.IsNullOrEmpty(texto))
            UI.MostrarAviso(texto);
    }

    /// <summary>Atalho para a interface do nível.</summary>
    protected UIManager UI
    {
        get { return GameManager.Instancia != null ? GameManager.Instancia.ui : null; }
    }
}
