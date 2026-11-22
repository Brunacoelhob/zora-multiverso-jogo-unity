using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Um por fase. Define o planeta: nome, gravidade, textos e qual é a próxima fase.
// É aqui que a FÍSICA de cada planeta é aplicada.
public class ConfiguracaoPlaneta : MonoBehaviour
{
    public static ConfiguracaoPlaneta Atual { get; private set; }

    [Header("Planeta")]
    public string nomePlaneta = "Terra";
    public int numeroFase = 1;
    [Tooltip("Gravidade REAL do planeta em m/s² (Terra = 9,81)")]
    public float gravidadeReal = 9.81f;
    [TextArea] public string curiosidade;
    [TextArea] public string dica;

    [Header("Fluxo")]
    public string proximaCena = Cenas.Vitoria;
    [Tooltip("Abaixo desta altura a Zora caiu no buraco")]
    public float limiteQueda = -3f;

    // Gravidade que o jogo usa para a Terra. Escolhida para o pulo na Terra ter ~3 blocos.
    public const float GravidadeTerraNoJogo = 30f;
    public const float GravidadeTerraReal = 9.81f;

    // A gravidade real varia MUITO (Júpiter é 15x a Lua). Se usássemos a proporção direta,
    // na Lua o pulo teria 18 blocos e em Júpiter meio bloco — impossível de jogar.
    // Por isso usamos a RAIZ CÚBICA da proporção: a ordem e a sensação de cada planeta
    // continuam corretas (Lua leve, Júpiter pesado), mas tudo fica jogável.
    public float GravidadeNoJogo =>
        GravidadeTerraNoJogo * Mathf.Pow(gravidadeReal / GravidadeTerraReal, 1f / 3f);

    public int TotalFragmentos { get; private set; }
    public int FragmentosColetados { get; private set; }
    public bool PortalAberto => FragmentosColetados >= TotalFragmentos;

    public event Action PortalAbriu;

    void Awake()
    {
        Atual = this;
        Time.timeScale = 1f;

        // Aplica a gravidade do planeta em TODO o mundo físico 2D
        Physics2D.gravity = new Vector2(0f, -GravidadeNoJogo);
        // Raycasts não "enxergam" gatilhos (escadas, moedas) e não acertam o próprio colisor
        Physics2D.queriesHitTriggers = false;
        Physics2D.queriesStartInColliders = false;

        // Conta quantos fragmentos de estrela existem na fase
        TotalFragmentos = 0;
        foreach (var c in FindObjectsByType<Coletavel>())
            if (c.tipo == TipoColetavel.Fragmento) TotalFragmentos++;

        EstadoJogo.UltimaFase = SceneManager.GetActiveScene().name;
        EstadoJogo.MoedasNoInicioDaFase = EstadoJogo.Moedas;
        // Cada fase começa com pelo menos 3 vidas (deixa o jogo mais justo)
        if (EstadoJogo.Vidas < EstadoJogo.VidasIniciais) EstadoJogo.Vidas = EstadoJogo.VidasIniciais;
    }

    void OnDestroy()
    {
        if (Atual == this) Atual = null;
    }

    void Update()
    {
        if (Time.timeScale > 0f) EstadoJogo.TempoTotal += Time.deltaTime;

        // Atalho de TESTE: F9 pula para a próxima fase (útil para gravar o vídeo)
        var teclado = Keyboard.current;
        if (teclado != null && teclado.f9Key.wasPressedThisFrame) IrParaProximaFase();
    }

    public void RegistrarFragmento()
    {
        FragmentosColetados++;
        if (FragmentosColetados == TotalFragmentos) PortalAbriu?.Invoke();
    }

    public void IrParaProximaFase()
    {
        SceneManager.LoadScene(proximaCena);
    }
}
