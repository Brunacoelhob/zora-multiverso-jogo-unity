using UnityEngine;

// Faz um elemento (imagem da Zora no menu, título) flutuar suavemente para cima e para baixo.
public class Flutuar : MonoBehaviour
{
    public float amplitude = 12f;
    public float velocidade = 1.5f;
    public float giro = 3f; // inclinação em graus

    Vector3 posicaoInicial;

    void Start()
    {
        posicaoInicial = transform.localPosition;
    }

    void Update()
    {
        float t = Time.time * velocidade;
        transform.localPosition = posicaoInicial + Vector3.up * Mathf.Sin(t) * amplitude;
        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.7f) * giro);
    }
}
