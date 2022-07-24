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

[RequireComponent(typeof(PlayerController), typeof(PlayerData))]
public class Player : MonoBehaviour, IPlayerInputCallbacks
{
    [SerializeField]
    private int _playerIndex = 0;

    [SerializeField]
    private bool _reversedFlyControls;

    [SerializeField]
    private Transform _followVirtualCamera;

    private PlayerController _playerController;
    private PlayerInputActionMapping _inputMapping;
    private PlayerInputValues _inputs;
    private InputManager _inputManager;

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
        _inputManager = GameObject.FindObjectOfType<InputManager>();
        if (_inputManager != null) // method is always unsubscribed from in first step when the input is preassigned from playerindex
        {                          // therefore a null check is not needed
            _inputManager.onJoin += ctx =>
            {
                if (ctx.Index == _playerIndex) {
                    InstantiateInputHandler(ctx);
                }
            };
        }
    }

    private void UpdateControllerInput()
    {
        _playerController.UpdateInputs(_inputs);
    }

    private void InstantiateInputHandler(Input input)
    {
        if (input != null)
        {
            _inputMapping = input.Mapping;
            _inputMapping.Subscribe(this);
            _inputManager.onJoin -= ctx => InstantiateInputHandler(ctx);
        }
    }

    public void SetPlayerIndex(int value)
    {
        // TODO: Implement player index replacement protection
        if (value >= 0 && _inputManager != null)
        {
            _playerIndex = value;
            Input playerInput = _inputManager.GetPlayerInput(_playerIndex);
            if (playerInput != null) this.InstantiateInputHandler(playerInput);
        }
    }

    public void MoveCallback(Vector2 input)
    {
        _inputs.direction.x = input.x;
        _inputs.direction.y = -input.y * (_reversedFlyControls ? -1 : 1);
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

    public void PauseCallback(float value)
    {
        // Missing implementation
    }

    private void OnDestroy()
    {
        if (_inputMapping != null)
        {
            _inputMapping.Unsubscribe();
        }
    }

    // private void OnValidate()
    // {
    //     if (Application.isPlaying)
    //         _playerController.UpdatePlayerStats(_stats);
    // }
}
