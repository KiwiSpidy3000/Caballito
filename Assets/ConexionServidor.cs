using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// ConexionServidor.cs — VERSIÓN ACTUALIZADA
///
/// Cambios respecto al original:
///   - Ahora mueve los caballos NPC usando HorseNPC.SetTargetSpeed() y
///     SetLateralPosition() en lugar de moverlos directamente con transform.
///   - El caballo del jugador NO se mueve desde aquí (lo controla HorseRacer).
///   - Se agregó soporte para múltiples caballos NPC con un arreglo.
///
/// INSTRUCCIONES DE USO:
///   1. Reemplaza tu ConexionServidor.cs existente con este archivo.
///   2. En el Inspector, arrastra cada GameObject de caballo NPC
///      al arreglo "Caballos NPC".
///   3. El orden del arreglo debe coincidir con los IDs del servidor Python
///      (índice 0 = caballo ID 1 del servidor, etc. — ajusta según tu lógica).
/// </summary>
public class ConexionServidor : MonoBehaviour
{
    [Header("Configuración del servidor")]
    public string urlServidor = "http://127.0.0.1:5000/tick";
    public float tiempoDeActualizacion = 0.2f;

    [Header("Caballos NPC (en orden de ID del servidor)")]
    [Tooltip("Arrastra aquí los GameObjects de los caballos NPC (NO el del jugador)")]
    public HorseNPC[] caballosNPC;

    void Start()
    {
        StartCoroutine(PedirDatosContinuamente());
    }

    IEnumerator PedirDatosContinuamente()
    {
        while (true)
        {
            UnityWebRequest www = UnityWebRequest.Get(urlServidor);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning("[ConexionServidor] No se pudo conectar a Python: " + www.error);
            }
            else
            {
                ProcesarDatosDePython(www.downloadHandler.text);
            }

            yield return new WaitForSeconds(tiempoDeActualizacion);
        }
    }

    void ProcesarDatosDePython(string json)
    {
        DatosDelServidor datos = JsonUtility.FromJson<DatosDelServidor>(json);
        if (datos == null || datos.caballos == null) return;

        foreach (CaballoPython cp in datos.caballos)
        {
            // El ID 1 es el jugador — lo saltamos, lo controla HorseRacer
            // Ajusta esta condición según cómo identifiques al jugador en tu servidor
            if (cp.id == 1) continue;

            // Los NPC van del ID 2 en adelante → índice en el arreglo = id - 2
            int index = cp.id - 2;
            if (index < 0 || index >= caballosNPC.Length) continue;

            HorseNPC npc = caballosNPC[index];
            if (npc == null) continue;

            // Posición lateral: el servidor da X de -7 a 7, escalamos al carril de Unity
            float posX = cp.x * (npc.laneRight / 7f);
            npc.SetLateralPosition(posX);

            // Velocidad: la estimamos según el estado del servidor
            float velocidadObjetivo = 0f;
            switch (cp.estado)
            {
                case "Corriendo":   velocidadObjetivo = npc.maxSpeed;         break;
                case "Caminando":   velocidadObjetivo = npc.maxSpeed * 0.4f;  break;
                case "Lesionado":   velocidadObjetivo = 0f;                   break;
                case "Descansando": velocidadObjetivo = 0f;                   break;
                default:            velocidadObjetivo = npc.maxSpeed * 0.5f;  break;
            }
            npc.SetTargetSpeed(velocidadObjetivo);
        }
    }
}

// ── Clases de deserialización JSON (igual que el original) ────────────────

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
