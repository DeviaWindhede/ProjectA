using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using static HelperFunctions;

[System.Serializable]
public struct PlayerStats
{
    public const int MAX_STAT_VALUE = 18;
    public const int MIN_STAT_VALUE = -14;

    [Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    public int _boost;

    [Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    public int _charge;

    [Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    public int _defence; // Missing implementation

    [Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    public int _glide;

    [Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    public int _health; // Missing implementation

    [Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    public int _offence; // Missing implementation

    [Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    public int _topSpeed;

    [Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    public int _turn;

    [Range(MIN_STAT_VALUE, MAX_STAT_VALUE)]
    public int _weight;

    public int Boost
    {
        get { return _boost; }
        set { _boost = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Charge
    {
        get { return _charge; }
        set { _charge = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Defence
    {
        get { return _defence; }
        set { _defence = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Glide
    {
        get { return _glide; }
        set { _glide = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Health
    {
        get { return _health; }
        set { _health = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Offence
    {
        get { return _offence; }
        set { _offence = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int TopSpeed
    {
        get { return _topSpeed; }
        set { _topSpeed = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Turn
    {
        get { return _turn; }
        set { _turn = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }
    public int Weight
    {
        get { return _weight; }
        set { _weight = Mathf.Clamp(value, MIN_STAT_VALUE, MAX_STAT_VALUE); }
    }

    public PlayerStats(
        int boost,
        int charge,
        int defence,
        int glide,
        int health,
        int offence,
        int topSpeed,
        int turn,
        int weight
    )
    {
        _boost = boost;
        _charge = charge;
        _defence = defence;
        _glide = glide;
        _health = health;
        _offence = offence;
        _topSpeed = topSpeed;
        _turn = turn;
        _weight = weight;
    }

    public static PlayerStats operator +(PlayerStats current, PlayerStats apply)
    {
        current.Boost += apply.Boost;
        current.Charge += apply.Charge;
        current.Defence += apply.Defence;
        current.Glide += apply.Glide;
        current.Health += apply.Health;
        current.Offence += apply.Offence;
        current.TopSpeed += apply.TopSpeed;
        current.Turn += apply.Turn;
        current.Weight += apply.Weight;
        return current;
    }
}

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

    [SerializeField]
    private PlayerStats _stats;

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
        UpdatePlayerStats(new PlayerStats());
    }

    private void SetupInputs()
    {
        _inputs = new PlayerInputValues();
        _inputs.direction = Vector2.zero;
        _inputs.isCharging = false;
        InputManager inputManager = GameObject.Find("InputManager").GetComponent<InputManager>();
        PlayerInput playerInput = inputManager.GetPlayerInput(_playerIndex);

        if (playerInput != null)
            this.InstantiateInputHandler(playerInput, inputManager);
        else
        {
            inputManager.onJoin += ctx =>
            {
                if (ctx.playerIndex == _playerIndex) {
                    InstantiateInputHandler(ctx, inputManager);
                }
            };
        }
    }

    private void UpdateControllerInput()
    {
        _playerController.UpdateInputs(_inputs);
    }

    public void UpdatePlayerStats(PlayerStats stats)
    {
        _stats += stats;
        _playerController.UpdatePlayerStats(_stats);
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
