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
