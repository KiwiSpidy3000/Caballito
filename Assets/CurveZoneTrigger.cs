using UnityEngine;

/// <summary>
/// CurveZoneTrigger.cs — Marca una zona de curva en la pista.
///
/// INSTRUCCIONES DE USO:
///   1. Crea un GameObject vacío en la zona de la curva.
///   2. Agrégale un BoxCollider y marca "Is Trigger" = true.
///   3. Agrega ESTE script al mismo GameObject.
///   4. Asegúrate de que el collider cubra el ancho completo de la pista.
///
/// Cuando un caballo entra en esta zona, se activa la detección de caída
/// por velocidad excesiva en curva.
/// </summary>
public class CurveZoneTrigger : MonoBehaviour
{
    [Tooltip("Color de visualización en el editor")]
    public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);

    void OnTriggerEnter(Collider other)
    {
        var playerHorse = other.GetComponent<HorseRacer>();
        if (playerHorse != null) { playerHorse.SetCurveZone(true); return; }

        var npcHorse = other.GetComponent<HorseNPC>();
        if (npcHorse != null) { npcHorse.SetCurveZone(true); }
    }

    void OnTriggerExit(Collider other)
    {
        var playerHorse = other.GetComponent<HorseRacer>();
        if (playerHorse != null) { playerHorse.SetCurveZone(false); return; }

        var npcHorse = other.GetComponent<HorseNPC>();
        if (npcHorse != null) { npcHorse.SetCurveZone(false); }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
        }
    }
}
