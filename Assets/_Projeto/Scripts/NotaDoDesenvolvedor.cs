using UnityEngine;

/// <summary>
/// Componente só de documentação: guarda uma explicação em texto que aparece
/// no Inspector. Não faz nada durante o jogo. Use para explicar ao grupo
/// para que serve um objeto e como editá-lo.
/// </summary>
public class NotaDoDesenvolvedor : MonoBehaviour
{
    [TextArea(4, 20)]
    [Tooltip("Explicação para quem abrir o projeto.")]
    public string nota = "Escreva aqui para que serve este objeto.";
}
