using UnityEngine;

// Entrega para a classe Efeitos (que é static) o sprite e o material das partículas.
public class RecursosEfeitos : MonoBehaviour
{
    public Sprite spriteParticula;
    public Material materialParticula;

    void Awake()
    {
        Efeitos.SpriteParticula = spriteParticula;
        Efeitos.MaterialParticula = materialParticula;
    }
}
