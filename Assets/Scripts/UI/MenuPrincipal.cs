using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tela inicial: Jogar, Como jogar, Créditos e Sair.
public class MenuPrincipal : MonoBehaviour
{
    public Button botaoJogar;
    public Button botaoComoJogar;
    public Button botaoCreditos;
    public Button botaoSair;

    public GameObject painelComoJogar;
    public GameObject painelCreditos;

    void Start()
    {
        Time.timeScale = 1f;
        botaoJogar.onClick.AddListener(Jogar);
        botaoComoJogar.onClick.AddListener(() => AlternarPainel(painelComoJogar));
        botaoCreditos.onClick.AddListener(() => AlternarPainel(painelCreditos));
        botaoSair.onClick.AddListener(Sair);

        painelComoJogar.SetActive(false);
        painelCreditos.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(botaoJogar.gameObject);
    }

    void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null) return;
        // Esc fecha o painel aberto
        if (teclado.escapeKey.wasPressedThisFrame)
        {
            painelComoJogar.SetActive(false);
            painelCreditos.SetActive(false);
        }
    }

    void AlternarPainel(GameObject painel)
    {
        GerenciadorAudio.Tocar(Som.Selecionar, 1f, 0f);
        bool abrir = !painel.activeSelf;
        painelComoJogar.SetActive(false);
        painelCreditos.SetActive(false);
        painel.SetActive(abrir);
    }

    void Jogar()
    {
        GerenciadorAudio.Tocar(Som.Selecionar, 1f, 0f);
        EstadoJogo.NovoJogo();
        SceneManager.LoadScene(Cenas.Fase1);
    }

    void Sair()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
