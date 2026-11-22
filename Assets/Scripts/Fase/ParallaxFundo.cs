using UnityEngine;

// Fundo espacial que acompanha a câmera, mas se mexe um pouco mais devagar que ela.
// Esse efeito (parallax) dá sensação de profundidade: o que está longe parece andar menos.
public class ParallaxFundo : MonoBehaviour
{
    [Range(0f, 1f)] public float fatorHorizontal = 0.06f;
    [Range(0f, 1f)] public float fatorVertical = 0.04f;

    Transform cam;
    Vector3 camInicial;

    void Start()
    {
        cam = Camera.main.transform;
        camInicial = cam.position;
    }

    void LateUpdate()
    {
        Vector3 deslocamento = cam.position - camInicial;
        transform.position = new Vector3(
            cam.position.x - deslocamento.x * fatorHorizontal,
            cam.position.y - deslocamento.y * fatorVertical,
            10f);
    }
}
