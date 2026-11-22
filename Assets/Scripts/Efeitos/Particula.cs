using UnityEngine;

// Cada faísca: anda, cai um pouco, encolhe e some
public class Particula : MonoBehaviour
{
    public Vector2 velocidade;
    public float vida = 0.5f;

    float idade;
    SpriteRenderer sr;
    Color corInicial;
    Vector3 escalaInicial;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        corInicial = sr.color;
        escalaInicial = transform.localScale;
    }

    void Update()
    {
        idade += Time.deltaTime;
        float k = idade / vida;
        if (k >= 1f) { Destroy(gameObject); return; }

        velocidade += Vector2.down * 6f * Time.deltaTime;
        transform.position += (Vector3)(velocidade * Time.deltaTime);
        transform.localScale = escalaInicial * (1f - k);
        sr.color = new Color(corInicial.r, corInicial.g, corInicial.b, 1f - k);
    }
}
