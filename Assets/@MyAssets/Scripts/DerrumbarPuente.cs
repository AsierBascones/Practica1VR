using UnityEngine;
using TMPro;

public class DerrumbarPuente : MonoBehaviour
{
    [Header("UI a Ocultar")]
    public GameObject panelInstruccion;
    public TextMeshProUGUI textoDirecto;

    [Header("Gestión de Puentes")]
    public GameObject puenteFantasma;
    public GameObject puenteReal;

    [Header("Audio (Opcional)")]
    public AudioSource audioSource;
    public AudioClip sonidoCaidaPuente;

    private bool yaDerribado = false;

    void Start()
    {
        if (puenteFantasma != null) puenteFantasma.SetActive(true);
        if (puenteReal != null) puenteReal.SetActive(false);
        if (panelInstruccion != null) panelInstruccion.SetActive(true);
        if (textoDirecto != null) textoDirecto.gameObject.SetActive(true);
    }

    public void ActivarPuenteReal()
    {
        if (yaDerribado) return;
        yaDerribado = true;

        if (panelInstruccion != null)
            panelInstruccion.SetActive(false);

        if (textoDirecto != null)
        {
            textoDirecto.enabled = false;
            textoDirecto.gameObject.SetActive(false);
        }

        if (puenteFantasma != null)
            puenteFantasma.SetActive(false);

        if (puenteReal != null)
            puenteReal.SetActive(true);

        if (audioSource != null && sonidoCaidaPuente != null)
            audioSource.PlayOneShot(sonidoCaidaPuente);
    }
}