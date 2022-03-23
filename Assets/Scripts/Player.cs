using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IPlayerInput
{
  void MoveCallback(Vector2 input);
  void InteractCallback(bool value);
}

public class InputHandler
{
  private InputActions inputActions;

  public void Subscribe(IPlayerInput input)
  {
    this.inputActions = new InputActions();
    this.inputActions.Gameplay.Move.Enable();
    this.inputActions.Gameplay.Move.performed += context => input.MoveCallback(context.ReadValue<Vector2>());
    this.inputActions.Gameplay.Move.canceled += context => input.MoveCallback(context.ReadValue<Vector2>());

    this.inputActions.Gameplay.Interact.Enable();
    this.inputActions.Gameplay.Interact.performed += context => input.InteractCallback(context.ReadValue<bool>());
    this.inputActions.Gameplay.Interact.performed += context => input.InteractCallback(context.ReadValue<bool>());
  }

  public void Unsubscribe()
  {
    this.inputActions.Gameplay.Move.Disable();
    this.inputActions.Gameplay.Interact.Disable();
    this.inputActions = null;
  }
}

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour, IPlayerInput
{
  private Rigidbody body;
  private InputHandler inputHandler;
  private Vector2 inputDirection;

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

  Vector3 _forward;
  Vector3 Forward { get { return this._forward; } }
  Quaternion _horizontalRotation;
  Quaternion _verticalRotation;
  Quaternion _finalRotation = new Quaternion();
  public Quaternion Rotation { get { return this._finalRotation; } }

  // Start is called before the first frame update
  void Awake()
  {
    this.body = gameObject.GetComponent<Rigidbody>();
    this.inputDirection = new Vector2();
    this.inputHandler = new InputHandler();

    this.inputHandler.Subscribe(this);

    this._verticalRotation = new Quaternion();
    this._horizontalRotation = Quaternion.Euler(0, this.body.rotation.y, 0);
    this._forward = transform.forward;

    PlayerInput i = GetComponent<PlayerInput>();
    // i.actionEvents[0].
  }


  [SerializeField] private Vector3 velocity;

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

    HandleRotation(time);

    // Horizontal Velocity
    float horizontalMagnitude = (this.Forward * forwardSpeed + this.velocity).magnitude;
    this.velocity = this.Forward * horizontalMagnitude;
    // this.velocity.x = this.Forward.x * horizontalMagnitude;
    // this.velocity.z = this.Forward.z * horizontalMagnitude;

    var vec = new Vector2(this.velocity.x, this.velocity.z);
    if (vec.magnitude > maxForwardSpeed)
    {
      vec.Normalize();
      this.velocity.x = vec.x * maxForwardSpeed;
      this.velocity.z = vec.y * maxForwardSpeed;
    }

    if (useGravity && !isGrounded)
    {
      Vector3 gravity = Vector3.down * this.gravityScale * time;
      this.body.AddForce(gravity);
      // this.velocity.y = this.body.velocity.y == 0 ? 0 : this.body.velocity.y + this.velocity.y;
    }

    if (isHolding)
    {
      this.velocity = Vector3.zero;
    }
    this.body.velocity = this.velocity * time;
  }

  private void HandleRotation(float time) {
    // Horizontal Rotation
    Quaternion horizontalDelta = Quaternion.Euler(Vector3.up * this.inputDirection.x * rotationSpeed * time);
    this._horizontalRotation = this._horizontalRotation * horizontalDelta;

    // Verticle Direction Handling (This would probably be easier with proper Quaternion calculations)
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
    this.inputHandler.Unsubscribe();
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