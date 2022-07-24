using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using static HelperFunctions;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {
    [SerializeField]
    private Transform _mesh;

    public bool useGravity = true;

    [Min(0)] public float maxAirTime = 5f;

    [Header("Velocity")]
    [SerializeField]
    private float _secondsToReachFullGroundSpeed = 5;

    [Min(0.01f)] public float secondsToReachFullAirSpeed = 0.75f;

    [SerializeField]
    private float _maxForwardGroundSpeed = 300f;

    [Min(0.01f)] public float maxForwardAirSpeed = 1000f;

    public float gravityScale = 9.82f;

    [Min(0.01f)] public float speedCorrectionFactor = 10;

    [Header("Rotation")]
    [SerializeField]
    private float _lookGroundedRotationDegsPerSecond = 90;

    public float lookAirHorizontalRotationDegsPerSecond = 90;

    public float lookAirVerticalRotationDegsPerSecond = 80;

    [Range(0f, 90f)] public float lookAirRollRotationMaxRotationAngle = 80;

    [Range(0f, 1f)] public float lookAirMaxRotationalBasedSpeedMultiplier = 0.5f;

    [SerializeField, Min(0)]
    private float _rideRotationSpeed = 70;

    [SerializeField, Min(0)]
    private float _chargeRotationSpeedExtra = 70;

    [SerializeField, Min(0.01f)]
    private float _maxTurnMagnitude = 3;

    [SerializeField]
    private float _airRotationSpeed = 50f; // TODO

    [Range(0f, 80f)] public float maxAirborneAngle = 45;

    [Range(0f, 80f)] public float minAirborneAngle = 45;

    [SerializeField]
    private float followGroundRotationAnglePerSecond = 15f;

    public float airRollRotationAnglePerSecond = 50f;

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

    [Min(0f)] public float passiveAirChargeGain = 0.1f;

    // Component
    [HideInInspector] public Rigidbody body;
    private CapsuleCollider _collider;
    private PlayerUIHandler _uiHandler;
    private PlayerData _data;
    [HideInInspector] public bool groundHit;
    [HideInInspector] public bool countAsGroundHit;
    [HideInInspector] public RaycastHit groundHitInfo;
    [HideInInspector] public Vector3 lastNormal = Vector3.zero;
    [HideInInspector] public Vector3 currentNormal = Vector3.zero;

    // States
    private PlayerPhysicsState _currentState = PlayerPhysicsState.Airborne;
    public Timer groundedCooldownTimer = new Timer(0.2f);

    // Charge
    [HideInInspector] public Vector3 chargeForce;
    [HideInInspector] public Timer chargeTimer;
    [HideInInspector] public Timer expirationTimer;
    [HideInInspector] public Timer chargeBurnoutTimer;
    [HideInInspector] public float chargeRatio;

    // Velocity
    [HideInInspector] public float speed;
    [HideInInspector] public float gravitySpeed;
    [HideInInspector] public Vector3 velocityDirection;
    [HideInInspector] public float airVerticalReductionAngle;

    // Rotation
    [HideInInspector] public Vector3 vecForward;
    private Vector3 Forward {
        get { return finalRotation * Vector3.forward; }
    }
    [HideInInspector] public Quaternion horizontalRotation;
    [HideInInspector] public Quaternion verticalRotation;
    [HideInInspector] public Quaternion rollRotation;
    [HideInInspector] public Quaternion finalRotation = new Quaternion();
    private Vector3 _meshPivotPoint;
    public Quaternion MeshRotation { get { return _mesh.rotation; } }

    public enum PlayerPhysicsState {
        Grounded,
        Airborne,
    }

    public PlayerPhysicsState CurrentState {
        get { return _currentState; }
        set {
            if (value != _currentState) {
                _currentState = value;
                switch (value) {
                    case PlayerPhysicsState.Grounded:
                        _currentPlayerState = _groundedState;
                        OnGroundedEnter();
                        break;
                    case PlayerPhysicsState.Airborne:
                        _currentPlayerState = _airBorneState;
                        break;
                }
                _currentPlayerState.OnEnter(this);
            }
        }
    }

    // Stat scalars

    // Weight multipliers should be equal to half of the primary stat
    public float WeightSpeedMultiplier {
        get {
            float weightMultiplier = 1f;
            if (_data.Stats.Weight > 2)
                weightMultiplier = (30f + (float)_data.Stats.Weight) / 32f;
            else if (_data.Stats.Weight < 2)
                weightMultiplier = (62f + (float)_data.Stats.Weight) / 64f;
            return weightMultiplier;
        }
    }

    //TODO: Implement a stat multiplier value editor, should also display final multipliers and ranges

    // Max speed = 3x
    // Min speed = 1/5x / 0.2x
    public float TopSpeedMultiplier {
        get { return GetStatMultiplierValue(_data.Stats.TopSpeed, 8, 20); }
    }
    public float BoostMultiplier { get { return GetStatMultiplierValue(_data.Stats.Boost, 8, 20); } }
    public float ChargeMultiplier { get { return GetStatMultiplierValue(_data.Stats.Charge, 8, 20); } }
    public float TurnMultiplier { get { return GetStatMultiplierValue(_data.Stats.Turn, 6, 20); } }
    public float GlideMultiplier { get { return GetStatMultiplierValue(_data.Stats.Glide, 16, 32); } }
    public float WeightChargeMultiplier { get { return GetStatMultiplierValue(_data.Stats.Weight, 8, 20); } }
    public float WeightGlideMultiplier { get { return GetStatMultiplierValue(_data.Stats.Weight, 32, 64); } }
    public float WeightTurnMultiplier { get { return GetStatMultiplierValue(_data.Stats.Weight, 4, 16); } }

    public float GetStatMultiplierValue(int stat, int overDefault, int underDefault) {
        int defaultValue = 2;
        float multiplier = 1f;
        if (_data.Stats.Weight > defaultValue)
            multiplier = (float)(overDefault - defaultValue + stat) / (float)overDefault;
        else if (_data.Stats.Weight < defaultValue)
            multiplier = (float)(underDefault - defaultValue + stat) / (float)underDefault;
        return multiplier;
    }

    #region Helpers
    private bool ShouldSlide {
        get {
            var left = Vector3.Cross(groundHitInfo.normal, Vector3.up);
            var downhill = Vector3.Cross(groundHitInfo.normal, left);
            var dot = Vector3.Dot(downhill, transform.forward);
            return dot >= -0.2f;
        }
    }

    public bool IsSurfaceClimbable(Vector3 vec1, Vector3 vec2) {
        float angle = Vector3.Angle(vec1, vec2);
        return angle < _maxClimbableSlopeAngle;
    }

    public float GetNegativeAngle(Vector3 vectorA, Vector3 vectorB) {
        float angle = Vector3.Angle(vectorA, vectorB);
        Vector3 cross = Vector3.Cross(vectorA, vectorB);
        if (cross.y < 0)
            angle = -angle;
        return angle;
    }
    #endregion

    // Start is called before the first frame update
    void Awake() {
        body = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
        _data = GetComponent<PlayerData>();

        chargeTimer = new Timer(_chargeTime);
        expirationTimer = new Timer(_chargeExpirationTime);
        chargeBurnoutTimer = new Timer(_chargeBurnoutTime);
        InitializeAirborneTimer();

        horizontalRotation = Quaternion.Euler(Vector3.up * this.transform.rotation.eulerAngles.y);
        this.transform.rotation = Quaternion.identity;
        rollRotation = Quaternion.identity;
        verticalRotation = Quaternion.Euler(this.transform.right);
        vecForward = transform.forward;
        finalRotation = transform.rotation;
        _meshPivotPoint = _mesh.position - transform.position;

        _airBorneState = new PlayerAirBorneState(this, _data);
        _groundedState = new PlayerGroundedState(this, _data);
        _currentPlayerState = _airBorneState;
    }

    void Start() {
        int playerIndex = GetComponent<Player>().PlayerIndex;
        _uiHandler = GameObject
            .FindObjectOfType<CameraManager>()
            .GetCamera(playerIndex)
            .GetComponent<PlayerUIHandler>();
    }

    private void OnGroundedEnter() {
        velocityDirection.y = 0;
        gravitySpeed = 0;
        velocityDirection = Vector3.zero;
        verticalRotation = Quaternion.identity;
        _airBorneTimer.Reset();
    }

    private void OnAirborneEnter() {
        var euler = _mesh.rotation.eulerAngles;
        _verticalRotation = Quaternion.Euler(euler.x, 0, 0);
        _rollRotation = Quaternion.Euler(0, 0, euler.z);
        _gravitySpeed = 0;
        _velocityDirection = Vector3.zero;
    }

    public void UpdatePlayerStats() {
        float time = _airBorneTimer.Time;
        InitializeAirborneTimer();
        _airBorneTimer += time;
    }

    private void InitializeAirborneTimer() { _airBorneTimer = new Timer(_maxAirTime * GlideMultiplier + _data.Stats.Glide / 2); }

    void FixedUpdate() {
        float time = Time.fixedDeltaTime;

        Vector3 groundHitOffset = Vector3.zero;
        if (CurrentState == PlayerPhysicsState.Airborne) {
            groundHitOffset = finalRotation * Vector3.forward;
        }
        groundHit = Physics.Raycast(
            transform.position + groundHitOffset,
            Vector3.down,
            out groundHitInfo,
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

        countAsGroundHit = groundHit;
        if (groundHit) {
            if (currentNormal != groundHitInfo.normal) {
                lastNormal = currentNormal;
                currentNormal = groundHitInfo.normal;
            }
            countAsGroundHit =
                Vector3.Distance(transform.position + groundHitOffset, groundHitInfo.point)
                    - _collider.height / 2
                < this.distanceFromColliderToCountAsGroundHit;
        }

        HandlePhysicsStateTransitions(time);

        switch (this.CurrentState) {
            case PlayerPhysicsState.Grounded:
                GroundedState(time);
                break;
            case PlayerPhysicsState.Airborne:
                AirborneState(time);
                break;
        }

        _mesh.position = finalRotation * _meshPivotPoint + transform.position;
        _mesh.rotation = finalRotation;
    }

    private void HandlePhysicsStateTransitions(float time) {
        if (!groundHit) {
            CurrentState = PlayerPhysicsState.Airborne;
            groundedCooldownTimer.Reset();
            lastNormal = Vector3.zero;
            currentNormal = Vector3.zero;
        }
        else {
            if (CurrentState == PlayerPhysicsState.Grounded) {
                if (lastNormal != currentNormal) // currentNormal will always have a value when groundHit is truthy
                {
                    // entering here means the player is trying to move from a surface to a new one or was flying and has now hit a surface
                    Vector3 vec = lastNormal == Vector3.zero ? Vector3.up : lastNormal; // If first time touching ground
                }
            }
        }
    }

    private void GroundedState(float time) {
        HandleGroundedRotation(time);

        // Horizontal Velocity
        Vector3 horizontalDirection = horizontalRotation * Vector3.forward;
        Quaternion rotationExtra = Quaternion.Euler(
            Vector3.up
                * 45
                / (0.01f + _rideRotationSpeed)
                * Mathf.Sign(GetNegativeAngle(velocityDirection, horizontalDirection))
        );
        velocityDirection +=
            horizontalRotation
            * rotationExtra
            * Vector3.forward
            * _rideRotationSpeed
            * WeightTurnMultiplier
            * time;
        if (velocityDirection.magnitude > _maxTurnMagnitude) {
            velocityDirection = velocityDirection.normalized * _maxTurnMagnitude;
        }

        // Speed acceleration
        // TODO: Make acceleration non-linear
        float maxSpeed = _maxForwardGroundSpeed * WeightSpeedMultiplier * TopSpeedMultiplier;
        var acceleration = time / _secondsToReachFullGroundSpeed * maxSpeed;
        speed += acceleration;
        if (speed > maxSpeed) {
            speed = Mathf.MoveTowards(speed, maxSpeed, acceleration * speedCorrectionFactor);
        }

        // TODO: Implement breaking (_data.input.isBreaking)

        OnCharge(time);

        Vector3 finalVelocity = verticalRotation * velocityDirection.normalized * speed * time;
        body.velocity = finalVelocity;
    }

    private void HandleGroundedRotation(float time) {
        // Horizontal Rotation
        Quaternion horizontalDelta = Quaternion.Euler(
            Vector3.up
                * _lookGroundedRotationDegsPerSecond
                * TurnMultiplier
                * time
                * _data.input.direction.x
                + Vector3.up
                    * _chargeRotationSpeedExtra
                    * time
                    * _data.input.direction.x
                    * (_data.input.isCharging ? 1 : 0)
        );
        horizontalRotation = horizontalRotation * horizontalDelta;

        Vector3 averageNormal = groundHitInfo.normal;
        float angleIncrement = 360f / rotationRayCount;
        int incrementCount = 1;
        for (int i = 0; i < rotationRayCount; i++) {
            RaycastHit rayData;
            var offset =
                finalRotation * Quaternion.Euler(0, -angleIncrement * i, 0) * Vector3.forward;
            if (
                Physics.Raycast(
                    transform.position + offset,
                    Vector3.down,
                    out rayData,
                    _groundRayDistance + _groundRotationRayExtraDistance,
                    _collidableLayer
                )
            ) {
                Vector3 vec = currentNormal == Vector3.zero ? Vector3.up : currentNormal; // If first time touching ground
                if (IsSurfaceClimbable(rayData.normal, vec)) {
                    averageNormal += rayData.normal;
                    incrementCount++;
                }
            }
        }
        averageNormal /= incrementCount;

        Vector3 upVec = groundHit ? averageNormal : Vector3.up;
        // TODO: Increase speed the larger the angle diff is
        verticalRotation = Quaternion.RotateTowards(
            verticalRotation,
            Quaternion.FromToRotation(Vector3.up, upVec),
            followGroundRotationAnglePerSecond * time
        );
        _finalRotation = _verticalRotation * _horizontalRotation;
        _forward = _finalRotation * Vector3.forward;
    }

    private void AirborneState(float time) {
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

        if (_speed > maxSpeed) {
            _speed = Mathf.MoveTowards(_speed, maxSpeed, acceleration * _speedCorrectionFactor);
        }

        Vector3 finalVelocity = _velocityDirection.normalized * _speed * time;

        finalVelocity += Vector3.up * GlideMultiplier * time;
        finalVelocity += Vector3.down * WeightGlideMultiplier * time;

        _airBorneTimer += time;
        if (_useGravity && _airBorneTimer.Expired) {
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

    private void HandleAirborneRotation(float time) {
        // Vertical Rotation
        Vector3 vertRotationAmount = Vector3.right * _lookAirVerticalRotationDegsPerSecond * _data.input.direction.y * time;
        Quaternion deltaPitchRotation = Quaternion.Euler(vertRotationAmount);
        float angle = 90 - Vector3.Angle(_verticalRotation * deltaPitchRotation * Vector3.forward, Vector3.up);

        // Faster decent angle
        if (angle <= 0 && Mathf.Sign(_data.input.direction.y) == 1)
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
                * _data.input.direction.x
        );
        _horizontalRotation = _horizontalRotation * horizontalDelta;

        // Roll Rotation
        // TODO: Make roll rotation dependent on air friction and torque
        _rollRotation = Quaternion.RotateTowards(
            _rollRotation,
            Quaternion.Euler(
                Vector3.forward
                    * _lookAirRollRotationMaxRotationAngle
                    * -_data.input.direction.x
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

    private void HandleChargeVelocity(float time) {
        if (_data.input.isCharging) {
            switch (CurrentState) {
                case PlayerPhysicsState.Grounded:
                    var acceleration =
                        time
                        / _secondsToReachFullGroundSpeed
                        * _maxForwardGroundSpeed
                        * WeightSpeedMultiplier
                        * TopSpeedMultiplier;
                    // TODO: create acceleration member variable
                    speed -= acceleration; // Remove ground speed addition

                    Vector3 horizontalDirection = horizontalRotation * Vector3.forward;

                    float dot = Mathf.Clamp01(
                        Vector3.Dot(horizontalDirection.normalized, velocityDirection.normalized)
                    );
                    float stopOnTurn = 1 + _chargeTimeToStopWhenTurningPercentageDenominator * (1 - dot);
                    float maxDelta = acceleration * _groundBreakSpeed / stopOnTurn * WeightChargeMultiplier;

                    speed = Mathf.MoveTowards(speed, 0, maxDelta);

                    break;
                case PlayerPhysicsState.Airborne:
                    break;
            }
        }
        else if (chargeRatio != 0 && CurrentState == PlayerPhysicsState.Grounded) {
            float boostSpeed = _boostSpeed * Mathf.Clamp01(chargeRatio) * BoostMultiplier;

            float limitMultiplier = 1.5f;
            float maxSpeed = _maxForwardGroundSpeed * WeightSpeedMultiplier * TopSpeedMultiplier;
            if (speed <= maxSpeed * limitMultiplier)
                speed += boostSpeed;

            velocityDirection = horizontalRotation * Vector3.forward;
        }
    }

    private void HandleChargeTimer(float time) {
        if (CurrentState == PlayerPhysicsState.Grounded) {
            if (_data.input.isCharging && !expirationTimer.Expired) // When you can charge
        {
                chargeTimer += time * ChargeMultiplier;
                chargeRatio = chargeTimer.Ratio;
                if (chargeTimer.Expired) expirationTimer += time;
        }
            else if (chargeRatio != 0) // On release
        {
                chargeRatio = 0;
                chargeBurnoutTimer.Reset();
                expirationTimer.Reset();
                chargeTimer.Reset();
        }

            if (expirationTimer.Expired) {
                chargeRatio = 0;
                chargeBurnoutTimer += time;
                if (chargeBurnoutTimer.Expired) {
                    chargeBurnoutTimer.Reset();
                    expirationTimer.Reset();
                    chargeTimer.Reset();
                }
            }
        }
    }

    private void HandleChargeUI() {
        if (_uiHandler) {
            float expirationRatio = expirationTimer.Expired ? 0 : expirationTimer.Ratio; // 0 to show burnout ui
            _uiHandler.SetExpirationRatio(expirationRatio);
            _uiHandler.SetBurnout(expirationTimer.Expired);

            if (expirationTimer.Expired)
                _uiHandler.SetFillRatio(1 - chargeBurnoutTimer.Ratio);
            else
                _uiHandler.SetFillRatio(chargeTimer.Ratio);
        }
    }


    private void OnValidate() {
        body = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position + Vector3.down * (_groundSphereOffset + _collider.height / 2),
            _collider.radius + _groundSphereExtraRadius
        );

        if (CurrentState == PlayerPhysicsState.Grounded) {
            Gizmos.DrawRay(transform.position, Vector3.down * _groundRayDistance);

            Gizmos.color = Color.blue;
            float angleIncrement = 360f / rotationRayCount;
            for (int i = 0; i < rotationRayCount; i++) {
                var offset =
                    finalRotation * Quaternion.Euler(0, -angleIncrement * i, 0) * Vector3.forward;
                Gizmos.DrawRay(
                    transform.position + offset,
                    Vector3.down * (_groundRayDistance + _groundRotationRayExtraDistance)
                );
            }
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, velocityDirection);
        }
        else if (CurrentState == PlayerPhysicsState.Airborne) {
            var offset = finalRotation * Vector3.forward;
            Gizmos.DrawRay(
                transform.position + offset,
                Vector3.down * (_groundRayDistance + offset.y)
            );
        }
    }
}
