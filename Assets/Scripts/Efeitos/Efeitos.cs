using UnityEngine;

// Pequeno sistema de partículas feito à mão: espalha "faíscas" coloridas a partir de um ponto.
// Uso: Efeitos.Explodir(posicao, cor, quantidade, velocidade);
public static class Efeitos
{
    // Preenchidos pelo componente RecursosEfeitos que existe em cada cena
    public static Sprite SpriteParticula;
    public static Material MaterialParticula;

    public static void Explodir(Vector3 posicao, Color cor, int quantidade = 10, float velocidade = 4f)
    {
        if (SpriteParticula == null) return;
        for (int i = 0; i < quantidade; i++)
        {
            var go = new GameObject("Particula");
            go.transform.position = posicao;
            float tamanho = Random.Range(0.18f, 0.4f);
            go.transform.localScale = Vector3.one * tamanho / 0.5f; // sprite tem 0,5 unidade

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteParticula;
            if (MaterialParticula != null) sr.sharedMaterial = MaterialParticula;
            sr.color = cor;
            sr.sortingOrder = 50;

            var p = go.AddComponent<Particula>();
            float angulo = Random.Range(0f, Mathf.PI * 2f);
            p.velocidade = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * velocidade * Random.Range(0.4f, 1f);
            p.vida = Random.Range(0.35f, 0.6f);
        }
    }
}
