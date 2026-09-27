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
    public Color colorCasillaInactiva = new Color(1f, 1f, 1f, 0.15f);
    public Color colorCasillaActiva = new Color(0.18f, 0.8f, 0.44f, 1f);

    [Header("Efectos de Sonido")]
    public AudioSource audioSource;
    public AudioClip sonidoColocarObjeto;
    public AudioClip sonidoFaseCompletada;

    [Header("Configuración de Tiempos")]
    public float tiempoFase1 = 60f;
    public float tiempoFase2 = 75f;
    public float tiempoFase3 = 90f;

    [Header("Pool Total de Altares / Platos")]
    public List<GameObject> todosLosPlatos;

    [Header("Eventos de Inicio de Fase")]
    public UnityEvent OnFase1Start;
    public UnityEvent OnFase2Start;
    public UnityEvent OnFase3Start;

    private List<GameObject> platosFaseActual = new List<GameObject>();
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
            ActualizarTextoInstrucciones("Fase 1: Coloca 3 objetos en los altares activos marcados.");
            PrepararPlatosAleatorios(3);
            OnFase1Start?.Invoke();
        }
        else if (faseActual == 2)
        {
            objetosNecesariosFase = 6;
            tiempoRestante = tiempoFase2;
            ActualizarTextoInstrucciones("Fase 2: ¡6 objetos! Usa el teletransporte a la otra isla.");
            PrepararPlatosAleatorios(6);
            OnFase2Start?.Invoke();
        }
        else if (faseActual == 3)
        {
            objetosNecesariosFase = 9;
            tiempoRestante = tiempoFase3;
            ActualizarTextoInstrucciones("Fase 3: ¡Completa los 9 pedestales al completo!");
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
        foreach (var plato in todosLosPlatos)
        {
            if (plato != null) plato.SetActive(false);
        }

        ResetearCasillas();

        List<GameObject> baraja = new List<GameObject>(todosLosPlatos);
        for (int i = baraja.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            GameObject temporal = baraja[i];
            baraja[i] = baraja[r];
            baraja[r] = temporal;
        }

        platosFaseActual.Clear();
        for (int i = 0; i < cantidad && i < baraja.Count; i++)
        {
            GameObject platoSeleccionado = baraja[i];
            platoSeleccionado.SetActive(true);
            platosFaseActual.Add(platoSeleccionado);

            int indicePlato = todosLosPlatos.IndexOf(platoSeleccionado);
            ActualizarColorCasilla(indicePlato, colorCasillaActiva);

            XRSocketInteractor socket = platoSeleccionado.GetComponentInChildren<XRSocketInteractor>();
            if (socket != null)
            {
                socket.selectEntered.RemoveListener(OnSocketSelectEntered);
                socket.selectExited.RemoveListener(OnSocketSelectExited);

                socket.selectEntered.AddListener(OnSocketSelectEntered);
                socket.selectExited.AddListener(OnSocketSelectExited);
            }
        }
    }

    private void LimpiarPlatosActivos()
    {
        foreach (var plato in platosFaseActual)
        {
            if (plato == null) continue;

            XRSocketInteractor socket = plato.GetComponentInChildren<XRSocketInteractor>();
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

        RegistrarObjetoColocado();
    }

    private void OnSocketSelectExited(SelectExitEventArgs args)
    {
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

    // --- Métodos de sincronización dual ---

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
        if (casillasJuego != null)
        {
            foreach (var c in casillasJuego)
            {
                if (c != null) c.color = colorCasillaInactiva;
            }
        }

        if (casillasLobby != null)
        {
            foreach (var c in casillasLobby)
            {
                if (c != null) c.color = colorCasillaInactiva;
            }
        }
    }

    private void ActualizarColorCasilla(int indice, Color nuevoColor)
    {
        if (casillasJuego != null && indice >= 0 && indice < casillasJuego.Length)
        {
            if (casillasJuego[indice] != null) casillasJuego[indice].color = nuevoColor;
        }

        if (casillasLobby != null && indice >= 0 && indice < casillasLobby.Length)
        {
            if (casillasLobby[indice] != null) casillasLobby[indice].color = nuevoColor;
        }
    }
}