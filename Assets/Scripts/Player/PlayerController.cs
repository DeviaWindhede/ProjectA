using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using static HelperFunctions;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private Transform _mesh;

    [SerializeField]
    private bool _useGravity = true;

    [SerializeField]
    private PlayerStats _playerStats;

    [SerializeField, Min(0)] private float _maxAirTime = 5f;

    [Header("Velocity")]
    [SerializeField]
    private float _secondsToReachFullGroundSpeed = 5;

    [SerializeField, Min(0.01f)]
    private float _secondsToReachFullAirSpeed = 0.75f;

    [SerializeField]
    private float _maxForwardGroundSpeed = 300f;

    [SerializeField, Min(0.01f)]
    private float _maxForwardAirSpeed = 1000f;

    [SerializeField]
    private float _gravityScale = 9.82f;

    [SerializeField, Min(0.01f)]
    private float _speedCorrectionFactor = 10;

    [Header("Rotation")]
    [SerializeField]
    private float _lookGroundedRotationDegsPerSecond = 90;

    [SerializeField]
    private float _lookAirHorizontalRotationDegsPerSecond = 90;

    [SerializeField]
    private float _lookAirVerticalRotationDegsPerSecond = 80;

    [SerializeField, Range(0f, 90f)]
    private float _lookAirRollRotationMaxRotationAngle = 80;

    [SerializeField, Range(0f, 1f)]
    private float _lookAirMaxRotationalBasedSpeedMultiplier = 0.5f;

    [SerializeField, Min(0)]
    private float _rideRotationSpeed = 70;

    [SerializeField, Min(0)]
    private float _chargeRotationSpeedExtra = 70;

    [SerializeField, Min(0.01f)]
    private float _maxTurnMagnitude = 3;

    [SerializeField]
    private float _airRotationSpeed = 50f; // TODO

    [SerializeField, Range(0f, 80f)]
    private float _maxAirborneAngle = 45;

    [SerializeField, Range(0f, 80f)]
    private float _minAirborneAngle = 45;

    [SerializeField]
    private float followGroundRotationAnglePerSecond = 15f;

    [SerializeField]
    private float airRollRotationAnglePerSecond = 50f;

    [SerializeField]
    private int rotationRayCount = 10;

    [Header("Ground Check")]
    [SerializeField]
    private float _groundSphereOffset = 0.5f;

    [SerializeField]
    private float _groundSphereExtraRadius = -0.1f;

    [SerializeField, Min(0)]
    private float distanceFromColliderToCountAsGroundHit = 1.25f;

    [SerializeField, Min(0)]
    private float _maxClimbableSlopeAngle = 45;

    [SerializeField]
    private float _groundRayDistance = 1f;

    [SerializeField]
    private float _groundRotationRayExtraDistance = 0.75f;

    [SerializeField]
    private LayerMask _collidableLayer;

    [Header("Charge")]
    [SerializeField]
    private float _chargeTime = 1f;

    [SerializeField, Min(0)]
    private float _chargeTimeToStopWhenTurningPercentageDenominator = 20f;

    [SerializeField]
    private float _chargeExpirationTime = 2f;

    [SerializeField]
    private float _chargeBurnoutTime = 3f;

    [SerializeField, Min(0)]
    private float _boostSpeed = 300;

    [SerializeField, Min(0.01f)]
    private float _groundBreakSpeed = 0.5f;

    [SerializeField, Min(0f)]
    private float _passiveAirChargeGain = 0.1f;

    // Input
    private PlayerInputValues _inputs;

    // Component
    private Rigidbody _body;
    private CapsuleCollider _collider;
    private PlayerUIHandler _uiHandler;
    private bool _groundHit;
    private bool _countAsGroundHit;
    private RaycastHit _groundHitInfo;
    private Vector3 _lastNormal = Vector3.zero;
    private Vector3 _currentNormal = Vector3.zero;

    // States
    private PlayerPhysicsState _currentState = PlayerPhysicsState.Airborne;
    private Timer _groundedCooldownTimer = new Timer(0.2f);
    private Timer _airBorneTimer;

    // Charge
    private Vector3 _chargeForce;
    private Timer _chargeTimer;
    private Timer _expirationTimer;
    private Timer _chargeBurnoutTimer;
    private float _chargeRatio;

    // Velocity
    private float _speed;
    private float _gravitySpeed;
    private Vector3 _velocityDirection;
    private float _airVerticalReductionAngle;

    // Rotation
    private Vector3 _forward;
    private Vector3 Forward
    {
        get { return _finalRotation * Vector3.forward; }
    }
    private Quaternion _horizontalRotation;
    private Quaternion _verticalRotation;
    private Quaternion _rollRotation;
    private Quaternion _finalRotation = new Quaternion();
    private Vector3 _meshPivotPoint;

    private enum PlayerPhysicsState
    {
        Grounded,
        Airborne,
    }

    private PlayerPhysicsState CurrentState
    {
        get { return _currentState; }
        set
        {
            if (value != _currentState)
            {
                _currentState = value;
                switch (value)
                {
                    case PlayerPhysicsState.Grounded:
                        OnGroundedEnter();
                        break;
                    case PlayerPhysicsState.Airborne:
                        OnAirborneEnter();
                        break;
                }
            }
        }
    }

    // Stat scalars

    // Weight multipliers should be equal to half of the primary stat
    private float WeightSpeedMultiplier
    {
        get
        {
            float weightMultiplier = 1f;
            if (_playerStats.Weight > 2)
                weightMultiplier = (30f + (float)_playerStats.Weight) / 32f;
            else if (_playerStats.Weight < 2)
                weightMultiplier = (62f + (float)_playerStats.Weight) / 64f;
            return weightMultiplier;
        }
    }

    //TODO: Implement a stat multiplier value editor, should also display final multipliers and ranges

    // Max speed = 3x
    // Min speed = 1/5x / 0.2x
    private float TopSpeedMultiplier
    {
        get { return GetStatMultiplierValue(_playerStats.TopSpeed, 8, 20); }
    }
    private float BoostMultiplier
    {
        get { return GetStatMultiplierValue(_playerStats.Boost, 8, 20); }
    }
    private float ChargeMultiplier
    {
        get { return GetStatMultiplierValue(_playerStats.Charge, 8, 20); }
    }
    private float TurnMultiplier
    {
        get { return GetStatMultiplierValue(_playerStats.Turn, 6, 20); }
    }
    private float GlideMultiplier
    {
        get { return GetStatMultiplierValue(_playerStats.Glide, 16, 32); }
    }

    private float WeightChargeMultiplier
    {
        get { return GetStatMultiplierValue(_playerStats.Weight, 8, 20); }
    }
    private float WeightGlideMultiplier
    {
        get { return GetStatMultiplierValue(_playerStats.Weight, 32, 64); }
    }
    private float WeightTurnMultiplier
    {
        get { return GetStatMultiplierValue(_playerStats.Weight, 4, 16); }
    }

    private float GetStatMultiplierValue(int stat, int overDefault, int underDefault)
    {
        int defaultValue = 2;
        float multiplier = 1f;
        if (_playerStats.Weight > defaultValue)
            multiplier = (float)(overDefault - defaultValue + stat) / (float)overDefault;
        else if (_playerStats.Weight < defaultValue)
            multiplier = (float)(underDefault - defaultValue + stat) / (float)underDefault;
        return multiplier;
    }

    #region Helpers
    private bool ShouldSlide
    {
        get
        {
            var left = Vector3.Cross(_groundHitInfo.normal, Vector3.up);
            var downhill = Vector3.Cross(_groundHitInfo.normal, left);
            var dot = Vector3.Dot(downhill, transform.forward);
            return dot >= -0.2f;
        }
    }

    private bool IsSurfaceClimbable(Vector3 vec1, Vector3 vec2)
    {
        float angle = Vector3.Angle(vec1, vec2);
        return angle < _maxClimbableSlopeAngle;
    }

    private float GetNegativeAngle(Vector3 vectorA, Vector3 vectorB)
    {
        float angle = Vector3.Angle(vectorA, vectorB);
        Vector3 cross = Vector3.Cross(vectorA, vectorB);
        if (cross.y < 0)
            angle = -angle;
        return angle;
    }
    #endregion

    // Start is called before the first frame update
    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();

        _chargeTimer = new Timer(_chargeTime);
        _expirationTimer = new Timer(_chargeExpirationTime);
        _chargeBurnoutTimer = new Timer(_chargeBurnoutTime);
        InitializeAirborneTimer();

        _horizontalRotation = Quaternion.Euler(Vector3.up * this.transform.rotation.eulerAngles.y);
        this.transform.rotation = Quaternion.identity;
        _rollRotation = Quaternion.identity;
        _verticalRotation = Quaternion.Euler(this.transform.right);
        _forward = transform.forward;
        _finalRotation = transform.rotation;
        _meshPivotPoint = _mesh.position - transform.position;
    }

    void Start()
    {
        int playerIndex = GetComponent<Player>().PlayerIndex;
        _uiHandler = GameObject
            .FindObjectOfType<CameraManager>()
            .GetCamera(playerIndex)
            .GetComponent<PlayerUIHandler>();
    }

    private void OnGroundedEnter()
    {
        _velocityDirection.y = 0;
        _gravitySpeed = 0;
        _velocityDirection = Vector3.zero;
        _verticalRotation = Quaternion.identity;
        _airBorneTimer.Reset();
    }

    private void OnAirborneEnter()
    {
        var euler = _mesh.rotation.eulerAngles;
        _verticalRotation = Quaternion.Euler(euler.x, 0, 0);
        _rollRotation = Quaternion.Euler(0, 0, euler.z);
        _gravitySpeed = 0;
        _velocityDirection = Vector3.zero;
    }

    public void UpdateInputs(PlayerInputValues inputs)
    {
        _inputs = inputs;
    }

    public void UpdatePlayerStats(PlayerStats stats)
    {
        _playerStats = stats;

        float time = _airBorneTimer.Time;
        InitializeAirborneTimer();
        _airBorneTimer += time;
    }

    private void InitializeAirborneTimer() { _airBorneTimer = new Timer(_maxAirTime * GlideMultiplier + _playerStats.Glide / 2); }

    void FixedUpdate()
    {
        float time = Time.fixedDeltaTime;

        Vector3 groundHitOffset = Vector3.zero;
        if (CurrentState == PlayerPhysicsState.Airborne)
        {
            groundHitOffset = _finalRotation * Vector3.forward;
        }
        _groundHit = Physics.Raycast(
            transform.position + groundHitOffset,
            Vector3.down,
            out _groundHitInfo,
            _groundRayDistance + groundHitOffset.y,
            _collidableLayer
        );
        // TODO: Add proper sphere ground check (can also probably be used for average normal rotation on ground state)
        #region Sphere Ground Check TODO
        // float groundHitOffset = _groundSphereOffset + _collider.height / 2;
        // _groundHit = Physics.SphereCast(
        //     transform.position,
        //     _collider.radius + _groundSphereExtraRadius,
        //     Vector3.down,
        //     out _groundHitInfo,
        //     groundHitOffset,
        //     _collidableLayer
        // );
        // if (!_countAsGroundHit) {
        //     float offset = _groundSphereOffset + _collider.height / 2;
        //     _countAsGroundHit = Physics.SphereCast(
        //         transform.position,
        //         _collider.radius + _groundSphereExtraRadius,
        //         Vector3.down,
        //         out _groundHitInfo,
        //         offset,
        //         _collidableLayer
        //     );
        // }
        #endregion

        _countAsGroundHit = _groundHit;
        if (_groundHit)
        {
            if (_currentNormal != _groundHitInfo.normal)
            {
                _lastNormal = _currentNormal;
                _currentNormal = _groundHitInfo.normal;
            }
            _countAsGroundHit =
                Vector3.Distance(transform.position + groundHitOffset, _groundHitInfo.point)
                    - _collider.height / 2
                < this.distanceFromColliderToCountAsGroundHit;
        }

        HandlePhysicsStateTransitions(time);

        switch (this.CurrentState)
        {
            case PlayerPhysicsState.Grounded:
                GroundedState(time);
                break;
            case PlayerPhysicsState.Airborne:
                AirborneState(time);
                break;
        }
        _mesh.position = _finalRotation * _meshPivotPoint + transform.position;
        _mesh.rotation = _finalRotation;
    }

    private void HandlePhysicsStateTransitions(float time)
    {
        if (!_groundHit)
        {
            CurrentState = PlayerPhysicsState.Airborne;
            _groundedCooldownTimer.Reset();
            _lastNormal = Vector3.zero;
            _currentNormal = Vector3.zero;
        }
        else
        {
            if (CurrentState == PlayerPhysicsState.Grounded)
            {
                if (_lastNormal != _currentNormal) // currentNormal will always have a value when groundHit is truthy
                {
                    // entering here means the player is trying to move from a surface to a new one or was flying and has now hit a surface
                    Vector3 vec = _lastNormal == Vector3.zero ? Vector3.up : _lastNormal; // If first time touching ground
                    if (!IsSurfaceClimbable(_groundHitInfo.normal, vec)) CurrentState = PlayerPhysicsState.Airborne;
                }
            }
            else if (CurrentState == PlayerPhysicsState.Airborne)
            {
                _lastNormal = Vector3.zero;
                _currentNormal = Vector3.zero;
                _groundedCooldownTimer.Time += time; // Cooldown until player is able to touch ground again
                if (_groundedCooldownTimer.Expired)
                {
                    if (_countAsGroundHit && IsSurfaceClimbable(_groundHitInfo.normal, Vector3.up))
                    {
                        CurrentState = PlayerPhysicsState.Grounded;
                        _groundedCooldownTimer.Reset();
                    }
                }
            }
        }
    }

    private void GroundedState(float time)
    {
        HandleGroundedRotation(time);

        // Horizontal Velocity
        Vector3 horizontalDirection = _horizontalRotation * Vector3.forward;
        Quaternion rotationExtra = Quaternion.Euler(
            Vector3.up
                * 45
                / (0.01f + _rideRotationSpeed)
                * Mathf.Sign(GetNegativeAngle(_velocityDirection, horizontalDirection))
        );
        _velocityDirection +=
            _horizontalRotation
            * rotationExtra
            * Vector3.forward
            * _rideRotationSpeed
            * WeightTurnMultiplier
            * time;
        if (_velocityDirection.magnitude > _maxTurnMagnitude)
        {
            _velocityDirection = _velocityDirection.normalized * _maxTurnMagnitude;
        }

        // Speed acceleration
        // TODO: Make acceleration non-linear
        float maxSpeed = _maxForwardGroundSpeed * WeightSpeedMultiplier * TopSpeedMultiplier;
        var acceleration = time / _secondsToReachFullGroundSpeed * maxSpeed;
        _speed += acceleration;
        if (_speed > maxSpeed)
        {
            _speed = Mathf.MoveTowards(_speed, maxSpeed, acceleration * _speedCorrectionFactor);
        }

        // TODO: Implement breaking (_inputs.isBreaking)

        OnCharge(time);

        Vector3 finalVelocity = _verticalRotation * _velocityDirection.normalized * _speed * time;
        _body.velocity = finalVelocity;
    }

    private void HandleGroundedRotation(float time)
    {
        // Horizontal Rotation
        Quaternion horizontalDelta = Quaternion.Euler(
            Vector3.up
                * _lookGroundedRotationDegsPerSecond
                * TurnMultiplier
                * time
                * _inputs.direction.x
                + Vector3.up
                    * _chargeRotationSpeedExtra
                    * time
                    * _inputs.direction.x
                    * (_inputs.isCharging ? 1 : 0)
        );
        _horizontalRotation = _horizontalRotation * horizontalDelta;

        Vector3 averageNormal = _groundHitInfo.normal;
        float angleIncrement = 360f / rotationRayCount;
        int incrementCount = 1;
        for (int i = 0; i < rotationRayCount; i++)
        {
            RaycastHit rayData;
            var offset =
                _finalRotation * Quaternion.Euler(0, -angleIncrement * i, 0) * Vector3.forward;
            if (
                Physics.Raycast(
                    transform.position + offset,
                    Vector3.down,
                    out rayData,
                    _groundRayDistance + _groundRotationRayExtraDistance,
                    _collidableLayer
                )
            )
            {
                Vector3 vec = _currentNormal == Vector3.zero ? Vector3.up : _currentNormal; // If first time touching ground
                if (IsSurfaceClimbable(rayData.normal, vec))
                {
                    averageNormal += rayData.normal;
                    incrementCount++;
                }
            }
        }
        averageNormal /= incrementCount;

        Vector3 upVec = _groundHit ? averageNormal : Vector3.up;
        // TODO: Increase speed the larger the angle diff is
        _verticalRotation = Quaternion.RotateTowards(
            _verticalRotation,
            Quaternion.FromToRotation(Vector3.up, upVec),
            followGroundRotationAnglePerSecond * time
        );
        _finalRotation = _verticalRotation * _horizontalRotation;
        _forward = _finalRotation * Vector3.forward;
    }

    private void AirborneState(float time)
    {
        HandleAirborneRotation(time);
        _velocityDirection = _forward;

        // Speed acceleration
        // TODO: Make acceleration non-linear
        float reductionMultiplier = 1;
        if (_velocityDirection.y > 0) {
            reductionMultiplier =
                _lookAirMaxRotationalBasedSpeedMultiplier
                + (1 - _lookAirMaxRotationalBasedSpeedMultiplier) * (1 - _airVerticalReductionAngle); // TODO: Add variable to this
        }

        float maxSpeed =
            _maxForwardAirSpeed * WeightSpeedMultiplier * TopSpeedMultiplier * reductionMultiplier;
        var acceleration = time / _secondsToReachFullAirSpeed * maxSpeed;
        _speed += acceleration;

        if (_speed > maxSpeed)
        {
            _speed = Mathf.MoveTowards(_speed, maxSpeed, acceleration * _speedCorrectionFactor);
        }

        Vector3 finalVelocity = _velocityDirection.normalized * _speed * time;

        finalVelocity += Vector3.up * GlideMultiplier * time;
        finalVelocity += Vector3.down * WeightGlideMultiplier * time;

        _airBorneTimer += time;
        if (_useGravity && _airBorneTimer.Expired)
        {
            _gravitySpeed += _gravityScale;

            // Counteracts upwards velocity
            if (Mathf.Sign(_velocityDirection.normalized.y) == 1)
                finalVelocity += Vector3.down * finalVelocity.y;

            finalVelocity += Vector3.down * _gravitySpeed * time;
        }

        OnCharge(time);
        finalVelocity += _chargeForce * time;

        _body.velocity = finalVelocity;
        _chargeForce = Vector3.zero;
    }

    private void HandleAirborneRotation(float time)
    {
        // Vertical Rotation
        Vector3 vertRotationAmount = Vector3.right * _lookAirVerticalRotationDegsPerSecond * _inputs.direction.y * time;
        Quaternion deltaPitchRotation = Quaternion.Euler(vertRotationAmount);
        float angle = 90 - Vector3.Angle(_verticalRotation * deltaPitchRotation * Vector3.forward, Vector3.up);

        // Faster decent angle
        if (angle <= 0 && Mathf.Sign(_inputs.direction.y) == 1)
            deltaPitchRotation = Quaternion.Euler(vertRotationAmount * 2f);

        // Angle cap
        if (angle <= -_minAirborneAngle)
            _verticalRotation = Quaternion.Euler(Vector3.right * _minAirborneAngle);
        else if (angle >= _maxAirborneAngle)
            _verticalRotation = Quaternion.Euler(Vector3.right * -_maxAirborneAngle);
        else
            _verticalRotation = _verticalRotation * deltaPitchRotation;

        // Horizontal Rotation
        float reductionAngle = Mathf.Sign(angle) >= 0 ? _maxAirborneAngle : -_minAirborneAngle;
        _airVerticalReductionAngle = angle / reductionAngle;
        float reductionMultiplier =
            _lookAirMaxRotationalBasedSpeedMultiplier
            + (1 - _lookAirMaxRotationalBasedSpeedMultiplier) * _airVerticalReductionAngle;
        Quaternion horizontalDelta = Quaternion.Euler(
            Vector3.up
                * _lookAirHorizontalRotationDegsPerSecond
                * reductionMultiplier
                * TurnMultiplier
                * time
                * _inputs.direction.x
        );
        _horizontalRotation = _horizontalRotation * horizontalDelta;

        // Roll Rotation
        // TODO: Make roll rotation dependent on air friction and torque
        _rollRotation = Quaternion.RotateTowards(
            _rollRotation,
            Quaternion.Euler(
                Vector3.forward
                    * _lookAirRollRotationMaxRotationAngle
                    * -_inputs.direction.x
                    * reductionMultiplier
            ),
            airRollRotationAnglePerSecond * time
        );

        _finalRotation = _horizontalRotation * _verticalRotation * _rollRotation;
        _forward = _finalRotation * Vector3.forward;
    }

    // TODO: Was hastily implemented, please refactor
    private void OnCharge(float time) //state machine?
    {
        HandleChargeVelocity(time);
        HandleChargeTimer(time);
        HandleChargeUI();
    }

    private void HandleChargeVelocity(float time)
    {
        if (_inputs.isCharging)
        {
            switch (CurrentState)
            {
                case PlayerPhysicsState.Grounded:
                    var acceleration =
                        time
                        / _secondsToReachFullGroundSpeed
                        * _maxForwardGroundSpeed
                        * WeightSpeedMultiplier
                        * TopSpeedMultiplier;
                    // TODO: create acceleration member variable
                    _speed -= acceleration; // Remove ground speed addition

                    Vector3 horizontalDirection = _horizontalRotation * Vector3.forward;

                    float dot = Mathf.Clamp01(
                        Vector3.Dot(horizontalDirection.normalized, _velocityDirection.normalized)
                    );
                    float stopOnTurn = 1 + _chargeTimeToStopWhenTurningPercentageDenominator * (1 - dot);
                    float maxDelta = acceleration * _groundBreakSpeed / stopOnTurn * WeightChargeMultiplier;

                    _speed = Mathf.MoveTowards(_speed, 0, maxDelta);

                    break;
                case PlayerPhysicsState.Airborne:
                    _chargeForce = Vector3.down * _speed + Vector3.down * _gravityScale * 2;
                    break;
            }
        }
        else if (_chargeRatio != 0 && CurrentState == PlayerPhysicsState.Grounded)
        {
            float boostSpeed = _boostSpeed * Mathf.Clamp01(_chargeRatio) * BoostMultiplier;

            float limitMultiplier = 1.5f;
            float maxSpeed = _maxForwardGroundSpeed * WeightSpeedMultiplier * TopSpeedMultiplier;
            if (_speed <= maxSpeed * limitMultiplier)
                _speed += boostSpeed;

            _velocityDirection = _horizontalRotation * Vector3.forward;
        }
    }

    private void HandleChargeTimer(float time)
    {
        if (CurrentState == PlayerPhysicsState.Airborne) // Unexpirable charge gain
        {
            _chargeTimer += _passiveAirChargeGain * time;
            _chargeRatio = _chargeTimer.Ratio;
            _expirationTimer.Reset();
        }
        else if (_inputs.isCharging && !_expirationTimer.Expired) // When you can charge
        {
            _chargeTimer += time * ChargeMultiplier;
            _chargeRatio = _chargeTimer.Ratio;
            if (_chargeTimer.Expired) _expirationTimer += time;
        }
        else if (_chargeRatio != 0 && CurrentState == PlayerPhysicsState.Grounded) // On release
        {
            _chargeRatio = 0;
            _chargeBurnoutTimer.Reset();
            _expirationTimer.Reset();
            _chargeTimer.Reset();
        }

        if (_expirationTimer.Expired)
        {
            _chargeRatio = 0;
            _chargeBurnoutTimer += time;
            if (_chargeBurnoutTimer.Expired)
            {
                _chargeBurnoutTimer.Reset();
                _expirationTimer.Reset();
                _chargeTimer.Reset();
            }
        }
    }

    private void HandleChargeUI()
    {
        if (_uiHandler) {
            float expirationRatio = _expirationTimer.Expired ? 0 : _expirationTimer.Ratio; // 0 to show burnout ui
            _uiHandler.SetExpirationRatio(expirationRatio);
            _uiHandler.SetBurnout(_expirationTimer.Expired);

            if (_expirationTimer.Expired)
                _uiHandler.SetFillRatio(1 - _chargeBurnoutTimer.Ratio);
            else
                _uiHandler.SetFillRatio(_chargeTimer.Ratio);
        }
    }


    private void OnValidate()
    {
        _body = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position + Vector3.down * (_groundSphereOffset + _collider.height / 2),
            _collider.radius + _groundSphereExtraRadius
        );

        if (CurrentState == PlayerPhysicsState.Grounded)
        {
            Gizmos.DrawRay(transform.position, Vector3.down * _groundRayDistance);

            Gizmos.color = Color.blue;
            float angleIncrement = 360f / rotationRayCount;
            for (int i = 0; i < rotationRayCount; i++)
            {
                var offset =
                    _finalRotation * Quaternion.Euler(0, -angleIncrement * i, 0) * Vector3.forward;
                Gizmos.DrawRay(
                    transform.position + offset,
                    Vector3.down * (_groundRayDistance + _groundRotationRayExtraDistance)
                );
            }
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, _velocityDirection);
        }
        else if (CurrentState == PlayerPhysicsState.Airborne)
        {
            var offset = _finalRotation * Vector3.forward;
            Gizmos.DrawRay(
                transform.position + offset,
                Vector3.down * (_groundRayDistance + offset.y)
            );
        }
    }
}
