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

[RequireComponent(typeof(PlayerController))]
public class Player : MonoBehaviour, IPlayerInputCallbacks
{
    [SerializeField]
    private int _playerIndex = 0;

    [SerializeField]
    private Transform _followVirtualCamera;

    private PlayerController _playerController;
    private PlayerInputActionMapping _inputHandler;
    private PlayerInputValues _inputs;

    public int PlayerIndex
    {
        get { return _playerIndex; }
    }

    public GameObject GetFollowVirtualCamera
    {
        get { return _followVirtualCamera.gameObject; }
    }

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        SetupInputs();
    }

    private void SetupInputs()
    {
        _inputs = new PlayerInputValues();
        _inputs.direction = Vector2.zero;
        _inputs.isCharging = false;
        InputManager inputManager = GameObject.Find("InputManager").GetComponent<InputManager>();
        PlayerInput playerInput = inputManager.GetPlayerInput(_playerIndex);

        if (playerInput)
            this.InstantiateInputHandler(playerInput, inputManager);
        else
            inputManager.onJoin += ctx => InstantiateInputHandler(ctx, inputManager);
    }

    private void UpdateControllerInput()
    {
        _playerController.UpdateInputs(_inputs);
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
                _inputHandler = new PlayerInputActionMapping(gameplayMap);
                _inputHandler.Subscribe(this);
                inputManager.onJoin -= ctx => InstantiateInputHandler(ctx, inputManager);
            }
        }
    }

    public void SetPlayerIndex(int value)
    {
        // TODO: Implement player index replacement protection
        if (value > 0)
            _playerIndex = value;
    }

    public void MoveCallback(Vector2 input)
    {
        _inputs.direction.x = input.x;
        _inputs.direction.y = -input.y;
        UpdateControllerInput();
    }

    public void ChargeCallback(float value)
    {
        _inputs.isCharging = value > 0;
        UpdateControllerInput();
    }

    public void BreakCallback(float value)
    {
        _inputs.isBreaking = value > 0;
        UpdateControllerInput();
    }

    private void OnDestroy()
    {
        if (_inputHandler != null)
        {
            _inputHandler.Unsubscribe();
        }
    }
}
