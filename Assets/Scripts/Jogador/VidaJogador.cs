using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Vidas da Zora: dano, invencibilidade temporária (piscando), quedas em buracos,
// checkpoint e Game Over. Também decide se ela PISOU num inimigo ou foi atingida.
[RequireComponent(typeof(MovimentoJogador))]
public class VidaJogador : MonoBehaviour
{
    public float tempoInvencivel = 1.4f;
    public float tempoMostrarDano = 0.35f;
    public float forcaQuiqueInimigo = 11f;

    public bool Invencivel => Time.time < fimInvencivel;
    public bool MostrandoDano => Time.time < fimDano;
    public bool Morta { get; private set; }

    MovimentoJogador movimento;
    Rigidbody2D rb;
    Collider2D colisor;
    SpriteRenderer desenho;
    Vector3 pontoRetorno;
    float fimInvencivel;
    float fimDano;

    void Awake()
    {
        movimento = GetComponent<MovimentoJogador>();
        rb = GetComponent<Rigidbody2D>();
        colisor = GetComponent<Collider2D>();
        desenho = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        pontoRetorno = transform.position;
    }

    void Update()
    {
        // Piscar enquanto está invencível (feedback visual de dano)
        if (!Morta)
        {
            float alfa = Invencivel ? (Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.3f : 1f) : 1f;
            desenho.color = new Color(1f, 1f, 1f, alfa);
        }

        var planeta = ConfiguracaoPlaneta.Atual;
        if (!Morta && planeta != null && transform.position.y < planeta.limiteQueda) CaiuNoBuraco();
    }

    public void DefinirCheckpoint(Vector3 posicao) => pontoRetorno = posicao;

    public void GanharVida()
    {
        EstadoJogo.Vidas = Mathf.Min(EstadoJogo.Vidas + 1, EstadoJogo.VidasMaximas);
    }

    public void TomarDano(Vector2 origem)
    {
        if (Invencivel || Morta) return;

        EstadoJogo.Vidas--;
        GerenciadorAudio.Tocar(Som.Dano);
        CameraSegue.Tremer(0.25f, 0.18f);
        Efeitos.Explodir(transform.position, new Color(1f, 0.4f, 0.5f), 10, 5f);

        fimInvencivel = Time.time + tempoInvencivel;
        fimDano = Time.time + tempoMostrarDano;

        // Empurra para o lado oposto ao que causou o dano
        float lado = transform.position.x >= origem.x ? 1f : -1f;
        movimento.Empurrar(new Vector2(lado * 6f, 8f), 0.25f);

        if (EstadoJogo.Vidas <= 0) Morrer();
    }

    void CaiuNoBuraco()
    {
        EstadoJogo.Vidas--;
        GerenciadorAudio.Tocar(Som.Dano);
        if (EstadoJogo.Vidas <= 0) { Morrer(); return; }

        // Volta para o último checkpoint
        movimento.SoltarDaEscada();
        rb.linearVelocity = Vector2.zero;
        rb.position = pontoRetorno;
        transform.position = pontoRetorno;
        fimInvencivel = Time.time + tempoInvencivel;
        HUD.Mensagem("Cuidado com os buracos!");
    }

    void Morrer()
    {
        if (Morta) return;
        Morta = true;
        movimento.ControleAtivo = false;
        movimento.SoltarDaEscada();
        fimDano = float.MaxValue; // fica com a cara de "dano"
        desenho.color = Color.white;
        // Pulinho para cima e cai atravessando tudo (clássico dos jogos de plataforma)
        colisor.enabled = false;
        rb.linearVelocity = new Vector2(0f, 12f);
        StartCoroutine(IrParaGameOver());
    }

    IEnumerator IrParaGameOver()
    {
        yield return new WaitForSeconds(1.6f);
        SceneManager.LoadScene(Cenas.GameOver);
    }

    // ---------- Encostou num inimigo: pisou ou foi atingida? ----------
    void OnCollisionEnter2D(Collision2D colisao) => VerificarInimigo(colisao);
    void OnCollisionStay2D(Collision2D colisao) => VerificarInimigo(colisao);

    void VerificarInimigo(Collision2D colisao)
    {
        if (Morta) return;
        if (!colisao.collider.TryGetComponent(out Inimigo inimigo) || inimigo.Morto) return;

        // Pisou = o contato empurra a Zora para CIMA, ou os pés estão acima do meio do inimigo
        bool pisou = false;
        for (int i = 0; i < colisao.contactCount; i++)
            if (colisao.GetContact(i).normal.y > 0.5f) pisou = true;
        if (colisor.bounds.min.y > colisao.collider.bounds.center.y) pisou = true;

        if (pisou)
        {
            inimigo.Derrotar();
            movimento.Quicar(forcaQuiqueInimigo);
        }
        else
        {
            TomarDano(colisao.collider.bounds.center);
        }
    }
}
