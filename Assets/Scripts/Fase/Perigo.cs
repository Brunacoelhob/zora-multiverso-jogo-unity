using UnityEngine;

// Qualquer coisa que machuca ao encostar (espinhos).
public class Perigo : MonoBehaviour
{
    void OnTriggerStay2D(Collider2D outro)
    {
        if (outro.TryGetComponent(out VidaJogador vida))
            vida.TomarDano(transform.position + Vector3.down); // empurra para cima/para trás
    }
}
