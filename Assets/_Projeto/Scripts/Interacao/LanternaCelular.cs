using UnityEngine;

/// <summary>
/// Lanterna do celular (fica no Jogador). Só funciona depois que o jogador pega o item "Celular".
/// A tecla F liga e desliga. Atenção: com a lanterna ligada, a Professora vê o jogador de mais longe.
/// </summary>
public class LanternaCelular : MonoBehaviour
{
    /// <summary>Verdadeiro enquanto a lanterna está acesa.</summary>
    public static bool Ligada { get; private set; }

    [Tooltip("Spot Light filha do jogador, apontando para a frente.")]
    public Light luz;

    [Tooltip("Nome do item que libera a lanterna.")]
    public string itemNecessario = "Celular";

    public KeyCode tecla = KeyCode.F;
    public AudioClip somClique;

    /// <summary>Começa apagada.</summary>
    void Start()
    {
        Ligada = false;
        if (luz != null)
            luz.enabled = false;
    }

    /// <summary>Liga/desliga com F. Dentro do armário, a lanterna apaga sozinha.</summary>
    void Update()
    {
        if (luz == null || !Inventario.Tem(itemNecessario))
            return;

        if (ArmarioEsconderijo.JogadorDentro && Ligada)
            Trocar();

        if (Input.GetKeyDown(tecla) && !UIManager.MenuAberto && !ArmarioEsconderijo.JogadorDentro)
            Trocar();
    }

    /// <summary>Inverte o estado da lanterna.</summary>
    private void Trocar()
    {
        Ligada = !Ligada;
        luz.enabled = Ligada;
        UIManager.TocarSom2D(somClique, 0.5f);
    }

    /// <summary>Apaga ao sair da cena.</summary>
    void OnDestroy()
    {
        Ligada = false;
    }
}
