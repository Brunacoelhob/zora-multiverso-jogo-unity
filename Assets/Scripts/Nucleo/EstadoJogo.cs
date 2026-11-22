using UnityEngine;

// Guarda o que precisa sobreviver à troca de cenas (vidas, moedas, tempo).
// É "static": existe uma cópia só, acessível de qualquer script, sem precisar de objeto na cena.
public static class EstadoJogo
{
    public const int VidasIniciais = 3;
    public const int VidasMaximas = 5;

    public static int Vidas = VidasIniciais;
    public static int Moedas;
    public static int InimigosDerrotados;
    public static float TempoTotal;

    // Para o botão "Tentar de novo" da tela de Game Over
    public static string UltimaFase = Cenas.Fase1;
    public static int MoedasNoInicioDaFase;

    // O projeto usa "Enter Play Mode" sem recarregar o domínio, então variáveis static
    // NÃO zeram sozinhas entre um Play e outro no editor. Este método zera tudo no início.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ZerarAoIniciar() => NovoJogo();

    public static void NovoJogo()
    {
        Vidas = VidasIniciais;
        Moedas = 0;
        InimigosDerrotados = 0;
        TempoTotal = 0f;
        UltimaFase = Cenas.Fase1;
        MoedasNoInicioDaFase = 0;
    }

    // Recomeça a fase em que a jogadora perdeu, devolvendo as moedas ao valor de quando ela entrou
    public static void PrepararNovaTentativa()
    {
        Vidas = VidasIniciais;
        Moedas = MoedasNoInicioDaFase;
    }
}

// Nomes das cenas num lugar só (evita erro de digitação espalhado pelo código)
public static class Cenas
{
    public const string Menu = "Menu";
    public const string Fase1 = "Fase1_Lua";
    public const string Fase2 = "Fase2_Marte";
    public const string Fase3 = "Fase3_Terra";
    public const string Fase4 = "Fase4_Jupiter";
    public const string Vitoria = "Vitoria";
    public const string GameOver = "GameOver";
}
