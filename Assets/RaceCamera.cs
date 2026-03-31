using UnityEngine;

/// <summary>
/// RaceCamera.cs — Cámara que sigue al caballo del jugador desde atrás.
///
/// INSTRUCCIONES DE USO:
///   1. Crea un GameObject vacío llamado "RaceCamera" en la escena.
///   2. Agrega tu Main Camera como hijo de ese GameObject.
///   3. Agrega ESTE script al GameObject "RaceCamera" (no a la cámara).
///   4. Arrastra el Transform del caballo del jugador al campo "Target".
///   5. Arrastra el RaceCamera al campo correspondiente en HorseRacer.
///
/// NOTA: Este script REEMPLAZA a ThirdPersonCamera.cs y PlayerCamera.cs
/// para el modo carrera. Los scripts originales del asset se pueden
/// mantener desactivados.
/// </summary>
public class RaceCamera : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Arrastra aquí el Transform del caballo del jugador")]
    public Transform target;

    [Header("Posición")]
    [Tooltip("Distancia detrás del caballo")]
    public float distance = 6f;

    [Tooltip("Altura sobre el caballo")]
    public float height = 2.5f;

    [Tooltip("Suavidad del seguimiento (menor = más suave)")]
    public float smoothSpeed = 8f;

    [Header("Rotación")]
    [Tooltip("Ángulo vertical de la cámara hacia abajo (en grados)")]
    public float pitchAngle = 10f;

    private Vector3 _velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // Posición deseada: detrás y arriba del caballo
        Vector3 desiredPos = target.position
                           - target.forward * distance
                           + Vector3.up * height;

        // Suavizamos el movimiento
        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref _velocity,
            1f / smoothSpeed, Mathf.Infinity, Time.deltaTime);

        // Miramos al caballo con el ángulo definido
        Vector3 lookTarget = target.position + Vector3.up * 1f;
        transform.LookAt(lookTarget);
    }
}
