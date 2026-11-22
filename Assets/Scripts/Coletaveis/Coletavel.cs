using System.Collections;
using UnityEngine;

public enum TipoColetavel
{
    Moeda,      // pontuação
    Fragmento,  // fragmento de estrela: precisa de todos para abrir o portal
    Coracao     // +1 vida
}

// Item que a Zora pega ao encostar. Flutua, dá feedback visual (partículas + "pop")
// e sonoro, e atualiza o estado do jogo.
public class Coletavel : MonoBehaviour
{
    public TipoColetavel tipo = TipoColetavel.Moeda;
    public int valor = 1;
    public float alturaFlutuar = 0.12f;
    public float velocidadeFlutuar = 3f;

    Vector3 posicaoInicial;
    float fase;
    bool coletado;

    void Start()
    {
        posicaoInicial = transform.position;
        fase = Random.value * 10f;
    }

    void Update()
    {
        if (coletado) return;
        float y = Mathf.Sin(Time.time * velocidadeFlutuar + fase) * alturaFlutuar;
        transform.position = posicaoInicial + Vector3.up * y;

        // O fragmento de estrela brilha (pulsa) e gira devagar para chamar atenção
        if (tipo == TipoColetavel.Fragmento)
        {
            transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 6f + fase) * 0.1f);
            transform.Rotate(0f, 0f, 40f * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (coletado || !outro.TryGetComponent(out VidaJogador vida) || vida.Morta) return;
        coletado = true;

        switch (tipo)
        {
            case TipoColetavel.Moeda:
                EstadoJogo.Moedas += valor;
                GerenciadorAudio.Tocar(Som.Moeda, 0.6f);
                Efeitos.Explodir(transform.position, new Color(1f, 0.85f, 0.2f), 8, 3.5f);
                break;

            case TipoColetavel.Fragmento:
                ConfiguracaoPlaneta.Atual?.RegistrarFragmento();
                GerenciadorAudio.Tocar(Som.Fragmento);
                Efeitos.Explodir(transform.position, new Color(1f, 1f, 0.6f), 20, 5.5f);
                CameraSegue.Tremer(0.12f, 0.06f);
                var p = ConfiguracaoPlaneta.Atual;
                if (p != null && !p.PortalAberto)
                    HUD.Mensagem($"Fragmento de estrela! {p.FragmentosColetados}/{p.TotalFragmentos}");
                break;

            case TipoColetavel.Coracao:
                vida.GanharVida();
                GerenciadorAudio.Tocar(Som.Coracao);
                Efeitos.Explodir(transform.position, new Color(1f, 0.3f, 0.4f), 12, 4f);
                HUD.Mensagem("+1 vida!");
                break;
        }

        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(Sumir());
    }

    // "Pop": cresce e some rapidinho
    IEnumerator Sumir()
    {
        var desenho = GetComponent<SpriteRenderer>();
        Vector3 escala = transform.localScale;
        const float duracao = 0.18f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = t / duracao;
            transform.localScale = escala * (1f + k * 0.8f);
            if (desenho != null) desenho.color = new Color(1f, 1f, 1f, 1f - k);
            yield return null;
        }
        Destroy(gameObject);
    }
}
