using System.Collections;
using UnityEngine;

public class ActivarIslaFuego : MonoBehaviour
{
    [Header("Efectos del Fuego")]
    [Tooltip("El GameObject hijo con el ParticleSystem o luces del fuego")]
    public GameObject fuegoEfecto;

    [Header("Muro del Amarradero")]
    [Tooltip("El muro que bloquea el paso hacia el muelle/amarradero")]
    public GameObject muroAmarradero;

    [Header("Configuración de la Isla")]
    [Tooltip("La raíz del GameObject de la isla que emergerá")]
    public Transform islaEmergente;
    public float alturaOcultaY = -15f;
    public float alturaVisibleY = 0f;
    public float velocidadEmerger = 3f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sonidoEncender;
    public AudioClip sonidoTerremoto;

    private bool encendido = false;

    void Start()
    {
        // Estado inicial
        if (fuegoEfecto != null) fuegoEfecto.SetActive(false);
        if (muroAmarradero != null) muroAmarradero.SetActive(true);

        if (islaEmergente != null)
        {
            Vector3 pos = islaEmergente.position;
            pos.y = alturaOcultaY;
            islaEmergente.position = pos;
        }
    }

    /// Vincular al evento 'Activated' o 'First Select Entered' del XR Simple Interactable
    public void EncenderFuego()
    {
        if (encendido) return;
        encendido = true;

        // 1. Encender partículas/luz del fuego
        if (fuegoEfecto != null)
            fuegoEfecto.SetActive(true);

        // 2. Desaparecer el muro para dejar paso libre al amarradero
        if (muroAmarradero != null)
            muroAmarradero.SetActive(false);

        // 3. Efectos sonoros
        if (audioSource != null)
        {
            if (sonidoEncender != null) audioSource.PlayOneShot(sonidoEncender);
            if (sonidoTerremoto != null) audioSource.PlayOneShot(sonidoTerremoto);
        }

        // 4. Emerger la isla desde el agua
        if (islaEmergente != null)
        {
            StartCoroutine(EmergerIsla());
        }
    }

    private IEnumerator EmergerIsla()
    {
        Vector3 inicio = islaEmergente.position;
        Vector3 fin = new Vector3(inicio.x, alturaVisibleY, inicio.z);

        while (Vector3.Distance(islaEmergente.position, fin) > 0.05f)
        {
            islaEmergente.position = Vector3.MoveTowards(islaEmergente.position, fin, velocidadEmerger * Time.deltaTime);
            yield return null;
        }

        islaEmergente.position = fin;
    }
}