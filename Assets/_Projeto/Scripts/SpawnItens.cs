using UnityEngine;

/// <summary>
/// Sorteia onde os itens perdidos aparecem. No início da cena, embaralha
/// o vetor de pontos e instancia um prefab de item em cada um dos primeiros pontos.
/// </summary>
public class SpawnItens : MonoBehaviour
{
    [Tooltip("Vetor de prefabs: um prefab por item perdido.")]
    public GameObject[] prefabsItens;

    [Tooltip("Vetor de posições possíveis. Precisa ter pelo menos tantos pontos quanto itens.")]
    public Transform[] pontosSpawn;

    /// <summary>Faz o sorteio assim que a cena começa.</summary>
    void Start()
    {
        SortearItens();
    }

    /// <summary>
    /// Embaralha os pontos (Fisher-Yates) e instancia cada prefab em um ponto diferente.
    /// </summary>
    public void SortearItens()
    {
        if (prefabsItens == null || pontosSpawn == null || pontosSpawn.Length < prefabsItens.Length)
        {
            Debug.LogWarning("SpawnItens: faltam pontos de spawn para a quantidade de itens.");
            return;
        }

        Transform[] pontos = (Transform[])pontosSpawn.Clone();
        for (int i = pontos.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Transform temp = pontos[i];
            pontos[i] = pontos[j];
            pontos[j] = temp;
        }

        for (int i = 0; i < prefabsItens.Length; i++)
            Instantiate(prefabsItens[i], pontos[i].position, pontos[i].rotation, transform);
    }

    /// <summary>Desenha os pontos de spawn na Scene View para facilitar o posicionamento.</summary>
    void OnDrawGizmos()
    {
        if (pontosSpawn == null)
            return;

        Gizmos.color = Color.yellow;
        foreach (Transform p in pontosSpawn)
            if (p != null)
                Gizmos.DrawWireSphere(p.position, 0.25f);
    }
}
