using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

/// <summary>
/// Faz a água da fonte do Nível 2 correr sem parar.
/// A fonte vem de um arquivo Alembic (.abc), que guarda a simulação da água quadro a quadro.
/// Este script avança o tempo da simulação e volta ao começo quando ela acaba (loop).
/// </summary>
[RequireComponent(typeof(AlembicStreamPlayer))]
public class AnimacaoFonte : MonoBehaviour
{
    [Tooltip("Velocidade da água (1 = a velocidade original da simulação).")]
    public float velocidade = 1f;

    private AlembicStreamPlayer player;
    private float tempo;

    /// <summary>Pega o player do Alembic e começa num ponto sorteado da simulação.</summary>
    void Start()
    {
        player = GetComponent<AlembicStreamPlayer>();
        tempo = Random.Range(0f, (float)player.Duration);
    }

    /// <summary>Avança o tempo da simulação e repete quando chega no fim.</summary>
    void Update()
    {
        float duracao = (float)player.Duration;
        if (duracao <= 0f)
            return;

        tempo = (tempo + Time.deltaTime * velocidade) % duracao;
        player.CurrentTime = (float)player.StartTime + tempo;
    }
}
