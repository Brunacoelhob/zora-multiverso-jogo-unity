using System.Collections;
using UnityEngine;

// Portal do multiverso no fim de cada fase.
// Fica FECHADO (cinza, girando devagar) até a Zora juntar todos os fragmentos de estrela.
[RequireComponent(typeof(SpriteRenderer))]
public class Portal : MonoBehaviour
{
    public float giroFechado = 25f;
    public float giroAberto = 220f;

    SpriteRenderer desenho;
    bool entrando;
    float proximoAviso;

    bool Aberto => ConfiguracaoPlaneta.Atual == null || ConfiguracaoPlaneta.Atual.PortalAberto;

    void Awake()
    {
        desenho = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (ConfiguracaoPlaneta.Atual != null) ConfiguracaoPlaneta.Atual.PortalAbriu += AoAbrir;
    }

    void OnDestroy()
    {
        if (ConfiguracaoPlaneta.Atual != null) ConfiguracaoPlaneta.Atual.PortalAbriu -= AoAbrir;
    }

    void Update()
    {
        bool aberto = Aberto;
        transform.Rotate(0f, 0f, -(aberto ? giroAberto : giroFechado) * Time.deltaTime);

        if (aberto)
        {
            float pulso = 1f + Mathf.Sin(Time.time * 5f) * 0.06f;
            transform.localScale = Vector3.one * pulso;
            desenho.color = Color.white;
        }
        else
        {
            transform.localScale = Vector3.one * 0.85f;
            desenho.color = new Color(0.55f, 0.55f, 0.6f, 0.55f);
        }
    }

    void AoAbrir()
    {
        GerenciadorAudio.Tocar(Som.PortalAbriu);
        Efeitos.Explodir(transform.position, new Color(0.85f, 0.5f, 1f), 24, 6f);
        HUD.Mensagem("Todos os fragmentos! O portal se abriu!");
    }

    // "Stay" (e não só "Enter"): se ela já estiver parada no portal quando ele abrir, entra mesmo assim
    void OnTriggerStay2D(Collider2D outro)
    {
        if (entrando || !outro.TryGetComponent(out MovimentoJogador jogadora)) return;

        if (!Aberto)
        {
            if (Time.time < proximoAviso) return;
            proximoAviso = Time.time + 2f;
            var p = ConfiguracaoPlaneta.Atual;
            HUD.Mensagem($"Portal fechado! Fragmentos de estrela: {p.FragmentosColetados}/{p.TotalFragmentos}");
            return;
        }

        StartCoroutine(Entrar(jogadora));
    }

    // Animação de teletransporte: a Zora gira, encolhe e é puxada para o centro
    IEnumerator Entrar(MovimentoJogador jogadora)
    {
        entrando = true;
        jogadora.ControleAtivo = false;
        jogadora.SoltarDaEscada();
        var rb = jogadora.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        GerenciadorAudio.Tocar(Som.Teletransporte);

        Vector3 inicio = jogadora.transform.position;
        Vector3 escalaInicial = jogadora.transform.localScale;
        const float duracao = 0.9f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = t / duracao;
            jogadora.transform.position = Vector3.Lerp(inicio, transform.position, k);
            jogadora.transform.localScale = escalaInicial * (1f - k);
            jogadora.transform.Rotate(0f, 0f, 720f * Time.deltaTime);
            yield return null;
        }
        Efeitos.Explodir(transform.position, Color.white, 30, 7f);
        yield return new WaitForSeconds(0.4f);
        ConfiguracaoPlaneta.Atual.IrParaProximaFase();
    }
}
