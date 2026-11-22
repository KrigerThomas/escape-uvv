using System.Collections;
using UnityEngine;

/// <summary>
/// Faz um som em loop (ex.: zumbido das lâmpadas) começar mudo e subir devagar
/// até o volume escolhido, depois de uma espera (ex.: o tempo da tela preta da abertura).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SomEntrandoAosPoucos : MonoBehaviour
{
    [Range(0f, 1f)] public float volumeFinal = 0.2f;

    [Tooltip("Segundos antes de começar a subir.")]
    public float espera = 3f;

    [Tooltip("Segundos para ir do mudo ao volume final.")]
    public float duracao = 6f;

    /// <summary>Zera o volume e começa a subida.</summary>
    void Start()
    {
        AudioSource fonte = GetComponent<AudioSource>();
        fonte.volume = 0f;
        StartCoroutine(Subir(fonte));
    }

    /// <summary>Espera um pouco e sobe o volume do zero até o volume final.</summary>
    /// <param name="fonte">Fonte de som que vai subir de volume.</param>
    private IEnumerator Subir(AudioSource fonte)
    {
        yield return new WaitForSeconds(espera);
        float t = 0f;
        while (t < duracao && fonte != null)
        {
            t += Time.deltaTime;
            fonte.volume = Mathf.Lerp(0f, volumeFinal, t / duracao);
            yield return null;
        }
        if (fonte != null)
            fonte.volume = volumeFinal;
    }
}
