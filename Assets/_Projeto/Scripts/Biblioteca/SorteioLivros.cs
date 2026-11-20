using System.Text;
using UnityEngine;

/// <summary>
/// Sorteia, a cada partida, em qual lugar cada livro do Nível 4 vai estar.
/// Cada livro tem uma lista de lugares possíveis (objetos vazios dentro das estantes
/// ou em cima das mesas). O lugar sorteado vira o texto da lista impressa no terminal.
/// </summary>
public class SorteioLivros : MonoBehaviour
{
    /// <summary>Um lugar possível para um livro.</summary>
    [System.Serializable]
    public class Lugar
    {
        [Tooltip("Objeto vazio que marca a posição e a rotação do livro.")]
        public Transform ponto;

        [Tooltip("Como o lugar aparece na lista (ex.: Térreo · Estante 03).")]
        public string descricao;
    }

    /// <summary>Um livro e os lugares onde ele pode aparecer.</summary>
    [System.Serializable]
    public class LivroSorteado
    {
        public Livro livro;

        [Tooltip("Número de chamada do livro, só para enfeitar a lista.")]
        public string chamada;

        public Lugar[] lugares;

        [HideInInspector] public string lugarSorteado;
    }

    /// <summary>Acesso rápido ao sorteio da cena.</summary>
    public static SorteioLivros Instancia { get; private set; }

    public LivroSorteado[] livros;

    /// <summary>Sorteia os lugares antes de qualquer outro script usar os livros.</summary>
    void Awake()
    {
        Instancia = this;

        foreach (LivroSorteado item in livros)
        {
            if (item.livro == null || item.lugares == null || item.lugares.Length == 0)
                continue;

            Lugar lugar = item.lugares[Random.Range(0, item.lugares.Length)];
            if (lugar.ponto != null)
                item.livro.transform.SetPositionAndRotation(lugar.ponto.position, lugar.ponto.rotation);
            item.lugarSorteado = lugar.descricao;
        }
    }

    /// <summary>Texto da lista: título, número de chamada e onde está. Livro já pego aparece riscado.</summary>
    public string TextoLista()
    {
        StringBuilder texto = new StringBuilder("<b>Livros para pegar</b>\n<size=80%>Consulta ao acervo · Biblioteca UVV</size>\n");
        int numero = 1;
        foreach (LivroSorteado item in livros)
        {
            if (item.livro == null)
                continue;

            bool pego = Inventario.Tem("Livro: " + item.livro.titulo);
            string linha = numero + ". <b>" + item.livro.titulo + "</b> · " + item.chamada + "\n    " + item.lugarSorteado;
            texto.Append("\n");
            texto.Append(pego ? "<color=#6b6f86><s>" + linha + "</s>  (pego)</color>" : linha);
            texto.Append("\n");
            numero++;
        }
        return texto.ToString();
    }

    /// <summary>Limpa a referência ao trocar de cena.</summary>
    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }
}
