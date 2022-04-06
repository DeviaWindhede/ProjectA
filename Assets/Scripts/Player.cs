using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using static HelperFunctions;

public struct PlayerInputValues
{
    public Vector2 direction;
    public bool isCharging;
    public bool isBreaking;
}

public interface IPlayerInputCallbacks
{
    void MoveCallback(Vector2 input);
    void ChargeCallback(float value);
    void BreakCallback(float value);
}

public class PlayerInputActionMapping
{
    private InputActionMap _inputActions;
    private IPlayerInputCallbacks _playerInput;
    private InputAction _actionMove;
    private InputAction _actionCharge;
    private InputAction _actionBreak;

    public PlayerInputActionMapping(InputActionMap actionMap)
    {
        this._inputActions = actionMap;
        this._actionMove = actionMap.FindAction("Move", true);
        this._actionCharge = actionMap.FindAction("Charge", true);
        this._actionBreak = actionMap.FindAction("Break", true);
    }

    public void Subscribe(IPlayerInputCallbacks input)
    {
        this.Unsubscribe();
        this._playerInput = input;

        _actionMove.Enable();
        _actionMove.performed += context => _playerInput.MoveCallback(context.ReadValue<Vector2>());
        _actionMove.canceled += context => _playerInput.MoveCallback(context.ReadValue<Vector2>());

        _actionCharge.Enable();
        _actionCharge.performed += context =>
            _playerInput.ChargeCallback(context.ReadValue<float>());
        _actionCharge.canceled += context =>
            _playerInput.ChargeCallback(context.ReadValue<float>());

        _actionBreak.Enable();
        _actionBreak.performed += context =>
            _playerInput.BreakCallback(context.ReadValue<float>());
        _actionBreak.canceled += context =>
            _playerInput.BreakCallback(context.ReadValue<float>());
    }

    public void Unsubscribe()
    {
        if (this._playerInput != null)
        {
            _actionMove.performed -= context =>
                _playerInput.MoveCallback(context.ReadValue<Vector2>());
            _actionMove.canceled -= context =>
                _playerInput.MoveCallback(context.ReadValue<Vector2>());
            _actionMove.Disable();

            _actionCharge.performed -= context =>
                _playerInput.ChargeCallback(context.ReadValue<float>());
            _actionCharge.canceled -= context =>
                _playerInput.ChargeCallback(context.ReadValue<float>());
            _actionCharge.Disable();

            _actionBreak.performed -= context =>
                _playerInput.BreakCallback(context.ReadValue<float>());
            _actionBreak.canceled -= context =>
                _playerInput.BreakCallback(context.ReadValue<float>());
            _actionBreak.Disable();

            this._playerInput = null;
        }
    }
}

[RequireComponent(typeof(PlayerController))]
public class Player : MonoBehaviour, IPlayerInputCallbacks
{
    private PlayerController playerController;
    private PlayerInputActionMapping inputHandler;
    private PlayerInputValues inputs;

    [SerializeField]
    private int playerIndex = 0;
    public int PlayerIndex
    {
        get { return this.playerIndex; }
    }

    public void SetPlayerIndex(int value)
    {
        if (value > 0)
        {
            this.playerIndex = value;
        }
    }

    [SerializeField]
    private Transform followVirtualCamera;
    public GameObject GetFollowVirtualCamera
    {
        get { return this.followVirtualCamera.gameObject; }
    }

    void Awake()
    {
        this.playerController = GetComponent<PlayerController>();
        this.SetupInputs();
    }

    private void SetupInputs()
    {
        inputs = new PlayerInputValues();
        inputs.direction = Vector2.zero;
        inputs.isCharging = false;
        InputManager inputManager = GameObject.Find("InputManager").GetComponent<InputManager>();
        PlayerInput playerInput = inputManager.GetPlayerInput(this.playerIndex);

        if (playerInput)
            this.InstantiateInputHandler(playerInput, inputManager);
        else
            inputManager.onJoin += ctx => InstantiateInputHandler(ctx, inputManager);
    }

    private void UpdateControllerInput()
    {
        this.playerController.UpdateInputs(this.inputs);
    }

    private void InstantiateInputHandler(PlayerInput playerInput, InputManager inputManager)
    {
        if (playerInput)
        {
            InputActionMap gameplayMap = playerInput.actions.actionMaps
                .ToArray()
                .First(m => m.name == InputManager.GAMEPLAY_MAPPING_NAME);
            if (gameplayMap != null)
            {
                this.inputHandler = new PlayerInputActionMapping(gameplayMap);
                this.inputHandler.Subscribe(this);
                inputManager.onJoin -= ctx => InstantiateInputHandler(ctx, inputManager);
            }
        }
    }

    public void MoveCallback(Vector2 input)
    {
        this.inputs.direction.x = input.x;
        this.inputs.direction.y = -input.y;
        this.UpdateControllerInput();
    }

    public void ChargeCallback(float value)
    {
        this.inputs.isCharging = value > 0;
        this.UpdateControllerInput();
    }

    public void BreakCallback(float value)
    {
        this.inputs.isBreaking = value > 0;
        this.UpdateControllerInput();
    }

    private void OnDestroy()
    {
        if (this.inputHandler != null)
        {
            this.inputHandler.Unsubscribe();
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
