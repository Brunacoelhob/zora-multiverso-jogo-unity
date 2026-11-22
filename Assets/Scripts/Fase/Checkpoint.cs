using UnityEngine;

// Bandeira de checkpoint: ao passar por ela, se a Zora cair num buraco volta para cá.
[RequireComponent(typeof(SpriteRenderer))]
public class Checkpoint : MonoBehaviour
{
    public Sprite spriteDesligada;
    public Sprite[] spritesTremulando; // animação da bandeira ao vento
    public float quadrosPorSegundo = 5f;

    SpriteRenderer desenho;
    bool ativa;

    void Awake()
    {
        desenho = GetComponent<SpriteRenderer>();
        if (spriteDesligada != null) desenho.sprite = spriteDesligada;
    }

    void Update()
    {
        if (!ativa || spritesTremulando == null || spritesTremulando.Length == 0) return;
        int quadro = (int)(Time.time * quadrosPorSegundo) % spritesTremulando.Length;
        desenho.sprite = spritesTremulando[quadro];
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (ativa || !outro.TryGetComponent(out VidaJogador vida)) return;
        ativa = true;
        // Ponto de retorno: em pé ao lado da bandeira
        vida.DefinirCheckpoint(new Vector3(transform.position.x, transform.position.y + 0.55f, 0f));
        GerenciadorAudio.Tocar(Som.Checkpoint);
        Efeitos.Explodir(transform.position + Vector3.up * 0.3f, new Color(1f, 0.9f, 0.3f), 12, 4f);
        HUD.Mensagem("Checkpoint salvo!");
    }
}
