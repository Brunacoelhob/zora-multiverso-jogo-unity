using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Usada nas telas de Vitória e de Game Over.
public class TelaFinal : MonoBehaviour
{
    public bool vitoria;
    public Text textoResumo;
    public Button botaoPrincipal; // Vitória: "Jogar de novo" | Game Over: "Tentar de novo"
    public Button botaoMenu;

    void Start()
    {
        Time.timeScale = 1f;

        int minutos = (int)(EstadoJogo.TempoTotal / 60f);
        int segundos = (int)(EstadoJogo.TempoTotal % 60f);
        textoResumo.text = vitoria
            ? $"Moedas: {EstadoJogo.Moedas}   •   Inimigos derrotados: {EstadoJogo.InimigosDerrotados}   •   Tempo: {minutos:00}:{segundos:00}"
            : $"Você chegou até: {NomeDaFase(EstadoJogo.UltimaFase)}";

        botaoPrincipal.onClick.AddListener(vitoria ? JogarDeNovo : TentarDeNovo);
        botaoMenu.onClick.AddListener(IrParaMenu);
        EventSystem.current?.SetSelectedGameObject(botaoPrincipal.gameObject);
    }

    static string NomeDaFase(string cena) => cena switch
    {
        Cenas.Fase1 => "Fase 1 - Lua",
        Cenas.Fase2 => "Fase 2 - Marte",
        Cenas.Fase3 => "Fase 3 - Terra",
        Cenas.Fase4 => "Fase 4 - Júpiter",
        _ => cena
    };

    void JogarDeNovo()
    {
        GerenciadorAudio.Tocar(Som.Selecionar, 1f, 0f);
        EstadoJogo.NovoJogo();
        SceneManager.LoadScene(Cenas.Fase1);
    }

    void TentarDeNovo()
    {
        GerenciadorAudio.Tocar(Som.Selecionar, 1f, 0f);
        EstadoJogo.PrepararNovaTentativa();
        SceneManager.LoadScene(EstadoJogo.UltimaFase);
    }

    void IrParaMenu()
    {
        GerenciadorAudio.Tocar(Som.Selecionar, 1f, 0f);
        SceneManager.LoadScene(Cenas.Menu);
    }
}
