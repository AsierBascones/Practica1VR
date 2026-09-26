using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("Interfaz UI")]
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoInstrucciones;
    public GameObject botonStart; // El botón físico o de UI del Lobby

    [Header("Configuración de Fases")]
    public float tiempoPorFase = 60f;

    // UnityEvents para activar/desactivar los sockets de cada fase desde el Inspector
    public UnityEvent OnFase1Start;
    public UnityEvent OnFase2Start;
    public UnityEvent OnFase3Start;

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
            textoTiempo.text = "Tiempo: " + Mathf.CeilToInt(tiempoRestante).ToString() + "s";

            if (tiempoRestante <= 0)
            {
                TerminarJuego(false);
            }
        }
    }

    // Función que llamará el botón del Lobby
    public void IniciarJuego()
    {
        botonStart.SetActive(false);
        AvanzarFase();
    }

    private void AvanzarFase()
    {
        faseActual++;
        objetosColocadosFase = 0;
        tiempoRestante = tiempoPorFase;
        juegoActivo = true;

        if (faseActual == 1)
        {
            objetosNecesariosFase = 3;
            textoInstrucciones.text = "Fase 1: Coloca 3 objetos en la primera fila.";
            OnFase1Start.Invoke(); // Enciende los sockets de la fase 1
        }
        else if (faseActual == 2)
        {
            objetosNecesariosFase = 6; // Acumulativo o nuevos
            textoInstrucciones.text = "Fase 2: Completa 6 objetos. ¡Busca en la otra isla!";
            OnFase2Start.Invoke();
        }
        else if (faseActual == 3)
        {
            objetosNecesariosFase = 9;
            textoInstrucciones.text = "Fase 3: ¡Completa el panel entero!";
            OnFase3Start.Invoke();
        }
        else
        {
            TerminarJuego(true);
        }
    }

    // Los Sockets llamarán a esta función cuando reciban un objeto correcto
    public void RegistrarObjetoColocado()
    {
        if (!juegoActivo) return;

        objetosColocadosFase++;

        if (objetosColocadosFase >= objetosNecesariosFase)
        {
            AvanzarFase();
        }
    }

    private void TerminarJuego(bool victoria)
    {
        juegoActivo = false;
        if (victoria)
        {
            textoInstrucciones.text = "¡Misión completada!";
            textoTiempo.text = "¡Victoria!";
        }
        else
        {
            textoInstrucciones.text = "El tiempo se ha agotado...";
            textoTiempo.text = "Fin";
            botonStart.SetActive(true); // Permite reintentar
            faseActual = 0;
        }
    }

    // Llamar cuando el objeto sale del socket
    public void RegistrarObjetoRetirado()
    {
        if (!juegoActivo) return;

        if (objetosColocadosFase > 0)
        {
            objetosColocadosFase--;
        }
    }
}