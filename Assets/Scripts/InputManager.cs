using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine.SceneManagement;

public class Input
{
    private int _index;
    private PlayerInput _playerInput;
    private PlayerInputActionMapping _mapping;

    public int Index { get { return _index; } }
    public PlayerInput PlayerInput { get { return _playerInput; } }
    public PlayerInputActionMapping Mapping { get { return _mapping; } }

    public Input(int index, PlayerInput playerInput)
    {
        _index = index;
        _playerInput = playerInput;

        InputActionMap gameplayMap = playerInput.actions.FindActionMap(InputManager.GAMEPLAY_MAPPING_NAME, true);
        _mapping = new PlayerInputActionMapping(gameplayMap);
    }
}

[RequireComponent(typeof(PlayerInputManager))]
public class InputManager : Singleton<MonoBehaviour>
{
    public const string GAMEPLAY_MAPPING_NAME = "Gameplay";
    public event System.Action<Input> onJoin;

    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string lobbySceneName = "Lobby"; // Used to prevent lobby join via invite edge case
    private List<Input> _inputs;
    private PlayerInputManager _manager;

    public int InputCount { get { return _inputs.Count; } }

    private static GameObject gameObjectInstance;
    private void Awake()
    {
        _inputs = new List<Input>();

        _manager = GetComponent<PlayerInputManager>();
        _manager.onPlayerJoined += ctx => this.OnPlayerJoined(ctx);
        _manager.onPlayerLeft += ctx => this.OnPlayerLeft(ctx);

        if (gameObjectInstance != null)
            Destroy(gameObject);

        gameObjectInstance = gameObject;
        DontDestroyOnLoad(this);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode _) {
        bool isInvalidScene = scene.name == mainMenuSceneName || scene.name == lobbySceneName;
        if (isInvalidScene && gameObject != null) {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Destroy(gameObject);
        }
    }

    private void OnPlayerJoined(PlayerInput input)
    {
        input.gameObject.transform.parent = this.transform;
        input.gameObject.name = "PlayerInput " + this._inputs.Count;

        var inputObj = new Input(this._inputs.Count, input);
        this._inputs.Add(inputObj);
        this.onJoin.Invoke(inputObj);
    }

    private void OnPlayerLeft(PlayerInput input)
    {
        print("Player " + this._inputs.Find(x => x.PlayerInput == input).Index + " has disconnected");
        // TODO: Handle input disconnect
        // this.inputs.Remove(input);
    }

    public Input GetPlayerInput(int index)
    {
        return _inputs.Exists(i => i.Index == index) ? _inputs.First(i => i.Index == index) : null;
    }
}
