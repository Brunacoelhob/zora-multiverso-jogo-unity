using UnityEngine;

// Marca uma área de escada (um gatilho / trigger). Não faz nada sozinha:
// o MovimentoJogador percebe quando a Zora entra nela e permite escalar.
[RequireComponent(typeof(BoxCollider2D))]
public class Escada : MonoBehaviour
{
    [Tooltip("Altura da base da escada (pés da Zora)")]
    public float baseY;
    [Tooltip("Altura máxima dos pés ao subir — um pouco acima da borda onde a escada termina")]
    public float topoY;

    void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }
}
