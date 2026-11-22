using UnityEngine;

// Escolhe qual animação da Zora tocar, olhando o estado do movimento.
// Ele só muda o parâmetro "Estado" do Animator; as transições ficam no Animator Controller
// (Assets/Gerado/Animacoes/Zora.controller), que dá para abrir na janela Animator da Unity.
[RequireComponent(typeof(Animator))]
public class AnimacaoJogador : MonoBehaviour
{
    public enum EstadoAnimacao
    {
        Parada = 0,
        Andando = 1,
        Correndo = 2,
        Pulando = 3,
        Escalando = 4,
        EscadaParada = 5,
        Dano = 6
    }

    static readonly int ParametroEstado = Animator.StringToHash("Estado");

    Animator animador;
    MovimentoJogador movimento;
    VidaJogador vida;

    void Awake()
    {
        animador = GetComponent<Animator>();
        movimento = GetComponent<MovimentoJogador>();
        vida = GetComponent<VidaJogador>();
    }

    void Update()
    {
        animador.SetInteger(ParametroEstado, (int)EscolherEstado());
    }

    EstadoAnimacao EscolherEstado()
    {
        if (vida != null && vida.MostrandoDano) return EstadoAnimacao.Dano;

        var v = movimento.Velocidade;
        if (movimento.Escalando)
            return v.sqrMagnitude > 0.05f ? EstadoAnimacao.Escalando : EstadoAnimacao.EscadaParada;

        if (!movimento.NoChao) return EstadoAnimacao.Pulando;

        float rapidez = Mathf.Abs(v.x);
        if (rapidez > movimento.velocidadeAndar + 0.5f) return EstadoAnimacao.Correndo;
        if (rapidez > 0.3f) return EstadoAnimacao.Andando;
        return EstadoAnimacao.Parada;
    }
}
