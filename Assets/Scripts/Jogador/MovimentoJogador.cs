using UnityEngine;
using UnityEngine.InputSystem;

// Controla a Zora: andar, correr, pular e escalar.
// Lê o teclado (e controle, se houver) no Update e aplica a física no FixedUpdate.
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MovimentoJogador : MonoBehaviour
{
    [Header("Andar e correr")]
    public float velocidadeAndar = 5.5f;
    public float velocidadeCorrer = 9f;
    public float aceleracao = 70f;      // quão rápido chega na velocidade máxima
    public float desaceleracao = 80f;   // quão rápido para ao soltar a tecla
    [Range(0f, 1f)] public float controleNoAr = 0.75f;

    [Header("Pulo")]
    public float forcaPulo = 13f;
    [Tooltip("Tempo que ainda dá para pular depois de sair da borda (coyote time)")]
    public float tempoCoyote = 0.1f;
    [Tooltip("Se apertar pular um pouco antes de tocar o chão, o pulo acontece mesmo assim")]
    public float memoriaPulo = 0.12f;
    [Tooltip("Soltar o botão no meio do pulo corta a subida (pulo de altura variável)")]
    [Range(0f, 1f)] public float cortePulo = 0.5f;
    public float quedaMaxima = 22f;

    [Header("Escada")]
    public float velocidadeEscalar = 4f;

    // Estado lido pela animação e por outros scripts
    public bool NoChao { get; private set; }
    public bool Escalando { get; private set; }
    public bool Correndo { get; private set; }
    public Vector2 Velocidade => rb.linearVelocity;
    public bool ControleAtivo { get; set; } = true;

    Rigidbody2D rb;
    Collider2D colisor;
    SpriteRenderer desenho;

    Vector2 entrada;
    float contadorCoyote;
    float contadorMemoriaPulo;
    float fimEmpurrao;
    float gravidadeOriginal;
    Escada escadaAtual;

    ContactFilter2D filtroChao;
    readonly RaycastHit2D[] acertos = new RaycastHit2D[4];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        colisor = GetComponent<Collider2D>();
        desenho = GetComponent<SpriteRenderer>();
        gravidadeOriginal = rb.gravityScale;

        // Só conta como chão superfícies sólidas viradas para cima (normal entre 45° e 135°)
        filtroChao = new ContactFilter2D { useTriggers = false };
        filtroChao.SetNormalAngle(45f, 135f);
    }

    // ---------- ENTRADA (todo frame) ----------
    void Update()
    {
        if (!ControleAtivo || Time.timeScale == 0f)
        {
            entrada = Vector2.zero;
            return;
        }

        var teclado = Keyboard.current;
        var controle = Gamepad.current;

        float h = 0f, v = 0f;
        bool apertouPulo = false, soltouPulo = false;
        Correndo = false;

        if (teclado != null)
        {
            if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) h += 1f;
            if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) h -= 1f;
            if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed) v += 1f;
            if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed) v -= 1f;
            Correndo = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
            apertouPulo = teclado.spaceKey.wasPressedThisFrame;
            soltouPulo = teclado.spaceKey.wasReleasedThisFrame;
        }
        if (controle != null)
        {
            var analogico = controle.leftStick.ReadValue() + controle.dpad.ReadValue();
            if (Mathf.Abs(analogico.x) > 0.3f) h = Mathf.Sign(analogico.x);
            if (Mathf.Abs(analogico.y) > 0.5f) v = Mathf.Sign(analogico.y);
            Correndo |= controle.buttonWest.isPressed || controle.rightTrigger.isPressed;
            apertouPulo |= controle.buttonSouth.wasPressedThisFrame;
            soltouPulo |= controle.buttonSouth.wasReleasedThisFrame;
        }

        entrada = new Vector2(h, v);

        if (apertouPulo) contadorMemoriaPulo = memoriaPulo;
        else contadorMemoriaPulo -= Time.deltaTime;

        // Pulo variável: soltou o botão enquanto subia -> corta a subida
        if (soltouPulo && !Escalando && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * cortePulo);

        // Vira o desenho para o lado em que está andando
        if (h != 0f && !Escalando) desenho.flipX = h < 0f;
    }

    // ---------- FÍSICA (passo fixo) ----------
    void FixedUpdate()
    {
        VerificarChao();

        if (NoChao) contadorCoyote = tempoCoyote;
        else contadorCoyote -= Time.fixedDeltaTime;

        if (!ControleAtivo) return;

        // Empurrão (ao levar dano): por um instante a física manda, não o jogador
        if (Time.time < fimEmpurrao) return;

        if (Escalando) { MoverNaEscada(); return; }

        // Começar a escalar: na escada + apertando para cima (ou para baixo, se estiver acima da base)
        if (escadaAtual != null)
        {
            bool sobe = entrada.y > 0.5f;
            bool desce = entrada.y < -0.5f && PesY() > escadaAtual.baseY + 0.3f;
            if (sobe || desce) { ComecarEscalar(); return; }
        }

        MoverNoChaoEAr();
    }

    void MoverNoChaoEAr()
    {
        float alvo = entrada.x * (Correndo ? velocidadeCorrer : velocidadeAndar);
        float taxa = Mathf.Abs(alvo) > 0.01f ? aceleracao : desaceleracao;
        if (!NoChao) taxa *= controleNoAr;

        float vx = Mathf.MoveTowards(rb.linearVelocity.x, alvo, taxa * Time.fixedDeltaTime);
        float vy = Mathf.Max(rb.linearVelocity.y, -quedaMaxima);

        // Pulo: apertou (há pouco) E está no chão (ou acabou de sair dele)
        if (contadorMemoriaPulo > 0f && contadorCoyote > 0f)
        {
            vy = forcaPulo;
            contadorMemoriaPulo = 0f;
            contadorCoyote = 0f;
            NoChao = false;
            GerenciadorAudio.Tocar(Som.Pulo, 0.7f);
        }

        rb.linearVelocity = new Vector2(vx, vy);
    }

    void MoverNaEscada()
    {
        // Pular solta da escada
        if (contadorMemoriaPulo > 0f)
        {
            PararEscalar();
            contadorMemoriaPulo = 0f;
            rb.linearVelocity = new Vector2(entrada.x * velocidadeAndar, forcaPulo * 0.8f);
            GerenciadorAudio.Tocar(Som.Pulo, 0.7f);
            return;
        }

        float vy = entrada.y * velocidadeEscalar;
        // Não passa do topo: para com os pés um pouco acima da borda, pronta para sair andando
        if (escadaAtual != null && PesY() >= escadaAtual.topoY && vy > 0f) vy = 0f;

        rb.linearVelocity = new Vector2(entrada.x * velocidadeEscalar * 0.6f, vy);

        // Chegou no chão descendo: solta da escada
        if (NoChao && entrada.y < 0f) PararEscalar();
    }

    void ComecarEscalar()
    {
        Escalando = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        // Centraliza na escada
        rb.position = new Vector2(escadaAtual.transform.position.x, rb.position.y);
    }

    void PararEscalar()
    {
        Escalando = false;
        rb.gravityScale = gravidadeOriginal;
    }

    void VerificarChao()
    {
        // "Projeta" o colisor 6 cm para baixo: se bater em algo sólido virado para cima, está no chão
        int qtd = colisor.Cast(Vector2.down, filtroChao, acertos, 0.06f);
        NoChao = qtd > 0 && rb.linearVelocity.y <= 0.05f;
    }

    float PesY() => colisor.bounds.min.y;

    // ---------- Chamado por outros scripts ----------

    // Dano: joga a Zora para trás por um instante
    public void Empurrar(Vector2 velocidade, float duracao)
    {
        if (Escalando) PararEscalar();
        rb.linearVelocity = velocidade;
        fimEmpurrao = Time.time + duracao;
    }

    // Quique ao pisar num inimigo
    public void Quicar(float forca)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, forca);
    }

    public void SoltarDaEscada()
    {
        if (Escalando) PararEscalar();
        escadaAtual = null;
    }

    // ---------- Detecção das escadas (gatilhos) ----------
    void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.TryGetComponent(out Escada escada)) escadaAtual = escada;
    }

    void OnTriggerStay2D(Collider2D outro)
    {
        if (escadaAtual == null && outro.TryGetComponent(out Escada escada)) escadaAtual = escada;
    }

    void OnTriggerExit2D(Collider2D outro)
    {
        if (outro.TryGetComponent(out Escada escada) && escada == escadaAtual)
        {
            escadaAtual = null;
            if (Escalando) PararEscalar();
        }
    }
}
