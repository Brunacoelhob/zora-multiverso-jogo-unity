using UnityEngine;

// Lista de todos os efeitos sonoros do jogo
public enum Som
{
    Pulo,
    Moeda,
    Fragmento,
    Coracao,
    Dano,
    PisarInimigo,
    PortalAbriu,
    Teletransporte,
    Checkpoint,
    Selecionar,
    Vitoria
}

// Um por cena. Toca a música de fundo em loop e os efeitos sonoros.
// Qualquer script chama: GerenciadorAudio.Tocar(Som.Moeda);
public class GerenciadorAudio : MonoBehaviour
{
    public static GerenciadorAudio Instancia { get; private set; }

    [Header("Música")]
    public AudioClip musica;
    [Range(0f, 1f)] public float volumeMusica = 0.4f;
    public AudioClip jingleInicial; // opcional: toca uma vez ao abrir a cena (ex.: vitória)

    [Header("Efeitos sonoros")]
    public AudioClip pulo;
    public AudioClip moeda;
    public AudioClip fragmento;
    public AudioClip coracao;
    public AudioClip dano;
    public AudioClip pisarInimigo;
    public AudioClip portalAbriu;
    public AudioClip teletransporte;
    public AudioClip checkpoint;
    public AudioClip selecionar;
    [Range(0f, 1f)] public float volumeEfeitos = 0.8f;

    AudioSource fonteMusica;
    AudioSource fonteEfeitos;

    void Awake()
    {
        Instancia = this;

        fonteMusica = gameObject.AddComponent<AudioSource>();
        fonteMusica.loop = true;
        fonteMusica.volume = volumeMusica;
        fonteMusica.playOnAwake = false;

        fonteEfeitos = gameObject.AddComponent<AudioSource>();
        fonteEfeitos.playOnAwake = false;
    }

    void Start()
    {
        if (musica != null) fonteMusica.clip = musica;

        if (jingleInicial != null)
        {
            fonteEfeitos.PlayOneShot(jingleInicial, volumeEfeitos);
            // a música entra depois do jingle
            if (musica != null) fonteMusica.PlayDelayed(jingleInicial.length);
        }
        else if (musica != null)
        {
            fonteMusica.Play();
        }
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    AudioClip Clip(Som som) => som switch
    {
        Som.Pulo => pulo,
        Som.Moeda => moeda,
        Som.Fragmento => fragmento,
        Som.Coracao => coracao,
        Som.Dano => dano,
        Som.PisarInimigo => pisarInimigo,
        Som.PortalAbriu => portalAbriu,
        Som.Teletransporte => teletransporte,
        Som.Checkpoint => checkpoint,
        Som.Selecionar => selecionar,
        Som.Vitoria => jingleInicial,
        _ => null
    };

    // variacaoTom: muda um pouquinho o tom a cada vez, para o som não ficar repetitivo
    public static void Tocar(Som som, float volume = 1f, float variacaoTom = 0.06f)
    {
        if (Instancia == null) return;
        var clip = Instancia.Clip(som);
        if (clip == null) return;
        Instancia.fonteEfeitos.pitch = 1f + Random.Range(-variacaoTom, variacaoTom);
        Instancia.fonteEfeitos.PlayOneShot(clip, volume * Instancia.volumeEfeitos);
    }
}
