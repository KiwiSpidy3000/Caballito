using UnityEngine;

/// <summary>
/// HorseRacer.cs — Caballo controlado por el JUGADOR.
/// 
/// INSTRUCCIONES DE USO:
///   1. Agrega este script al GameObject del caballo del jugador.
///   2. El GameObject DEBE tener: CharacterController + Animator.
///   3. ELIMINA o DESACTIVA MovePlayerInput.cs de este caballo.
///   4. Configura los valores en el Inspector.
///   5. Asigna la cámara (RaceCamera) en el campo "Race Camera".
///
/// CONTROLES:
///   A / Flecha Izquierda  → Moverse a la izquierda
///   D / Flecha Derecha    → Moverse a la derecha
///   Espacio               → Saltar
///   S / Flecha Abajo      → Frenar
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class HorseRacer : MonoBehaviour
{
    // ── Velocidad ──────────────────────────────────────────────────────────
    [Header("Velocidad de avance")]
    [Tooltip("Velocidad máxima en unidades/segundo")]
    public float maxSpeed = 15f;

    [Tooltip("Segundos que tarda en llegar a la velocidad máxima desde 0")]
    public float accelerationTime = 8f;

    [Tooltip("Qué tan rápido frena al presionar S / Flecha Abajo")]
    public float brakeForce = 20f;

    // ── Movimiento lateral ─────────────────────────────────────────────────
    [Header("Movimiento lateral")]
    [Tooltip("Velocidad de desplazamiento lateral")]
    public float lateralSpeed = 5f;

    [Tooltip("Límite izquierdo del carril (coordenada X)")]
    public float laneLeft = -3f;

    [Tooltip("Límite derecho del carril (coordenada X)")]
    public float laneRight = 3f;

    // ── Salto ──────────────────────────────────────────────────────────────
    [Header("Salto")]
    [Tooltip("Fuerza inicial del salto")]
    public float jumpForce = 8f;

    // ── Curvas ────────────────────────────────────────────────────────────
    [Header("Curvas y caída")]
    [Tooltip("Velocidad máxima permitida en curva antes de caerse")]
    public float maxCurveSpeed = 8f;

    // ── Colisiones ────────────────────────────────────────────────────────
    [Header("Colisiones con otros caballos")]
    [Tooltip("Velocidad que pierde al chocar con otro caballo")]
    public float horseCollisionSpeedLoss = 5f;

    // ── Cámara ────────────────────────────────────────────────────────────
    [Header("Cámara")]
    [Tooltip("Arrastra aquí el GameObject de la cámara (RaceCamera)")]
    public RaceCamera raceCamera;

    // ── Estado interno ─────────────────────────────────────────────────────
    private CharacterController _controller;
    private Animator _animator;

    private float _currentSpeed = 0f;
    private float _verticalVelocity = 0f;
    private bool _isFallen = false;
    private float _fallenTimer = 0f;
    private const float FallenRecoveryTime = 2f;

    private bool _isInCurve = false;

    // Parámetros del Animator (deben coincidir con el Horse.controller del asset)
    private static readonly int ParamVert  = Animator.StringToHash("Vert");
    private static readonly int ParamState = Animator.StringToHash("State");

    // ── Propiedades públicas ───────────────────────────────────────────────
    /// <summary>Velocidad actual (0 → maxSpeed)</summary>
    public float CurrentSpeed => _currentSpeed;
    /// <summary>Velocidad normalizada (0.0 → 1.0)</summary>
    public float NormalizedSpeed => maxSpeed > 0 ? _currentSpeed / maxSpeed : 0f;
    /// <summary>¿Está caído?</summary>
    public bool IsFallen => _isFallen;

    // ══════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator   = GetComponent<Animator>();
    }

    void Update()
    {
        if (_isFallen)
        {
            HandleFallenState();
            return;
        }

        HandleAcceleration();
        HandleLateralMovement();
        HandleJump();
        HandleCurveFall();
        ApplyForwardMovement();
        UpdateAnimator();
    }

    // ══════════════════════════════════════════════════════════════════════
    // LÓGICA PRINCIPAL
    // ══════════════════════════════════════════════════════════════════════

    void HandleAcceleration()
    {
        bool isBraking = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        if (isBraking)
        {
            _currentSpeed -= brakeForce * Time.deltaTime;
            _currentSpeed  = Mathf.Max(_currentSpeed, 0f);
        }
        else
        {
            float accel = maxSpeed / accelerationTime;
            _currentSpeed += accel * Time.deltaTime;
            _currentSpeed  = Mathf.Min(_currentSpeed, maxSpeed);
        }
    }

    void HandleLateralMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // -1, 0 o 1
        float newX = transform.position.x + horizontal * lateralSpeed * Time.deltaTime;
        newX = Mathf.Clamp(newX, laneLeft, laneRight);

        Vector3 lateralDelta = new Vector3(newX - transform.position.x, 0f, 0f);
        _controller.Move(lateralDelta);
    }

    void HandleJump()
    {
        if (_controller.isGrounded)
        {
            _verticalVelocity = -1f; // mantiene pegado al suelo

            if (Input.GetKeyDown(KeyCode.Space))
                _verticalVelocity = jumpForce;
        }
        else
        {
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }
    }

    void HandleCurveFall()
    {
        if (_isInCurve && _currentSpeed > maxCurveSpeed)
            TriggerFall();
    }

    void ApplyForwardMovement()
    {
        Vector3 move = transform.forward * _currentSpeed * Time.deltaTime
                     + Vector3.up * _verticalVelocity * Time.deltaTime;
        _controller.Move(move);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ESTADO CAÍDO
    // ══════════════════════════════════════════════════════════════════════

    void TriggerFall()
    {
        if (_isFallen) return;
        _isFallen     = true;
        _currentSpeed = 0f;
        _fallenTimer  = 0f;
        Debug.Log($"[HorseRacer] {gameObject.name} se cayó en la curva.");
    }

    void HandleFallenState()
    {
        _fallenTimer += Time.deltaTime;

        // Solo gravedad mientras está caído
        _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        _controller.Move(new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f));

        // Animación lenta de idle
        _animator.SetFloat(ParamVert,  0f);
        _animator.SetFloat(ParamState, 0f);

        if (_fallenTimer >= FallenRecoveryTime)
        {
            _isFallen     = false;
            _currentSpeed = 0f;
            Debug.Log($"[HorseRacer] {gameObject.name} se recuperó.");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // COLISIONES
    // ══════════════════════════════════════════════════════════════════════

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log($"[HorseRacer] {gameObject.name} chocó con obstáculo. Velocidad → 0.");
            _currentSpeed = 0f;
        }
        else if (hit.gameObject.CompareTag("Horse"))
        {
            Debug.Log($"[HorseRacer] {gameObject.name} chocó con otro caballo.");
            _currentSpeed -= horseCollisionSpeedLoss;
            _currentSpeed  = Mathf.Max(_currentSpeed, 0f);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // ANIMACIONES
    // ══════════════════════════════════════════════════════════════════════

    void UpdateAnimator()
    {
        float norm = NormalizedSpeed;
        _animator.SetFloat(ParamVert,  norm);
        _animator.SetFloat(ParamState, norm > 0.5f ? 1f : 0f);
    }

    // ══════════════════════════════════════════════════════════════════════
    // API PÚBLICA (para otros scripts)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llámalo desde CurveZoneTrigger.cs al entrar/salir de una curva.
    /// </summary>
    public void SetCurveZone(bool entering)
    {
        _isInCurve = entering;
    }

    /// <summary>Resetea el caballo al inicio de carrera.</summary>
    public void ResetHorse()
    {
        _currentSpeed     = 0f;
        _isFallen         = false;
        _fallenTimer      = 0f;
        _verticalVelocity = 0f;
        _isInCurve        = false;
    }

    /// <summary>Fuerza una caída desde código externo.</summary>
    public void ForceFall() => TriggerFall();
}
