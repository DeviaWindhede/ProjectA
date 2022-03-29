using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public interface IPlayerInput
{
  void MoveCallback(Vector2 input);
  void InteractCallback(bool value);
}

public class PlayerInputActions
{
  private InputActionMap inputActions;
  private IPlayerInput playerInput;
  private InputAction actionMove;
  private InputAction actionInteract;
  public PlayerInputActions(InputActionMap actionMap)
  {
    this.inputActions = actionMap;
    this.actionMove = actionMap.FindAction("Move", true);
    this.actionInteract = actionMap.FindAction("Interact", true);
  }

  public void Subscribe(IPlayerInput input)
  {
    this.Unsubscribe();
    this.playerInput = input;

    actionMove.Enable();
    actionMove.performed += context => playerInput.MoveCallback(context.ReadValue<Vector2>());
    actionMove.canceled += context => playerInput.MoveCallback(context.ReadValue<Vector2>());

    actionInteract.Enable();
    actionInteract.performed += context => playerInput.InteractCallback(context.ReadValue<bool>());
  }

  public void Unsubscribe()
  {
    if (this.playerInput != null) {
      actionMove.performed -= context => playerInput.MoveCallback(context.ReadValue<Vector2>());
      actionMove.canceled -= context => playerInput.MoveCallback(context.ReadValue<Vector2>());
      actionMove.Disable();

      actionInteract.performed -= context => playerInput.InteractCallback(context.ReadValue<bool>());
      actionInteract.Disable();

      this.playerInput = null;
    }
  }
}

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour, IPlayerInput
{
  private Rigidbody body;
  private PlayerInputActions inputHandler;
  private Vector2 inputDirection;
  [SerializeField] private int playerIndex = 0;
  public int PlayerIndex { get { return this.playerIndex; } }
  public void SetPlayerIndex(int value) {
    if (value > 0) {
      this.playerIndex = value;
    }
  }
  [SerializeField] private Transform mesh;
  [SerializeField] private bool useGravity = true;
  [SerializeField, Min(0f)] private float steeringMultiplier = 1f;
  [SerializeField, Range(0f, 1f)] private float turnPercentage = 1;
  [SerializeField] private float forwardSpeed = 5;
  [SerializeField] private float rotationSpeed = 5;
  [SerializeField] private float maxForwardSpeed = 50f;
  [SerializeField] private float gravityScale = 9.82f;
  [SerializeField] private float airRotationSpeed = 50f;
  [SerializeField] private bool isGrounded = false;
  [SerializeField] private float maxAngle = 45;
  [SerializeField] private float minAngle = -45;
  [SerializeField] private Transform followVirtualCamera;
  public GameObject GetFollowVirtualCamera { get { return this.followVirtualCamera.gameObject; }}

  Vector3 _forward;
  Vector3 Forward { get { return this._forward; } }
  Quaternion _horizontalRotation;
  Quaternion _verticalRotation;
  Quaternion _finalRotation = new Quaternion();
  public Quaternion Rotation { get { return this._finalRotation; } }

  // Start is called before the first frame update
  void Awake()
  {
    this.body = GetComponent<Rigidbody>();
    this.SetupInputs();

    this._horizontalRotation = Quaternion.Euler(Vector3.up * this.transform.rotation.eulerAngles.y);
    this._verticalRotation = Quaternion.Euler(this.transform.right);
    this._forward = transform.forward;
    this._finalRotation = transform.rotation;
  }

  private void SetupInputs() {
    this.inputDirection = new Vector2();
    InputManager inputManager = GameObject.Find("InputManager").GetComponent<InputManager>();
    PlayerInput playerInput = inputManager.GetPlayerInput(this.playerIndex);

    if (playerInput)
      this.InstantiateInputHandler(playerInput, inputManager);
    else
      inputManager.onJoin += ctx => InstantiateInputHandler(ctx, inputManager);
  }

  private void InstantiateInputHandler(PlayerInput playerInput, InputManager inputManager) {
    if (playerInput) {
      InputActionMap gameplayMap = playerInput.actions.actionMaps.ToArray().First(m => m.name == InputManager.GAMEPLAY_MAPPING_NAME);
      if (gameplayMap != null) {
        this.inputHandler = new PlayerInputActions(gameplayMap);
        this.inputHandler.Subscribe(this);
        inputManager.onJoin -= ctx => InstantiateInputHandler(ctx, inputManager);
      }
    }
  }


  [SerializeField] private Vector3 velocity;
  private bool groundHit;
  private RaycastHit hit;
  Vector3 lastNormal = Vector3.zero;
  Vector3 currentNormal = Vector3.zero;
  [SerializeField] private float distanceToCountAsGroundHit = 1.25f;
  private bool countAsGroundHit = false;
  void FixedUpdate()
  {
    float time = Time.fixedDeltaTime;

    float radius = GetComponent<CapsuleCollider>().radius;
    // if (Physics.SphereCast(transform.position, radius, Vector3.down, out hit, rayDist - radius, collidableLayer)) {
    // // if (Physics.Raycast(transform.position, Vector3.down, out hit, rayDist, collidableLayer)) {
    //   RaycastHit h;
    //   if (Physics.Raycast(transform.position, Vector3.down, out h, rayDist * 1.5f, collidableLayer) && currentNormal != h.normal) {
    //     lastNormal = currentNormal;
    //     currentNormal = h.normal;
    //     this.groundHit = transform.position.y - h.point.y < this.rayDist - 0.5f;
    //   }
    // }
    this.groundHit = Physics.Raycast(transform.position, Vector3.down, out hit, rayDist * 1.5f, collidableLayer);
    this.countAsGroundHit = groundHit;
    if (groundHit) {
      if (currentNormal != hit.normal) {
        lastNormal = currentNormal;
        currentNormal = hit.normal;
      }
      this.countAsGroundHit = Vector3.Distance(transform.position, hit.point) - radius < this.distanceToCountAsGroundHit;
    }

    // IsGrounded state checker, TODO: Move this to state machine
    if (isGrounded) {
      if (!groundHit) {
        isGrounded = false;
      }
      else {
        if (hit.normal != lastNormal) { // Compare new ledge angle
          float angle = Vector3.Angle(hit.normal, lastNormal);
          if (angle >= maxAngleThingy || !countAsGroundHit) { // TODO: Count as ground introduces bug when going up slope where this becomes false breifly in beginning
            isGrounded = false;
          }
        }
        else { // Compare current angle diff
          float angle = Vector3.Angle(hit.normal, mesh.forward);
          if (angle >= maxAngleThingy || !countAsGroundHit) { // TODO: Count as ground introduces bug when going up slope where this becomes false breifly in beginning
            isGrounded = false;
          }
        }
      }
    }
    else {
      groundedCooldownTimer.Time += time;
      if (countAsGroundHit && groundedCooldownTimer.Expired) {
        isGrounded = true;
        groundedCooldownTimer.Reset();
      }
    }

    if (this.isGrounded) {
      GroundedState(time);
    }
    else {
      FlyingState(time);
    }

    this.body.velocity = this.velocity * time;
  }

  public float rayDist = 1f;
  public float rayDistRotExtra = 0.75f;
  public LayerMask collidableLayer;
  private bool ShouldSlide {
    get {
      var left = Vector3.Cross(this.hit.normal, Vector3.up);
      var downhill = Vector3.Cross(this.hit.normal, left);
      var dot = Vector3.Dot(downhill, transform.forward);
      return dot >= -0.2f;
    }
  }

  public float maxAngleThingy = 45;
  private void GroundedState(float time) {
    HandleHorizontalStateRotation(time);

    // Horizontal Velocity
    float horizontalMagnitude = (this.Forward * forwardSpeed + this.velocity).magnitude;
    this.velocity = this.Forward * horizontalMagnitude;

    var vec = new Vector2(this.velocity.x, this.velocity.z);
    if (vec.magnitude > maxForwardSpeed)
    {
      vec.Normalize();
      this.velocity.x = vec.x * maxForwardSpeed;
      this.velocity.z = vec.y * maxForwardSpeed;
    }

    if (useGravity && !countAsGroundHit)
    {
      Vector3 gravity = Vector3.down * this.gravityScale * time;
      this.body.AddForce(gravity);
      // this.velocity.y = this.body.velocity.y == 0 ? 0 : this.body.velocity.y + this.velocity.y;
    }

    if (isHolding)
    {
      this.velocity = Vector3.zero;
    }
  }

  private Timer groundedCooldownTimer = new Timer(0.2f);
  private void FlyingState(float time) {

    #region Comments
    // Vector3 forwardForce = this.transform.forward * forwardSpeed;
    // this.velocity += forwardForce;


    // if (!isGrounded) {
    //     float deltaAngle = -(this.body.rotation.eulerAngles.x + (this.body.rotation.eulerAngles.x - 180 > 0 ? -1 : 0) * 360);
    //     print("d, " + deltaAngle + ", b " + this.body.rotation.eulerAngles.x);
    //     if (deltaAngle > maxAngle) {
    //         print("too much!");
    //     }
    //     else if (deltaAngle < minAngle) {
    //         print("too little!");
    //     }
    //     else {
    //         Vector3 upForce = Vector3.right * this.inputDirection.y * airRotationSpeed;
    //         Quaternion deltaAirRotation = Quaternion.Euler(upForce * time);
    //         this.body.MoveRotation(this.body.rotation * deltaAirRotation);
    //     }
    // }

    // this.body.velocity = this.velocity * time;

    // var vec = new Vector2(this.velocity.x, this.velocity.z);
    // if (vec.magnitude > maxForwardSpeed) {
    //     vec.Normalize();
    //     this.velocity.x = vec.x * maxForwardSpeed;
    //     this.velocity.z = vec.y * maxForwardSpeed;
    // }

    // Vector3 deltaY = Quaternion.Euler(Vector3.right).eulerAngles * this.inputDirection.y * this.steeringMultiplier;
    // float angle = (rotation.eulerAngles + deltaY).x % 360;
    // // float angle = (verticalRotation.eulerAngles + deltaY).x % 360;
    // // if (!(angle >= 90 && angle <= 270)) verticalRotation.eulerAngles = verticalRotation.eulerAngles + deltaY;
    // if (angle >= 90 && angle <= 270) deltaY = Vector3.zero;
    // // finalDirection = transform.rotation * verticalRotation * Vector3.forward;


    // this.body.rotation = Quaternion.Euler(eulers) * verticalRotation;
    // Vertical Velocity
    // if (!isGrounded)
    // {
    //   float verticalMagnitude = (this.transform.forward * forwardSpeed + this.velocity).magnitude;
    //   this.velocity.y += this.finalDirection.y * verticalMagnitude;
    // }

    // Vector3 forwardForce = Vector3.Lerp(this.velocity, this.finalDirection, turnPercentage);
    // forwardForce.Normalize();
    // this.velocity += this.finalDirection * forwardSpeed;
    // this.velocity = this.finalDirection * this.velocity.magnitude;
    // this.velocity = Vector3.RotateTowards(
    //   this.velocity.normalized,
    //   forwardForce.normalized,
    //   Mathf.PI / 2 * this.turnPercentage,
    //   0f).normalized * this.velocity.magnitude;
    #endregion

    HandleVerticalStateRotation(time);

    // Horizontal Velocity
    float horizontalMagnitude = (this.Forward * forwardSpeed + this.velocity).magnitude;
    this.velocity = this.Forward * horizontalMagnitude;

    var vec = new Vector2(this.velocity.x, this.velocity.z);
    if (vec.magnitude > maxForwardSpeed)
    {
      vec.Normalize();
      this.velocity.x = vec.x * maxForwardSpeed;
      this.velocity.z = vec.y * maxForwardSpeed;
    }

    if (useGravity && !countAsGroundHit)
    {
      Vector3 gravity = Vector3.down * this.gravityScale * time;
      this.body.AddForce(gravity);
      // this.velocity.y = this.body.velocity.y == 0 ? 0 : this.body.velocity.y + this.velocity.y;
    }

    if (isHolding)
    {
      this.velocity = Vector3.zero;
    }
  }

  private void HandleHorizontalStateRotation(float time) {
    // Horizontal Rotation
    Quaternion horizontalDelta = Quaternion.Euler(Vector3.up * this.inputDirection.x * rotationSpeed * time);
    this._horizontalRotation = this._horizontalRotation * horizontalDelta;

    Vector3 averageNormal = hit.normal;
    int count = 5;
    float angleIncrement = 360f / count;
    int incrementCount = 1;
    for (int i = 0; i < count; i++) {
      RaycastHit rayData;
      var offset = this.Rotation * Quaternion.Euler(0, -angleIncrement * i, 0) * Vector3.forward;
      if (Physics.Raycast(transform.position + offset, Vector3.down, out rayData, rayDist + rayDistRotExtra, collidableLayer)) {
        averageNormal += rayData.normal;
        incrementCount++;
      }
    }
    averageNormal /= incrementCount;

    Vector3 upVec = this.groundHit ? averageNormal : Vector3.up;
    this._finalRotation = Quaternion.FromToRotation(Vector3.up, upVec) * _horizontalRotation;
    this._forward = this._finalRotation * Vector3.forward;
    this.mesh.rotation = this._finalRotation;

    this._verticalRotation = Quaternion.Euler(this.mesh.transform.right);
  }

  private void HandleVerticalStateRotation(float time) {
    // Horizontal Rotation
    Quaternion horizontalDelta = Quaternion.Euler(Vector3.up * this.inputDirection.x * rotationSpeed * time);
    this._horizontalRotation = this._horizontalRotation * horizontalDelta;

    // Vertical Direction Handling (This would probably be easier with proper Quaternion calculations)
    Vector3 verticalDeltaEuler = Quaternion.Euler(Vector3.right).eulerAngles * this.inputDirection.y * this.rotationSpeed * time;

    float verticalAngle = (this._verticalRotation.eulerAngles + verticalDeltaEuler).x % 360;
    if (verticalAngle < 45 || verticalAngle > 315) // TODO: Add variables for this
      this._verticalRotation.eulerAngles = this._verticalRotation.eulerAngles + verticalDeltaEuler;

    Vector3 rotationDirection = this._horizontalRotation * _verticalRotation * Vector3.forward;

    this._finalRotation = Quaternion.LookRotation(rotationDirection);
    this._forward = this.Rotation * Vector3.forward;
    this.mesh.rotation = this.Rotation;
  }

  public void MoveCallback(Vector2 input)
  {
    this.inputDirection.x = input.x;
    this.inputDirection.y = -input.y;
  }

  private bool isHolding = false;
  public void InteractCallback(bool value)
  {
    this.isHolding = value;
    Debug.Log("Interact!");
  }

  private void OnDestroy() {
    if (this.inputHandler != null) {
      this.inputHandler.Unsubscribe();
    }
  }

  private void OnDrawGizmos() {
    Gizmos.color = Color.red;
    Gizmos.DrawRay(transform.position, Vector3.down * rayDist);

    Gizmos.color = Color.blue;
    // var offset = this.Rotation * Vector3.forward;
    // Gizmos.DrawRay(transform.position + offset, Vector3.down * rayDist);

    int count = 5;
    float angleIncrement = 360f / count;
    for (int i = 0; i < count; i++) {
      var offset = this.Rotation * Quaternion.Euler(0, -angleIncrement * i, 0) * Vector3.forward;
      Gizmos.DrawRay(transform.position + offset, Vector3.down * (rayDist + rayDistRotExtra));
    }
  }
}




// WIP
/*

  // Update is called once per frame
  void FixedUpdate()
  {
    float time = Time.fixedDeltaTime;

    // Vector3 forwardForce = this.transform.forward * forwardSpeed;
    // this.velocity += forwardForce;


    // if (!isGrounded) {
    //     float deltaAngle = -(this.body.rotation.eulerAngles.x + (this.body.rotation.eulerAngles.x - 180 > 0 ? -1 : 0) * 360);
    //     print("d, " + deltaAngle + ", b " + this.body.rotation.eulerAngles.x);
    //     if (deltaAngle > maxAngle) {
    //         print("too much!");
    //     }
    //     else if (deltaAngle < minAngle) {
    //         print("too little!");
    //     }
    //     else {
    //         Vector3 upForce = Vector3.right * this.inputDirection.y * airRotationSpeed;
    //         Quaternion deltaAirRotation = Quaternion.Euler(upForce * time);
    //         this.body.MoveRotation(this.body.rotation * deltaAirRotation);
    //     }
    // }

    // this.body.velocity = this.velocity * time;

    // var vec = new Vector2(this.velocity.x, this.velocity.z);
    // if (vec.magnitude > maxForwardSpeed) {
    //     vec.Normalize();
    //     this.velocity.x = vec.x * maxForwardSpeed;
    //     this.velocity.z = vec.y * maxForwardSpeed;
    // }

    // Rotation
    Vector3 deltaY = Quaternion.Euler(Vector3.right).eulerAngles * this.inputDirection.y * this.steeringMultiplier;
    float angle = (verticalRotation.eulerAngles + deltaY).x % 360;
    if (!(angle >= 90 && angle <= 270)) verticalRotation.eulerAngles = verticalRotation.eulerAngles + deltaY;
    finalDirection = Quaternion.Euler(transform.rotation.x, 0, transform.rotation.z) * verticalRotation * Vector3.forward;

    float inputSign = this.inputDirection.y == 0 ? 0 : Mathf.Sign((1 + this.inputDirection.y) / 2);
    float verticleAngle = finalDirection.y * -90f;
    Vector3 upForce = Vector3.right * verticleAngle * inputSign;
    Vector3 sideForce = Vector3.up * this.inputDirection.x * rotationSpeed;
    Vector3 totalForce = sideForce + upForce;
    Quaternion deltaRotation = Quaternion.Euler(totalForce * time);

    this.body.MoveRotation(this.body.rotation * deltaRotation);

    // this.body.MoveRotation(Quaternion.Euler(finalDirection.y * -90f, 0, 0));

    // Horizontal Velocity
    float horizontalMagnitude = (this.transform.forward * forwardSpeed + this.velocity).magnitude;
    this.velocity.x = this.transform.forward.x * horizontalMagnitude;
    this.velocity.z = this.transform.forward.z * horizontalMagnitude;

    var vec = new Vector2(this.velocity.x, this.velocity.z);
    if (vec.magnitude > maxForwardSpeed)
    {
      vec.Normalize();
      this.velocity.x = vec.x * maxForwardSpeed;
      this.velocity.z = vec.y * maxForwardSpeed;
    }

    if (useGravity)
    {
      Vector3 gravity = Vector3.down * this.gravityScale * time;
      this.body.AddForce(gravity);
      this.velocity.y = this.body.velocity.y == 0 ? 0 : this.body.velocity.y + this.velocity.y;
    }

    if (isHolding)
    {
      this.velocity = Vector3.zero;
    }


    this.body.velocity = this.velocity * time;



    
    // Vertical Velocity
    // if (!isGrounded)
    // {
    //   float verticalMagnitude = (this.transform.forward * forwardSpeed + this.velocity).magnitude;
    //   this.velocity.y += this.finalDirection.y * verticalMagnitude;
    // }
  
    // Vector3 forwardForce = Vector3.Lerp(this.velocity, this.finalDirection, turnPercentage);
    // forwardForce.Normalize();
    // this.velocity += this.finalDirection * forwardSpeed;
    // this.velocity = this.finalDirection * this.velocity.magnitude;
    // this.velocity = Vector3.RotateTowards(
    //   this.velocity.normalized,
    //   forwardForce.normalized,
    //   Mathf.PI / 2 * this.turnPercentage,
    //   0f).normalized * this.velocity.magnitude;
  }



*/