using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Interface durante a fase: vidas (corações), moedas, fragmentos de estrela, planeta e gravidade,
// faixa de apresentação do planeta, mensagens rápidas e menu de pausa (Esc).
public class HUD : MonoBehaviour
{
    static HUD instancia;
    static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

    [Header("Vidas")]
    public Image[] coracoes;
    public Sprite coracaoCheio;
    public Sprite coracaoVazio;

    [Header("Textos")]
    public Text textoMoedas;
    public Text textoFragmentos;
    public Text textoPlaneta;
    public Text textoMensagem;

    [Header("Apresentação do planeta")]
    public CanvasGroup faixa;
    public Text faixaTitulo;
    public Text faixaCuriosidade;
    public Text faixaDica;
    public float tempoFaixa = 5f;

    [Header("Pausa")]
    public GameObject painelPausa;

    float fimMensagem;
    float inicioFase;

    void Awake()
    {
        instancia = this;
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    void Start()
    {
        inicioFase = Time.time;
        if (painelPausa != null) painelPausa.SetActive(false);
        if (textoMensagem != null) textoMensagem.text = "";

        var p = ConfiguracaoPlaneta.Atual;
        if (p == null) return;
        textoPlaneta.text = $"Fase {p.numeroFase}: {p.nomePlaneta.ToUpper()}   •   g = {p.gravidadeReal.ToString("0.00", PtBr)} m/s²";
        faixaTitulo.text = p.nomePlaneta.ToUpper();
        faixaCuriosidade.text = p.curiosidade;
        faixaDica.text = p.dica;
    }

    void Update()
    {
        AtualizarContadores();
        AtualizarFaixa();
        AtualizarMensagem();
        VerificarPausa();
    }

    void AtualizarContadores()
    {
        int vidas = EstadoJogo.Vidas;
        int espacos = Mathf.Max(EstadoJogo.VidasIniciais, vidas);
        for (int i = 0; i < coracoes.Length; i++)
        {
            coracoes[i].enabled = i < espacos;
            coracoes[i].sprite = i < vidas ? coracaoCheio : coracaoVazio;
        }

        textoMoedas.text = "x " + EstadoJogo.Moedas;
        var p = ConfiguracaoPlaneta.Atual;
        if (p != null) textoFragmentos.text = $"{p.FragmentosColetados}/{p.TotalFragmentos}";
    }

    void AtualizarFaixa()
    {
        if (faixa == null) return;
        float t = Time.time - inicioFase;
        // aparece rápido, fica parada, some devagar
        float alfa = t < 0.4f ? t / 0.4f : t < tempoFaixa ? 1f : 1f - (t - tempoFaixa) / 0.8f;
        faixa.alpha = Mathf.Clamp01(alfa);
    }

    void AtualizarMensagem()
    {
        if (textoMensagem == null) return;
        float resto = fimMensagem - Time.unscaledTime;
        var c = textoMensagem.color;
        textoMensagem.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(resto / 0.5f));
    }

    void VerificarPausa()
    {
        var teclado = Keyboard.current;
        if (teclado == null || painelPausa == null) return;

        if (teclado.escapeKey.wasPressedThisFrame || teclado.pKey.wasPressedThisFrame)
        {
            bool pausar = !painelPausa.activeSelf;
            painelPausa.SetActive(pausar);
            Time.timeScale = pausar ? 0f : 1f;
            GerenciadorAudio.Tocar(Som.Selecionar, 0.6f, 0f);
        }

        if (painelPausa.activeSelf && teclado.mKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(Cenas.Menu);
        }
    }

    // Mostra um aviso rápido no rodapé da tela (ex.: "Checkpoint salvo!")
    public static void Mensagem(string texto, float duracao = 2.5f)
    {
        if (instancia == null || instancia.textoMensagem == null) return;
        instancia.textoMensagem.text = texto;
        instancia.fimMensagem = Time.unscaledTime + duracao;
    }
}
