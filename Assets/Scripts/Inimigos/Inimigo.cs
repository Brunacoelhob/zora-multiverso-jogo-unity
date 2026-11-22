using UnityEngine;

// Inimigos do jogo.
//  - Andante (slimes, caracol): anda para um lado e vira ao bater numa parede ou chegar na borda.
//  - Voador (abelha): voa de um lado para o outro, ondulando para cima e para baixo.
// A Zora derrota um inimigo pulando em cima dele (quem decide isso é o VidaJogador).
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Inimigo : MonoBehaviour
{
    public enum TipoMovimento { Andante, Voador }

    public TipoMovimento tipo = TipoMovimento.Andante;
    public float velocidade = 1.6f;

    [Header("Voador")]
    public float distanciaPatrulha = 3f;
    public float amplitudeVoo = 0.35f;

    [Header("Derrota")]
    public Sprite spriteDerrotado;
    public int moedasDeBonus = 2;

    [Tooltip("Para qual lado o desenho original olha. Kenney: inimigos olham para a esquerda.")]
    public bool desenhoOlhaParaEsquerda = true;

    public bool Morto { get; private set; }

    Rigidbody2D rb;
    Collider2D colisor;
    SpriteRenderer desenho;
    Animator animador;
    int direcao = -1;
    float proximaVirada;
    Vector2 origem;
    float tempoVoo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        colisor = GetComponent<Collider2D>();
        desenho = GetComponent<SpriteRenderer>();
        animador = GetComponent<Animator>();
    }

    void Start()
    {
        origem = rb.position;
        tempoVoo = Random.value * 5f;
    }

    void FixedUpdate()
    {
        if (Morto) return;
        if (tipo == TipoMovimento.Andante) Andar();
        else Voar();
        desenho.flipX = desenhoOlhaParaEsquerda ? direcao > 0 : direcao < 0;
    }

    void Andar()
    {
        if (Time.time >= proximaVirada && (TemParedeNaFrente() || NaoTemChaoNaFrente()))
        {
            direcao = -direcao;
            proximaVirada = Time.time + 0.25f; // evita ficar virando sem parar
        }
        rb.linearVelocity = new Vector2(direcao * velocidade, rb.linearVelocity.y);
    }

    bool TemParedeNaFrente()
    {
        var b = colisor.bounds;
        var origemRaio = new Vector2(b.center.x + direcao * (b.extents.x + 0.02f), b.center.y);
        var acerto = Physics2D.Raycast(origemRaio, Vector2.right * direcao, 0.1f);
        // A Zora não conta como parede (senão o inimigo fugiria dela)
        return acerto.collider != null && !acerto.collider.GetComponent<VidaJogador>();
    }

    bool NaoTemChaoNaFrente()
    {
        var b = colisor.bounds;
        var origemRaio = new Vector2(b.center.x + direcao * (b.extents.x + 0.05f), b.min.y + 0.05f);
        return Physics2D.Raycast(origemRaio, Vector2.down, 0.4f).collider == null;
    }

    void Voar()
    {
        tempoVoo += Time.fixedDeltaTime;
        float vaiEVolta = Mathf.PingPong(tempoVoo * velocidade, distanciaPatrulha * 2f) - distanciaPatrulha;
        var nova = new Vector2(origem.x + vaiEVolta, origem.y + Mathf.Sin(tempoVoo * 3f) * amplitudeVoo);
        direcao = nova.x >= rb.position.x ? 1 : -1;
        rb.MovePosition(nova);
    }

    public void Derrotar()
    {
        if (Morto) return;
        Morto = true;

        GerenciadorAudio.Tocar(Som.PisarInimigo);
        Efeitos.Explodir(transform.position, new Color(0.7f, 1f, 0.6f), 10, 4f);
        EstadoJogo.Moedas += moedasDeBonus;
        EstadoJogo.InimigosDerrotados++;

        if (animador != null) animador.enabled = false;
        if (spriteDerrotado != null) desenho.sprite = spriteDerrotado;
        else transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.4f, 1f); // "amassa"

        colisor.enabled = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        Destroy(gameObject, 0.5f);
    }
}
