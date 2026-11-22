using UnityEngine;

// Câmera que segue a Zora com suavidade, olha um pouco à frente de onde ela anda,
// não sai dos limites da fase e treme quando ela leva dano.
[RequireComponent(typeof(Camera))]
public class CameraSegue : MonoBehaviour
{
    public Transform alvo;
    public float suavidade = 0.18f;
    public float olharAFrente = 2f;
    public float alturaExtra = 1f;

    [Header("Limites da fase (em unidades do mundo)")]
    public Vector2 limiteMinimo;
    public Vector2 limiteMaximo;

    static CameraSegue instancia;
    Camera cam;
    Vector3 velocidadeSuave;
    float frente;
    float fimTremor;
    float forcaTremor;

    void Awake()
    {
        instancia = this;
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (alvo != null) transform.position = Limitar(alvo.position);
    }

    void LateUpdate()
    {
        if (alvo == null) return;

        // Olha para o lado em que a Zora está indo
        var rb = alvo.GetComponent<Rigidbody2D>();
        if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.5f)
            frente = Mathf.Lerp(frente, Mathf.Sign(rb.linearVelocity.x) * olharAFrente, Time.deltaTime * 2f);

        Vector3 desejada = Limitar(alvo.position + new Vector3(frente, alturaExtra, 0f));
        Vector3 pos = Vector3.SmoothDamp(transform.position, desejada, ref velocidadeSuave, suavidade);

        if (Time.time < fimTremor)
            pos += (Vector3)(Random.insideUnitCircle * forcaTremor);

        transform.position = pos;
    }

    Vector3 Limitar(Vector3 p)
    {
        float meiaAltura = cam.orthographicSize;
        float meiaLargura = meiaAltura * cam.aspect;
        float x = Mathf.Clamp(p.x, limiteMinimo.x + meiaLargura, Mathf.Max(limiteMinimo.x + meiaLargura, limiteMaximo.x - meiaLargura));
        float y = Mathf.Clamp(p.y, limiteMinimo.y + meiaAltura, Mathf.Max(limiteMinimo.y + meiaAltura, limiteMaximo.y - meiaAltura));
        return new Vector3(x, y, -10f);
    }

    public static void Tremer(float duracao, float forca)
    {
        if (instancia == null) return;
        instancia.fimTremor = Time.time + duracao;
        instancia.forcaTremor = forca;
    }
}
