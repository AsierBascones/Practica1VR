using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GameManager : MonoBehaviour
{
    [Serializable]
    public class DatoPlato
    {
        public string nombreIdentificador;
        [Tooltip("El pedestal/altar (GameObject en la escena)")]
        public GameObject platoObjeto;
        [Tooltip("El sprite representativo del objeto que debe ir aquí")]
        public Sprite spriteObjeto;
        [Tooltip("Nombre exacto de la Interaction Layer creada para este objeto")]
        public string nombreInteractionLayer;
    }

    [Header("Paneles UI - Isla del Juego")]
    public TextMeshProUGUI textoTiempoJuego;
    public TextMeshProUGUI textoInstruccionesJuego;
    public GameObject botonStartJuego;

    [Header("Paneles UI - Lobby")]
    public TextMeshProUGUI textoTiempoLobby;
    public TextMeshProUGUI textoInstruccionesLobby;

    [Header("Iconos Minimapa / Cuadrícula")]
    [Tooltip("Iconos del panel en la isla")]
    public Image[] casillasJuego;
    [Tooltip("Iconos equivalentes del panel en el lobby (mismo orden)")]
    public Image[] casillasLobby;

    [Header("Colores de Estado")]
    public Color colorCasillaInactiva = new Color(1f, 1f, 1f, 0.1f);
    public Color colorCasillaActiva = Color.white;
    public Color colorCasillaCompletada = new Color(0.18f, 0.8f, 0.44f, 1f);

    [Header("Efectos de Sonido")]
    public AudioSource audioSource;
    public AudioClip sonidoColocarObjeto;
    public AudioClip sonidoFaseCompletada;

    [Header("Configuración de Tiempos")]
    public float tiempoFase1 = 60f;
    public float tiempoFase2 = 75f;
    public float tiempoFase3 = 90f;

    [Header("Base de Datos de Platos y Objetos")]
    [Tooltip("Lista con los 9 platos, sus sprites y sus Interaction Layers")]
    public List<DatoPlato> listaPlatos = new List<DatoPlato>();

    [Header("Eventos de Inicio de Fase")]
    public UnityEvent OnFase1Start;
    public UnityEvent OnFase2Start;
    public UnityEvent OnFase3Start;

    private List<DatoPlato> platosFaseActual = new List<DatoPlato>();
    private int faseActual = 0;
    private float tiempoRestante;
    private bool juegoActivo = false;
    private int objetosColocadosFase = 0;
    private int objetosNecesariosFase = 0;

    void Update()
    {
        if (juegoActivo)
        {
            tiempoRestante -= Time.deltaTime;
            ActualizarTextoTiempo("Tiempo: " + Mathf.CeilToInt(tiempoRestante).ToString() + "s");

            if (tiempoRestante <= 0)
            {
                TerminarJuego(false);
            }
        }
    }

    public void IniciarJuego()
    {
        SetBotonStartActivo(false);
        faseActual = 0;
        AvanzarFase();
    }

    private void AvanzarFase()
    {
        if (faseActual > 0)
        {
            LimpiarPlatosActivos();

            if (audioSource != null && sonidoFaseCompletada != null)
            {
                audioSource.PlayOneShot(sonidoFaseCompletada);
            }
        }

        faseActual++;
        objetosColocadosFase = 0;
        juegoActivo = true;

        if (faseActual == 1)
        {
            objetosNecesariosFase = 3;
            tiempoRestante = tiempoFase1;
            ActualizarTextoInstrucciones("Fase 1: Coloca los 3 objetos indicados en sus altares.");
            PrepararPlatosAleatorios(3);
            OnFase1Start?.Invoke();
        }
        else if (faseActual == 2)
        {
            objetosNecesariosFase = 6;
            tiempoRestante = tiempoFase2;
            ActualizarTextoInstrucciones("Fase 2: ¡6 objetos! Busca también en la otra isla.");
            PrepararPlatosAleatorios(6);
            OnFase2Start?.Invoke();
        }
        else if (faseActual == 3)
        {
            objetosNecesariosFase = 9;
            tiempoRestante = tiempoFase3;
            ActualizarTextoInstrucciones("Fase 3: ¡Completa los 9 pedestales requeridos!");
            PrepararPlatosAleatorios(9);
            OnFase3Start?.Invoke();
        }
        else
        {
            LimpiarPlatosActivos();
            TerminarJuego(true);
        }
    }

    private void PrepararPlatosAleatorios(int cantidad)
    {
        // 1. Apagar todos los pedestales
        foreach (var dp in listaPlatos)
        {
            if (dp.platoObjeto != null) dp.platoObjeto.SetActive(false);
        }

        ResetearCasillas();

        // 2. Barajar la lista de platos
        List<DatoPlato> baraja = new List<DatoPlato>(listaPlatos);
        for (int i = baraja.Count - 1; i > 0; i--)
        {
            int r = UnityEngine.Random.Range(0, i + 1);
            DatoPlato temp = baraja[i];
            baraja[i] = baraja[r];
            baraja[r] = temp;
        }

        // 3. Activar los N seleccionados y preparar sus sockets y UI
        platosFaseActual.Clear();
        for (int i = 0; i < cantidad && i < baraja.Count; i++)
        {
            DatoPlato seleccionado = baraja[i];
            seleccionado.platoObjeto.SetActive(true);
            platosFaseActual.Add(seleccionado);

            int indice = listaPlatos.IndexOf(seleccionado);

            // Mostrar el Sprite en ambos paneles
            AsignarSpriteCasilla(indice, seleccionado.spriteObjeto, colorCasillaActiva);

            // Configurar el socket y forzar su Interaction Layer Mask
            XRSocketInteractor socket = seleccionado.platoObjeto.GetComponentInChildren<XRSocketInteractor>();
            if (socket != null)
            {
                if (!string.IsNullOrEmpty(seleccionado.nombreInteractionLayer))
                {
                    socket.interactionLayers = InteractionLayerMask.GetMask(seleccionado.nombreInteractionLayer);
                }

                socket.selectEntered.RemoveListener(OnSocketSelectEntered);
                socket.selectExited.RemoveListener(OnSocketSelectExited);

                socket.selectEntered.AddListener(OnSocketSelectEntered);
                socket.selectExited.AddListener(OnSocketSelectExited);
            }
        }
    }

    private void LimpiarPlatosActivos()
    {
        foreach (var dp in platosFaseActual)
        {
            if (dp.platoObjeto == null) continue;

            XRSocketInteractor socket = dp.platoObjeto.GetComponentInChildren<XRSocketInteractor>();
            if (socket != null)
            {
                socket.selectEntered.RemoveListener(OnSocketSelectEntered);
                socket.selectExited.RemoveListener(OnSocketSelectExited);

                if (socket.hasSelection)
                {
                    IXRSelectInteractable interactable = socket.firstInteractableSelected;
                    if (interactable != null)
                    {
                        GameObject objAEliminar = interactable.transform.gameObject;
                        socket.interactionManager.SelectExit((IXRSelectInteractor)socket, interactable);
                        Destroy(objAEliminar);
                    }
                }
            }
        }
    }

    private void OnSocketSelectEntered(SelectEnterEventArgs args)
    {
        if (audioSource != null && sonidoColocarObjeto != null)
        {
            audioSource.PlayOneShot(sonidoColocarObjeto);
        }

        // Buscar qué plato activó el evento para marcar su casilla en verde
        for (int i = 0; i < listaPlatos.Count; i++)
        {
            if (listaPlatos[i].platoObjeto != null &&
                args.interactorObject.transform.IsChildOf(listaPlatos[i].platoObjeto.transform))
            {
                ActualizarColorCasilla(i, colorCasillaCompletada);
                break;
            }
        }

        RegistrarObjetoColocado();
    }

    private void OnSocketSelectExited(SelectExitEventArgs args)
    {
        for (int i = 0; i < listaPlatos.Count; i++)
        {
            if (listaPlatos[i].platoObjeto != null &&
                args.interactorObject.transform.IsChildOf(listaPlatos[i].platoObjeto.transform))
            {
                ActualizarColorCasilla(i, colorCasillaActiva);
                break;
            }
        }

        RegistrarObjetoRetirado();
    }

    public void RegistrarObjetoColocado()
    {
        if (!juegoActivo) return;

        objetosColocadosFase++;

        if (objetosColocadosFase >= objetosNecesariosFase)
        {
            AvanzarFase();
        }
    }

    public void RegistrarObjetoRetirado()
    {
        if (!juegoActivo) return;

        if (objetosColocadosFase > 0)
        {
            objetosColocadosFase--;
        }
    }

    private void TerminarJuego(bool victoria)
    {
        juegoActivo = false;
        if (victoria)
        {
            ActualizarTextoInstrucciones("¡Misión completada! Has superado todas las fases.");
            ActualizarTextoTiempo("¡Victoria!");
            if (audioSource != null && sonidoFaseCompletada != null)
            {
                audioSource.PlayOneShot(sonidoFaseCompletada);
            }
        }
        else
        {
            ActualizarTextoInstrucciones("¡Tiempo agotado! Pulsa Start para reintentar.");
            ActualizarTextoTiempo("Fin");
            SetBotonStartActivo(true);
            faseActual = 0;
            LimpiarPlatosActivos();
            ResetearCasillas();
        }
    }

    private void ActualizarTextoTiempo(string valor)
    {
        if (textoTiempoJuego != null) textoTiempoJuego.text = valor;
        if (textoTiempoLobby != null) textoTiempoLobby.text = valor;
    }

    private void ActualizarTextoInstrucciones(string valor)
    {
        if (textoInstruccionesJuego != null) textoInstruccionesJuego.text = valor;
        if (textoInstruccionesLobby != null) textoInstruccionesLobby.text = valor;
    }

    private void SetBotonStartActivo(bool estado)
    {
        if (botonStartJuego != null) botonStartJuego.SetActive(estado);
    }

    private void ResetearCasillas()
    {
        for (int i = 0; i < casillasJuego.Length; i++)
        {
            if (casillasJuego[i] != null)
            {
                casillasJuego[i].sprite = null;
                casillasJuego[i].color = colorCasillaInactiva;
            }
        }

        for (int i = 0; i < casillasLobby.Length; i++)
        {
            if (casillasLobby[i] != null)
            {
                casillasLobby[i].sprite = null;
                casillasLobby[i].color = colorCasillaInactiva;
            }
        }
    }

    private void AsignarSpriteCasilla(int indice, Sprite sprite, Color color)
    {
        if (casillasJuego != null && indice >= 0 && indice < casillasJuego.Length && casillasJuego[indice] != null)
        {
            casillasJuego[indice].sprite = sprite;
            casillasJuego[indice].color = color;
        }

        if (casillasLobby != null && indice >= 0 && indice < casillasLobby.Length && casillasLobby[indice] != null)
        {
            casillasLobby[indice].sprite = sprite;
            casillasLobby[indice].color = color;
        }
    }

    private void ActualizarColorCasilla(int indice, Color nuevoColor)
    {
        if (casillasJuego != null && indice >= 0 && indice < casillasJuego.Length && casillasJuego[indice] != null)
        {
            casillasJuego[indice].color = nuevoColor;
        }

        if (casillasLobby != null && indice >= 0 && indice < casillasLobby.Length && casillasLobby[indice] != null)
        {
            casillasLobby[indice].color = nuevoColor;
        }
    }
}