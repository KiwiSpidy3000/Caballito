using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ConexionServidor : MonoBehaviour
{
    [Header("Configuración")]
    public string urlServidor = "http://127.0.0.1:5000/tick";
    public float tiempoDeActualizacion = 0.2f; // 5 veces por segundo, igual que en tu web

    [Header("Caballos en Unity")]
    // Aquí arrastraremos a tu modelo de caballo desde la jerarquía de Unity
    public GameObject caballoRelampago; 

    private Animator animRelampago;

    void Start()
    {
        // Obtenemos el cerebro de animación del caballo
        if (caballoRelampago != null)
        {
            animRelampago = caballoRelampago.GetComponentInChildren<Animator>();
        }

        // Iniciamos el ciclo infinito de preguntarle a Python
        StartCoroutine(PedirDatosContinuamente());
    }

    IEnumerator PedirDatosContinuamente()
    {
        while (true)
        {
            // Hacemos la petición a Python
            UnityWebRequest www = UnityWebRequest.Get(urlServidor);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("Error conectando a Python: " + www.error);
                Debug.Log("¿Asegúrate de que app.py esté corriendo en tu terminal!");
            }
            else
            {
                // Si Python responde, leemos el texto (JSON)
                string jsonRespuesta = www.downloadHandler.text;
                ProcesarDatosDePython(jsonRespuesta);
            }

            // Esperamos 0.2 segundos antes de volver a preguntar
            yield return new WaitForSeconds(tiempoDeActualizacion);
        }
    }

    void ProcesarDatosDePython(string json)
    {
        // Convertimos el texto JSON a variables de C# usando la clase que definimos abajo
        DatosDelServidor datos = JsonUtility.FromJson<DatosDelServidor>(json);

        if (datos != null && datos.caballos != null)
        {
            foreach (CaballoPython cp in datos.caballos)
            {
                // Vamos a buscar la información del caballo ID 1 (Relámpago)
                if (cp.id == 1 && caballoRelampago != null)
                {
                    // 1. TRADUCIR COORDENADAS:
                    // En Python, X va de -7 a 7. Y es la altura del valle.
                    // En Unity, podemos multiplicar X por 5 para que la pista sea más grande.
                    float nuevaPosicionX = cp.x * 5f;
                    float nuevaPosicionY = cp.y * 2f; // Multiplicamos para exagerar las colinas

                    // Movemos suavemente al caballo a su nueva posición
                    Vector3 posicionDestino = new Vector3(nuevaPosicionX, nuevaPosicionY, 0f);
                    caballoRelampago.transform.position = Vector3.Lerp(caballoRelampago.transform.position, posicionDestino, Time.deltaTime * 10f);

                    // 2. TRADUCIR ANIMACIÓN:
                    // Si el estado es "Corriendo", le ponemos velocidad para que mueva las patas
                    if (animRelampago != null)
                    {
                        if (cp.estado == "Corriendo")
                        {
                            animRelampago.SetFloat("State", 2f); // 2 era correr en tu Animator
                        }
                        else if (cp.estado == "Descansando" || cp.estado == "Lesionado")
                        {
                            animRelampago.SetFloat("State", 0f); // 0 es quieto
                        }
                    }
                }
            }
        }
    }
}

// =========================================================================
// CLASES PARA "TRADUCIR" EL JSON DE PYTHON A C#
// Tienen que llamarse EXACTAMENTE igual que en tu código de Python (app.py)
// =========================================================================

[System.Serializable]
public class DatosDelServidor
{
    public int iteracion;
    public int max_iteraciones;
    public string estado_carrera;
    public int ganador_id;
    public CaballoPython[] caballos;
    public float[] zanahorias;
}

[System.Serializable]
public class CaballoPython
{
    public int id;
    public string nombre;
    public float x;
    public float y;
    public string color;
    public string estado;
    public string evento_reciente;
    public bool tiene_zanahoria;
}