using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody body;

    [SerializeField]
    private Transform mesh;

    [SerializeField]
    private bool useGravity = true;

    [SerializeField, Min(0f)]
    private float steeringMultiplier = 1f;

    [SerializeField, Range(0f, 1f)]
    private float turnPercentage = 1;

    [SerializeField]
    private float timeToReachFullSpeed = 5;

    [SerializeField]
    private float lookRotationDegsPerSecond = 5;

    [SerializeField, Min(0)]
    private float rotationSpeed = 70;
    [Min(0.01f)] public float slippyScalar = 3;

    [SerializeField]
    private float maxForwardSpeed = 50f;

    [SerializeField]
    private float gravityScale = 9.82f;

    [SerializeField]
    private float airRotationSpeed = 50f;

    // [SerializeField]
    // private bool isGrounded = false;

    [SerializeField]
    private float maxAngle = 45;

    [SerializeField]
    private float minAngle = -45;
    Vector3 _forward;
    private Vector3 Forward { get { return this._finalRotation * Vector3.forward; } }
    Quaternion _horizontalRotation;
    Quaternion _verticalRotation;
    Quaternion _finalRotation = new Quaternion();

    [SerializeField]
    private Vector3 velocityDirection;
    private bool groundHit;
    private RaycastHit hit;
    Vector3 lastNormal = Vector3.zero;
    Vector3 currentNormal = Vector3.zero;

    [SerializeField, Min(0)]
    private float distanceFromColliderToCountAsGroundHit = 1.25f;
    private bool countAsGroundHit = false;

    private enum PlayerPhysicsState
    {
        Grounded,
        Airborne,
    }
    private PlayerPhysicsState CurrentState {
        get {
            return this._currentState;
        }
        set {
            if (value != this._currentState) {
                this._currentState = value;
                switch (value) {
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

    private PlayerPhysicsState _currentState = PlayerPhysicsState.Airborne;
    private bool isHolding = false;
    private new CapsuleCollider collider;
    private PlayerInputValues inputs;
    public float rayDist = 1f;
    public float rayDistRotExtra = 0.75f;
    public LayerMask collidableLayer;
    [SerializeField, Min(0)] float maxSlopeAngle = 45;
    private Timer groundedCooldownTimer = new Timer(0.2f);
    private float speed;
    [Min(0)] public float timeToRideInTurnedDirection = 0.3f;

    #region Helpers
    private bool ShouldSlide
    {
        get
        {
            var left = Vector3.Cross(this.hit.normal, Vector3.up);
            var downhill = Vector3.Cross(this.hit.normal, left);
            var dot = Vector3.Dot(downhill, transform.forward);
            return dot >= -0.2f;
        }
    }

    private bool IsSurfaceClimbable(Vector3 vec1, Vector3 vec2) {
        if (groundHit) {
            float angle = Vector3.Angle(vec1, vec2);
            return angle < maxSlopeAngle;
        }
        return false;
    }

    private float GetNegativeAngle(Vector3 vectorA, Vector3 vectorB) {
        float angle = Vector3.Angle(vectorA, vectorB);
        Vector3 cross = Vector3.Cross(vectorA, vectorB);
        if (cross.y < 0) angle = -angle;
        return angle;
    }
    #endregion

    // Start is called before the first frame update
    void Awake()
    {
        this.body = GetComponent<Rigidbody>();
        this.collider = GetComponent<CapsuleCollider>();

        this._horizontalRotation = Quaternion.Euler(
            Vector3.up * this.transform.rotation.eulerAngles.y
        );
        this.transform.rotation = Quaternion.identity;
        this._verticalRotation = Quaternion.Euler(this.transform.right);
        this._forward = transform.forward;
        this._finalRotation = transform.rotation;
    }

    private void OnGroundedEnter() {
        this.velocityDirection.y = 0;
    }

    private void OnAirborneEnter() { }

    public void UpdateInputs(PlayerInputValues inputs)
    {
        this.inputs = inputs;
    }

    void FixedUpdate()
    {
        float time = Time.fixedDeltaTime;

        Vector3 groundHitOffset = Vector3.zero;
        if (CurrentState == PlayerPhysicsState.Airborne) {
            groundHitOffset = this._finalRotation * Vector3.forward;
        }
        this.groundHit = Physics.Raycast(
            transform.position + groundHitOffset,
            Vector3.down,
            out hit,
            rayDist + groundHitOffset.y,
            collidableLayer
        );

        this.countAsGroundHit = groundHit;
        if (groundHit)
        {
            if (currentNormal != hit.normal)
            {
                lastNormal = currentNormal;
                currentNormal = hit.normal;
            }
            var offset = this._finalRotation * Vector3.forward;
            this.countAsGroundHit =
                Vector3.Distance(transform.position + offset, hit.point) - collider.height / 2
                < this.distanceFromColliderToCountAsGroundHit;
        }

        HandlePhysicsStateTransitions(time);

        switch (this.CurrentState) {
            case PlayerPhysicsState.Grounded:
                GroundedState(time);
                break;
            case PlayerPhysicsState.Airborne:
                FlyingState(time);
                break;
        }
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
                if (lastNormal != currentNormal) { // currentNormal will always have a value when groundHit is truthy
                    // entering here means the player is trying to move from a surface to a new one or was flying and has now hit a surface

                    Vector3 vec = lastNormal == Vector3.zero ? Vector3.up : lastNormal;  // If first time touching ground
                    if (!IsSurfaceClimbable(hit.normal, vec)) {
                        CurrentState = PlayerPhysicsState.Airborne;
                    }
                }
            }
            else if (CurrentState == PlayerPhysicsState.Airborne) {
                lastNormal = Vector3.zero;
                currentNormal = Vector3.zero;
                groundedCooldownTimer.Time += time; // Cooldown until player is able to touch ground again
                if (groundedCooldownTimer.Expired)
                {
                    if (countAsGroundHit && IsSurfaceClimbable(hit.normal, Vector3.up)) {
                        CurrentState = PlayerPhysicsState.Grounded;
                        groundedCooldownTimer.Reset();
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
            Vector3.up * 45 / (0.01f + rotationSpeed) * Mathf.Sign(GetNegativeAngle(velocityDirection, horizontalDirection))
        );
        velocityDirection += _horizontalRotation * rotationExtra * Vector3.forward * rotationSpeed * time;
        if (velocityDirection.magnitude > slippyScalar) {
            velocityDirection = velocityDirection.normalized * slippyScalar;
        }

        float dot = Vector3.Dot(horizontalDirection.normalized, velocityDirection.normalized);
        if (dot >= 1 - 0.0001f && rotationSpeed > 0) {
            velocityDirection = horizontalDirection.normalized;
        }

        // Speed acceleration
        speed += time / timeToReachFullSpeed * maxForwardSpeed;
        if (speed > maxForwardSpeed) {
            speed = maxForwardSpeed;
        }

        if (useGravity && !countAsGroundHit)
        {
            // Vector3 gravity = Vector3.down * this.gravityScale * time;
            // this.body.AddForce(gravity);
            // this.velocity.y = this.body.velocity.y == 0 ? 0 : this.body.velocity.y + this.velocity.y;
        }

        // if (isHolding)
        // {
        //     this.velocity = Vector3.zero;
        // }
        this.body.velocity = _verticalRotation * this.velocityDirection.normalized * speed * time;
    }

    private void FlyingState(float time)
    {
        HandleVerticalStateRotation(time);

        // Horizontal Velocity
        float horizontalMagnitude = (this._forward * timeToReachFullSpeed + this.velocityDirection).magnitude;
        this.velocityDirection = this._forward * horizontalMagnitude;

        var vec = new Vector2(this.velocityDirection.x, this.velocityDirection.z);
        if (vec.magnitude > maxForwardSpeed)
        {
            vec.Normalize();
            this.velocityDirection.x = vec.x * maxForwardSpeed;
            this.velocityDirection.z = vec.y * maxForwardSpeed;
        }

        if (useGravity && !countAsGroundHit)
        {
            Vector3 gravity = Vector3.down * this.gravityScale * time;
            this.body.AddForce(gravity);
        }

        if (isHolding)
        {
            this.velocityDirection = Vector3.zero;
        }
        this.body.velocity = this.velocityDirection.normalized * speed * time;
    }

    private void HandleHorizontalStateRotation(float time)
    {
        // Horizontal Rotation
        Quaternion horizontalDelta = Quaternion.Euler(
            Vector3.up * lookRotationDegsPerSecond * time * inputs.direction.x
        );
        this._horizontalRotation = this._horizontalRotation * horizontalDelta;

        Vector3 averageNormal = hit.normal;
        int count = 5;
        float angleIncrement = 360f / count;
        int incrementCount = 1;
        for (int i = 0; i < count; i++)
        {
            RaycastHit rayData;
            var offset =
                this._finalRotation * Quaternion.Euler(0, -angleIncrement * i, 0) * Vector3.forward;
            if (Physics.Raycast(
                    transform.position + offset,
                    Vector3.down,
                    out rayData,
                    rayDist + rayDistRotExtra,
                    collidableLayer
                )
            )
            {
                Vector3 vec = currentNormal == Vector3.zero ? Vector3.up : currentNormal;  // If first time touching ground
                if (IsSurfaceClimbable(rayData.normal, vec)) {
                    averageNormal += rayData.normal;
                    incrementCount++;
                }
            }
        }
        averageNormal /= incrementCount;

        Vector3 upVec = this.groundHit ? averageNormal : Vector3.up;
        this._verticalRotation = Quaternion.FromToRotation(Vector3.up, upVec);
        this._finalRotation = _verticalRotation * _horizontalRotation;
        this._forward = this._finalRotation * Vector3.forward;
        this.mesh.rotation = this._finalRotation;

        // this._verticalRotation = Quaternion.Euler(this.mesh.transform.right);
    }

    private void HandleVerticalStateRotation(float time)
    {
        // Horizontal Rotation
        Quaternion horizontalDelta = Quaternion.Euler(
            Vector3.up * inputs.direction.x * lookRotationDegsPerSecond * time
        );
        this._horizontalRotation = this._horizontalRotation * horizontalDelta;

        // Vertical Direction Handling (This would probably be easier with proper Quaternion calculations)
        Vector3 verticalDeltaEuler =
            Quaternion.Euler(Vector3.right).eulerAngles
            * inputs.direction.y
            * this.lookRotationDegsPerSecond
            * time;

        float verticalAngle = (this._verticalRotation.eulerAngles + verticalDeltaEuler).x % 360;
        if (verticalAngle < 45 || verticalAngle > 315) // TODO: Add variables for this
            this._verticalRotation.eulerAngles =
                this._verticalRotation.eulerAngles + verticalDeltaEuler;

        Vector3 rotationDirection = this._horizontalRotation * _verticalRotation * Vector3.forward;

        this._finalRotation = Quaternion.LookRotation(rotationDirection);
        this._forward = this._finalRotation * Vector3.forward;
        this.mesh.rotation = this._finalRotation;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (CurrentState == PlayerPhysicsState.Grounded) {
            Gizmos.DrawRay(transform.position, Vector3.down * rayDist);

            Gizmos.color = Color.blue;
            int count = 5;
            float angleIncrement = 360f / count;
            for (int i = 0; i < count; i++)
            {
                var offset =
                    this._finalRotation * Quaternion.Euler(0, -angleIncrement * i, 0) * Vector3.forward;
                Gizmos.DrawRay(transform.position + offset, Vector3.down * (rayDist + rayDistRotExtra));
            }
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, velocityDirection);
        }
        else if (CurrentState == PlayerPhysicsState.Airborne) {
            var offset = this._finalRotation * Vector3.forward;
            Gizmos.DrawRay(transform.position + offset, Vector3.down * (rayDist + offset.y));
        }
    }
}
