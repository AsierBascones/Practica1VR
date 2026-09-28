using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GameManager : MonoBehaviour
{
    public enum CategoriaFase
    {
        Fase1_Base,
        Fase2_Especial,
        Fase3_Isla
    }

    [Serializable]
    public class InfoObjeto
    {
        public string nombreIdentificador;
        [Tooltip("Sprite que se mostrará en los paneles")]
        public Sprite spriteObjeto;
        [Tooltip("Nombre exacto de la Interaction Layer de este objeto")]
        public string nombreInteractionLayer;
        [Tooltip("Fase en la que se introduce este objeto")]
        public CategoriaFase categoria = CategoriaFase.Fase1_Base;
    }

    private class PlatoActivo
    {
        public GameObject pedestal;
        public int indiceUI;
    }

    [Header("Paneles UI - Isla del Juego")]
    public TextMeshProUGUI textoTiempoJuego;
    public TextMeshProUGUI textoInstruccionesJuego;
    public GameObject botonStartJuego;

    [Header("Paneles UI - Lobby")]
    public TextMeshProUGUI textoTiempoLobby;
    public TextMeshProUGUI textoInstruccionesLobby;

    [Header("Iconos Minimapa / Cuadrícula")]
    public Image[] casillasJuego;
    public Image[] casillasLobby;

    [Header("Efectos de Sonido")]
    public AudioSource audioSource;
    public AudioClip sonidoColocarObjeto;
    public AudioClip sonidoFaseCompletada;

    [Header("Configuración de Tiempos")]
    public float tiempoFase1 = 60f;
    public float tiempoFase2 = 75f;
    public float tiempoFase3 = 90f;

    [Header("Infraestructura Física")]
    [Tooltip("Arrastra aquí los 9 pedestales/altares en el orden de la cuadrícula UI")]
    public List<GameObject> pedestalesFisicos = new List<GameObject>();

    [Header("Base de Datos de Objetos")]
    [Tooltip("Registra aquí todos los objetos con sus categorías")]
    public List<InfoObjeto> baseDatosObjetos = new List<InfoObjeto>();

    [Header("Eventos de Inicio de Fase")]
    public UnityEvent OnFase1Start;
    public UnityEvent OnFase2Start;
    public UnityEvent OnFase3Start;

    private List<PlatoActivo> platosFaseActual = new List<PlatoActivo>();
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
                audioSource.PlayOneShot(sonidoFaseCompletada);
        }

        faseActual++;
        objetosColocadosFase = 0;
        juegoActivo = true;

        if (faseActual == 1)
        {
            objetosNecesariosFase = 3;
            tiempoRestante = tiempoFase1;
            ActualizarTextoInstrucciones("Fase 1: Coloca los 3 objetos iniciales en sus pedestales correspondientes.");
            PrepararPlatosAleatorios(3);
            OnFase1Start?.Invoke();
        }
        else if (faseActual == 2)
        {
            objetosNecesariosFase = 6;
            tiempoRestante = tiempoFase2;
            ActualizarTextoInstrucciones("Fase 2: Coloca 6 objetos.\n<color=#FFA500>Pista:</color> Dispara a la hoguera si quieres revelar secretos ocultos.");
            PrepararPlatosAleatorios(6);
            OnFase2Start?.Invoke();
        }
        else if (faseActual == 3)
        {
            objetosNecesariosFase = 9;
            tiempoRestante = tiempoFase3;
            ActualizarTextoInstrucciones("Fase 3: ¡Último desafío! Enciende la hoguera para hacer emerger la isla del mar y usa el Teleport para recoger los objetos.");
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
        // 1. Ocultar todos los pedestales y resetear casillas UI
        foreach (var p in pedestalesFisicos)
        {
            if (p != null) p.SetActive(false);
        }
        ResetearCasillas();

        // 2. Segmentar la base de datos por categorías
        List<InfoObjeto> listaBase = baseDatosObjetos.Where(o => o.categoria == CategoriaFase.Fase1_Base).ToList();
        List<InfoObjeto> listaFase2 = baseDatosObjetos.Where(o => o.categoria == CategoriaFase.Fase2_Especial).ToList();
        List<InfoObjeto> listaFase3 = baseDatosObjetos.Where(o => o.categoria == CategoriaFase.Fase3_Isla).ToList();

        List<InfoObjeto> objetosSeleccionados = new List<InfoObjeto>();

        // 3. Reglas de selección según la fase
        if (faseActual == 1)
        {
            objetosSeleccionados.AddRange(ObtenerAleatorios(listaBase, 3));
        }
        else if (faseActual == 2)
        {
            InfoObjeto especialF2 = ObtenerAleatorios(listaFase2, 1)[0];
            objetosSeleccionados.Add(especialF2);

            List<InfoObjeto> poolRestante = new List<InfoObjeto>(listaBase);
            poolRestante.AddRange(listaFase2.Where(o => o != especialF2));
            objetosSeleccionados.AddRange(ObtenerAleatorios(poolRestante, 5));
        }
        else if (faseActual == 3)
        {
            InfoObjeto especialF3 = ObtenerAleatorios(listaFase3, 1)[0];
            InfoObjeto especialF2 = ObtenerAleatorios(listaFase2, 1)[0];
            objetosSeleccionados.Add(especialF3);
            objetosSeleccionados.Add(especialF2);

            List<InfoObjeto> poolRestante = new List<InfoObjeto>(listaBase);
            poolRestante.AddRange(listaFase2.Where(o => o != especialF2));
            poolRestante.AddRange(listaFase3.Where(o => o != especialF3));
            objetosSeleccionados.AddRange(ObtenerAleatorios(poolRestante, 7));
        }

        // Barajar objetos elegidos para distribuir posiciones aleatorias
        objetosSeleccionados = ObtenerAleatorios(objetosSeleccionados, objetosSeleccionados.Count);

        // 4. Elegir qué índices de pedestales se activan (0 al 8)
        List<int> indicesDisponibles = Enumerable.Range(0, pedestalesFisicos.Count).ToList();
        List<int> indicesElegidos = ObtenerAleatorios(indicesDisponibles, cantidad);

        platosFaseActual.Clear();

        // 5. Vincular pedestal físico con su casilla idéntica en la UI
        for (int i = 0; i < cantidad; i++)
        {
            int index = indicesElegidos[i];
            GameObject pedestal = pedestalesFisicos[index];
            InfoObjeto item = objetosSeleccionados[i];

            pedestal.SetActive(true);

            PlatoActivo platoAct = new PlatoActivo
            {
                pedestal = pedestal,
                indiceUI = index
            };
            platosFaseActual.Add(platoAct);

            // Muestra el sprite en la posición exacta del pedestal
            MostrarSpriteCasilla(index, item.spriteObjeto);

            XRSocketInteractor socket = pedestal.GetComponentInChildren<XRSocketInteractor>();
            if (socket != null)
            {
                socket.interactionLayers = InteractionLayerMask.GetMask(item.nombreInteractionLayer);

                socket.selectEntered.RemoveListener(OnSocketSelectEntered);
                socket.selectExited.RemoveListener(OnSocketSelectExited);

                socket.selectEntered.AddListener(OnSocketSelectEntered);
                socket.selectExited.AddListener(OnSocketSelectExited);
            }
        }
    }

    private List<T> ObtenerAleatorios<T>(List<T> listaOriginal, int cantidad)
    {
        List<T> temporal = new List<T>(listaOriginal);
        for (int i = temporal.Count - 1; i > 0; i--)
        {
            int r = UnityEngine.Random.Range(0, i + 1);
            T temp = temporal[i];
            temporal[i] = temporal[r];
            temporal[r] = temp;
        }
        return temporal.Take(cantidad).ToList();
    }

    private void LimpiarPlatosActivos()
    {
        foreach (var plato in platosFaseActual)
        {
            if (plato.pedestal == null) continue;

            XRSocketInteractor socket = plato.pedestal.GetComponentInChildren<XRSocketInteractor>();
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

                        // 1. Desconectar la selección formalmente en XRI
                        socket.interactionManager.SelectExit((IXRSelectInteractor)socket, interactable);

                        // 2. Destruir en el siguiente fotograma para que XRI termine su ciclo interno
                        StartCoroutine(DestruirEnSiguienteFrame(objAEliminar));
                    }
                }
            }
        }
    }

    private System.Collections.IEnumerator DestruirEnSiguienteFrame(GameObject obj)
    {
        if (obj != null)
        {
            // Ocultamos el objeto inmediatamente para que no se vea en escena
            obj.SetActive(false);

            // Esperamos a que termine el frame actual y se resuelvan las jerarquías de XRI
            yield return new WaitForEndOfFrame();

            if (obj != null)
            {
                obj.transform.SetParent(null);
                Destroy(obj);
            }
        }
    }

    private void OnSocketSelectEntered(SelectEnterEventArgs args)
    {
        if (audioSource != null && sonidoColocarObjeto != null)
            audioSource.PlayOneShot(sonidoColocarObjeto);

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
        if (objetosColocadosFase >= objetosNecesariosFase) AvanzarFase();
    }

    public void RegistrarObjetoRetirado()
    {
        if (!juegoActivo) return;
        if (objetosColocadosFase > 0) objetosColocadosFase--;
    }

    private void TerminarJuego(bool victoria)
    {
        juegoActivo = false;
        if (victoria)
        {
            ActualizarTextoInstrucciones("¡Misión completada!");
            ActualizarTextoTiempo("¡Victoria!");
            if (audioSource != null && sonidoFaseCompletada != null)
                audioSource.PlayOneShot(sonidoFaseCompletada);
        }
        else
        {
            ActualizarTextoInstrucciones("¡Tiempo agotado! Pulsa Start.");
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
        foreach (var c in casillasJuego)
        {
            if (c != null)
            {
                c.sprite = null;
                c.color = new Color(1f, 1f, 1f, 0.15f);
            }
        }

        foreach (var c in casillasLobby)
        {
            if (c != null)
            {
                c.sprite = null;
                c.color = new Color(1f, 1f, 1f, 0.15f);
            }
        }
    }

    private void MostrarSpriteCasilla(int indice, Sprite sprite)
    {
        if (casillasJuego != null && indice < casillasJuego.Length && casillasJuego[indice] != null)
        {
            casillasJuego[indice].sprite = sprite;
            casillasJuego[indice].color = Color.white;
        }

        if (casillasLobby != null && indice < casillasLobby.Length && casillasLobby[indice] != null)
        {
            casillasLobby[indice].sprite = sprite;
            casillasLobby[indice].color = Color.white;
        }
    }
}