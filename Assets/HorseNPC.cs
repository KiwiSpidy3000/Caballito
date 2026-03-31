using UnityEngine;

/// <summary>
/// HorseNPC.cs — Caballo controlado por la IA (o por el servidor Python).
///
/// INSTRUCCIONES DE USO:
///   1. Agrega este script a cada caballo NPC (no el del jugador).
///   2. El GameObject DEBE tener: CharacterController + Animator.
///   3. Configura los valores en el Inspector.
///   4. Desde ConexionServidor.cs llama a SetTargetSpeed() y SetLateralPosition()
///      para moverlos según los datos del servidor.
///
/// El NPC acelera y desacelera suavemente hacia su velocidad objetivo,
/// y se comporta igual que el caballo del jugador en colisiones y curvas.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class HorseNPC : MonoBehaviour
{
    // ── Velocidad ──────────────────────────────────────────────────────────
    [Header("Velocidad")]
    public float maxSpeed = 15f;
    public float accelerationTime = 8f;

    [Tooltip("Suavizado al cambiar velocidad objetivo (mayor = más suave)")]
    public float speedSmoothTime = 1f;

    // ── Movimiento lateral ─────────────────────────────────────────────────
    [Header("Carril")]
    public float laneLeft  = -3f;
    public float laneRight =  3f;

    [Tooltip("Velocidad con la que se mueve hacia su carril objetivo")]
    public float lateralSmoothSpeed = 4f;

    // ── Curvas ────────────────────────────────────────────────────────────
    [Header("Curvas")]
    public float maxCurveSpeed = 8f;

    // ── Colisiones ────────────────────────────────────────────────────────
    [Header("Colisiones")]
    public float horseCollisionSpeedLoss = 5f;

    // ── Estado interno ─────────────────────────────────────────────────────
    private CharacterController _controller;
    private Animator _animator;

    private float _currentSpeed   = 0f;
    private float _targetSpeed    = 0f;     // velocidad objetivo (la fija el servidor o la IA)
    private float _speedVelocity  = 0f;     // usado por SmoothDamp

    private float _targetX        = 0f;     // posición lateral objetivo
    private float _verticalVelocity = 0f;

    private bool  _isFallen       = false;
    private float _fallenTimer    = 0f;
    private const float FallenRecoveryTime = 2f;

    private bool  _isInCurve      = false;

    private static readonly int ParamVert  = Animator.StringToHash("Vert");
    private static readonly int ParamState = Animator.StringToHash("State");

    // ── Propiedades públicas ───────────────────────────────────────────────
    public float CurrentSpeed    => _currentSpeed;
    public float NormalizedSpeed => maxSpeed > 0 ? _currentSpeed / maxSpeed : 0f;
    public bool  IsFallen        => _isFallen;

    // ══════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator   = GetComponent<Animator>();
        _targetX    = transform.position.x;
    }

    void Update()
    {
        if (_isFallen)
        {
            HandleFallenState();
            return;
        }

        SmoothAcceleration();
        SmoothLateralMovement();
        ApplyGravity();
        HandleCurveFall();
        ApplyMovement();
        UpdateAnimator();
    }

    // ══════════════════════════════════════════════════════════════════════
    // LÓGICA
    // ══════════════════════════════════════════════════════════════════════

    void SmoothAcceleration()
    {
        // Aceleración automática hasta maxSpeed si no hay objetivo fijado externamente
        if (_targetSpeed <= 0f)
        {
            float accel = maxSpeed / accelerationTime;
            _targetSpeed += accel * Time.deltaTime;
            _targetSpeed  = Mathf.Min(_targetSpeed, maxSpeed);
        }

        _currentSpeed = Mathf.SmoothDamp(_currentSpeed, _targetSpeed, ref _speedVelocity, speedSmoothTime);
    }

    void SmoothLateralMovement()
    {
        float newX = Mathf.MoveTowards(transform.position.x, _targetX, lateralSmoothSpeed * Time.deltaTime);
        newX = Mathf.Clamp(newX, laneLeft, laneRight);
        Vector3 lateralDelta = new Vector3(newX - transform.position.x, 0f, 0f);
        _controller.Move(lateralDelta);
    }

    void ApplyGravity()
    {
        if (_controller.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
    }

    void HandleCurveFall()
    {
        if (_isInCurve && _currentSpeed > maxCurveSpeed)
            TriggerFall();
    }

    void ApplyMovement()
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
        _targetSpeed  = 0f;
        _fallenTimer  = 0f;
        Debug.Log($"[HorseNPC] {gameObject.name} se cayó en la curva.");
    }

    void HandleFallenState()
    {
        _fallenTimer += Time.deltaTime;
        _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        _controller.Move(new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f));

        _animator.SetFloat(ParamVert,  0f);
        _animator.SetFloat(ParamState, 0f);

        if (_fallenTimer >= FallenRecoveryTime)
        {
            _isFallen     = false;
            _currentSpeed = 0f;
            _targetSpeed  = 0f;
            Debug.Log($"[HorseNPC] {gameObject.name} se recuperó.");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // COLISIONES
    // ══════════════════════════════════════════════════════════════════════

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Obstacle"))
        {
            _currentSpeed = 0f;
            _targetSpeed  = 0f;
        }
        else if (hit.gameObject.CompareTag("Horse"))
        {
            _currentSpeed -= horseCollisionSpeedLoss;
            _currentSpeed  = Mathf.Max(_currentSpeed, 0f);
            _targetSpeed   = _currentSpeed;
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
    // API PÚBLICA
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fija la velocidad objetivo del NPC.
    /// Llámalo desde ConexionServidor.cs con el valor del servidor.
    /// Valor entre 0 y maxSpeed.
    /// </summary>
    public void SetTargetSpeed(float speed)
    {
        if (_isFallen) return;
        _targetSpeed = Mathf.Clamp(speed, 0f, maxSpeed);
    }

    /// <summary>
    /// Fija la posición lateral objetivo del NPC.
    /// Llámalo desde ConexionServidor.cs con el valor X del servidor.
    /// </summary>
    public void SetLateralPosition(float x)
    {
        _targetX = Mathf.Clamp(x, laneLeft, laneRight);
    }

    /// <summary>
    /// Notifica al NPC que entró o salió de una curva.
    /// </summary>
    public void SetCurveZone(bool entering) => _isInCurve = entering;

    /// <summary>Resetea el NPC al inicio de carrera.</summary>
    public void ResetHorse()
    {
        _currentSpeed     = 0f;
        _targetSpeed      = 0f;
        _isFallen         = false;
        _fallenTimer      = 0f;
        _verticalVelocity = 0f;
        _isInCurve        = false;
        _targetX          = transform.position.x;
    }

    /// <summary>Fuerza una caída desde código externo.</summary>
    public void ForceFall() => TriggerFall();
}
