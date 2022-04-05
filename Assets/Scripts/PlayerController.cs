using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody _body;

    [SerializeField]
    private Transform _mesh;

    [SerializeField]
    private bool _useGravity = true;

    [Header("Velocity")]
    [SerializeField]
    private float _secondsToReachFullGroundSpeed = 5;
    [SerializeField]
    private float _secondsToReachFullAirSpeed = 0.3f;
    [SerializeField]
    private float _maxForwardGroundSpeed = 300f;
    [SerializeField]
    private float _maxForwardAirSpeed = 1000f;
    [SerializeField]
    private float _gravityScale = 9.82f;
    private float _gravitySpeed;

    [Header("Rotation")]
    [SerializeField]
    private float _lookRotationDegsPerSecond = 5;
    [SerializeField, Min(0)]
    private float _timeToRideInTurnedDirection = 0.3f;

    [SerializeField, Min(0)]
    private float _rotationSpeed = 70;

    [SerializeField, Min(0.01f)]
    private float _slippinessScale = 3;

    [SerializeField]
    private float _airRotationSpeed = 50f;

    [SerializeField]
    private float _maxAirborneAngle = 135;

    [SerializeField]
    private float _minAirborneAngle = -135;

    [SerializeField]
    private float followGroundRotationAnglePerSecond = 15f;

    [SerializeField]
    private int rotationRayCount = 10;

    [Header("Ground Check")]
    [SerializeField] private float _groundSphereOffset = 0.5f;
    [SerializeField] private float _groundSphereExtraRadius = -0.1f;
    [SerializeField, Min(0)]
    private float distanceFromColliderToCountAsGroundHit = 1.25f;

    [SerializeField, Min(0)]
    private float _maxClimbableSlopeAngle = 45;
    [SerializeField] private float _groundRayDistance = 1f;
    [SerializeField] private float _groundRotationRayExtraDistance = 0.75f;
    [SerializeField] private LayerMask _collidableLayer;


    // Input
    private PlayerInputValues _inputs;

    // Component
    private CapsuleCollider _collider;
    private bool _groundHit;
    private bool _countAsGroundHit;
    private RaycastHit _groundHitInfo;
    private Vector3 _lastNormal = Vector3.zero;
    private Vector3 _currentNormal = Vector3.zero;

    // States
    private PlayerPhysicsState _currentState = PlayerPhysicsState.Airborne;
    private Timer _groundedCooldownTimer = new Timer(0.2f);

    // Velocity
    private float _speed;
    private Vector3 _velocityDirection;

    // Rotation
    private Vector3 _forward;
    private Vector3 Forward
    {
        get { return _finalRotation * Vector3.forward; }
    }
    private Quaternion _horizontalRotation;
    private Quaternion _verticalRotation;
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

    private void log(params System.Object[] arguments)
    {
        string finalString = string.Empty;
        for (int i = 0; i < arguments.Length; i++)
        {
            finalString += arguments[i];
            if (i != arguments.Length - 1)
                finalString += " , ";
        }
        Debug.Log(finalString);
    }
    #endregion

    // Start is called before the first frame update
    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();

        _horizontalRotation = Quaternion.Euler(
            Vector3.up * this.transform.rotation.eulerAngles.y
        );
        this.transform.rotation = Quaternion.identity;
        _verticalRotation = Quaternion.Euler(this.transform.right);
        _forward = transform.forward;
        _finalRotation = transform.rotation;
    }

    private void OnGroundedEnter()
    {
        _velocityDirection.y = 0;
    }

    private void OnAirborneEnter()
    {
        var euler = _mesh.rotation.eulerAngles;
        _verticalRotation = Quaternion.Euler(euler.x, 0, euler.z);
    }

    public void UpdateInputs(PlayerInputValues inputs)
    {
        _inputs = inputs;
    }

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

        _countAsGroundHit = _groundHit;
        if (_groundHit)
        {
            if (_currentNormal != _groundHitInfo.normal)
            {
                _lastNormal = _currentNormal;
                _currentNormal = _groundHitInfo.normal;
            }
            var offset = _finalRotation * Vector3.forward;
            _countAsGroundHit =
                Vector3.Distance(transform.position + offset, _groundHitInfo.point) - _collider.height / 2
                < this.distanceFromColliderToCountAsGroundHit;
        }

        HandlePhysicsStateTransitions(time);

        switch (this.CurrentState)
        {
            case PlayerPhysicsState.Grounded:
                GroundedState(time);
                break;
            case PlayerPhysicsState.Airborne:
                FlyingState(time);
                break;
        }
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
                if (_lastNormal != _currentNormal)
                { // currentNormal will always have a value when groundHit is truthy
                    // entering here means the player is trying to move from a surface to a new one or was flying and has now hit a surface
                    Vector3 vec = _lastNormal == Vector3.zero ? Vector3.up : _lastNormal; // If first time touching ground
                    if (!IsSurfaceClimbable(_groundHitInfo.normal, vec))
                    {
                        CurrentState = PlayerPhysicsState.Airborne;
                    }
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
        HandleHorizontalStateRotation(time);

        // Horizontal Velocity
        Vector3 horizontalDirection = _horizontalRotation * Vector3.forward;
        Quaternion rotationExtra = Quaternion.Euler(
            Vector3.up
                * 45
                / (0.01f + _rotationSpeed)
                * Mathf.Sign(GetNegativeAngle(_velocityDirection, horizontalDirection))
        );
        _velocityDirection +=
            _horizontalRotation * rotationExtra * Vector3.forward * _rotationSpeed * time;
        if (_velocityDirection.magnitude > _slippinessScale)
        {
            _velocityDirection = _velocityDirection.normalized * _slippinessScale;
        }

        float dot = Vector3.Dot(horizontalDirection.normalized, _velocityDirection.normalized);
        if (dot >= 1 - 0.0001f && _rotationSpeed > 0)
        {
            _velocityDirection = horizontalDirection.normalized;
        }

        // Speed acceleration
        _speed += time / _secondsToReachFullSpeed * _maxForwardSpeed;
        if (_speed > _maxForwardSpeed)
        {
            _speed = _maxForwardSpeed;
        }

        if (_useGravity && !_countAsGroundHit)
        {
            // Vector3 gravity = Vector3.down * this.gravityScale * time;
            // this.body.AddForce(gravity);
            // this.velocity.y = this.body.velocity.y == 0 ? 0 : this.body.velocity.y + this.velocity.y;
        }

        // if (isHolding)
        // {
        //     this.velocity = Vector3.zero;
        // }
        _body.velocity = _verticalRotation * _velocityDirection.normalized * _speed * time;
    }

    private void FlyingState(float time)
    {
        HandleVerticalStateRotation(time);

        // Horizontal Velocity
        float horizontalMagnitude =
            (_forward * _secondsToReachFullSpeed + _velocityDirection).magnitude;
        _velocityDirection = _forward * horizontalMagnitude;

        var vec = new Vector2(_velocityDirection.x, _velocityDirection.z);
        if (vec.magnitude > _maxForwardSpeed)
        {
            vec.Normalize();
            _velocityDirection.x = vec.x * _maxForwardSpeed;
            _velocityDirection.z = vec.y * _maxForwardSpeed;
        }

        if (_useGravity && !_countAsGroundHit)
        {
            Vector3 gravity = Vector3.down * _gravityScale * time;
            _body.AddForce(gravity);
        }

        // Speed acceleration
        _speed += time / _secondsToReachFullSpeed * _maxForwardSpeed;
        if (_speed > _maxForwardSpeed)
        {
            _speed = _maxForwardSpeed;
        }

        if (_inputs.isInteracting)
        {
            _velocityDirection = Vector3.zero;
        }
        _body.velocity = _velocityDirection.normalized * _speed * time;
    }

    private void HandleHorizontalStateRotation(float time)
    {
        // Horizontal Rotation
        Quaternion horizontalDelta = Quaternion.Euler(
            Vector3.up * _lookRotationDegsPerSecond * time * _inputs.direction.x
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
        _verticalRotation = Quaternion.RotateTowards(_verticalRotation, Quaternion.FromToRotation(Vector3.up, upVec), followGroundRotationAnglePerSecond * time);
        _finalRotation = _verticalRotation * _horizontalRotation;
        _forward = _finalRotation * Vector3.forward;
    }

    private void HandleVerticalStateRotation(float time)
    {
        // Horizontal Rotation
        Quaternion horizontalDelta = Quaternion.Euler(
            Vector3.up * _inputs.direction.x * _lookRotationDegsPerSecond * time
        );
        _horizontalRotation = _horizontalRotation * horizontalDelta;

        // Vertical Direction Handling (This would probably be easier with proper Quaternion calculations)
        Vector3 verticalDeltaEuler =
            Quaternion.Euler(Vector3.right).eulerAngles
            * _inputs.direction.y
            * _lookRotationDegsPerSecond
            * time;
        float verticalAngle = (_verticalRotation.eulerAngles + verticalDeltaEuler).x % 360;
        if (verticalAngle < _minAirborneAngle + 180 || verticalAngle > _maxAirborneAngle + 180)
            _verticalRotation.eulerAngles =
                _verticalRotation.eulerAngles + verticalDeltaEuler;

        Vector3 rotationDirection = _horizontalRotation * _verticalRotation * Vector3.forward;

        _finalRotation = Quaternion.LookRotation(rotationDirection);
        _forward = _finalRotation * Vector3.forward;

    private void OnValidate() {
        _body = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * (_groundSphereOffset + _collider.height / 2), _collider.radius + _groundSphereExtraRadius);

        if (CurrentState == PlayerPhysicsState.Grounded)
        {
            Gizmos.DrawRay(transform.position, Vector3.down * _groundRayDistance);

            Gizmos.color = Color.blue;
            float angleIncrement = 360f / rotationRayCount;
            for (int i = 0; i < rotationRayCount; i++)
            {
                var offset =
                    _finalRotation
                    * Quaternion.Euler(0, -angleIncrement * i, 0)
                    * Vector3.forward;
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
            Gizmos.DrawRay(transform.position + offset, Vector3.down * (_groundRayDistance + offset.y));
        }
        Gizmos.color = Color.black;
        Gizmos.DrawRay(transform.position, _mesh.transform.forward * 5);
    }
}