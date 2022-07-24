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

    // TODO: MOVE TO STATES
    public int rotationRayCount = 10;

    [Header("Ground Check")]
    [SerializeField]
    private float _groundSphereOffset = 0.15f;

    [SerializeField]
    private float _groundSphereExtraRadius = -0.05f;

    [SerializeField, Min(0)]
    private float distanceFromColliderToCountAsGroundHit = 1.3f;

    [SerializeField, Min(0)]
    private float _maxClimbableSlopeAngle = 80;

    public float groundRayDistance = 2.5f;

    public float groundRotationRayExtraDistance = 0f;

    public LayerMask collidableLayer;


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

    // Rotation
    [HideInInspector] public Vector3 vecForward;
    private Vector3 Forward { get { return finalRotation * Vector3.forward; } }
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
                _currentPlayerState.OnExit(this);
                switch (value) { // TODO
                    case PlayerPhysicsState.Grounded:
                        _currentPlayerState = _groundedState;
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

    private PlayerAirBorneState _airBorneState;
    private PlayerGroundedState _groundedState;
    private PlayerState _currentPlayerState;

    // Start is called before the first frame update
    void Awake() {
        body = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
        _data = GetComponent<PlayerData>();

        chargeTimer = new Timer(_data._chargeTime);
        expirationTimer = new Timer(_data._chargeExpirationTime);
        chargeBurnoutTimer = new Timer(_data._chargeBurnoutTime);

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
            groundRayDistance + groundHitOffset.y,
            collidableLayer
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

        _currentPlayerState.OnUpdate(this, _data);
        HandleChargeUI(); // TODO

        _mesh.position = finalRotation * _meshPivotPoint + transform.position;
        _mesh.rotation = finalRotation;
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
            Gizmos.DrawRay(transform.position, Vector3.down * groundRayDistance);

            Gizmos.color = Color.blue;
            float angleIncrement = 360f / rotationRayCount;
            for (int i = 0; i < rotationRayCount; i++) {
                var offset =
                    finalRotation * Quaternion.Euler(0, -angleIncrement * i, 0) * Vector3.forward;
                Gizmos.DrawRay(
                    transform.position + offset,
                    Vector3.down * (groundRayDistance + groundRotationRayExtraDistance)
                );
            }
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, velocityDirection);
        }
        else if (CurrentState == PlayerPhysicsState.Airborne) {
            var offset = finalRotation * Vector3.forward;
            Gizmos.DrawRay(
                transform.position + offset,
                Vector3.down * (groundRayDistance + offset.y)
            );
        }
    }
}
