using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public struct Input
{
    public int index;
    public PlayerInput playerInput;

    public Input(int index, PlayerInput playerInput)
    {
        this.index = index;
        this.playerInput = playerInput;
    }
}

[RequireComponent(typeof(PlayerInputManager))]
public class InputManager : MonoBehaviour
{
    public const string GAMEPLAY_MAPPING_NAME = "Gameplay";
    public event System.Action<PlayerInput> onJoin;

    public int AvailableInputs { get { return _inputs.Count; } }

    private List<Input> _inputs;
    private PlayerInputManager _manager;


    private void Awake()
    {
        _inputs = new List<Input>();

        _manager = GetComponent<PlayerInputManager>();
        _manager.onPlayerJoined += ctx => this.OnPlayerJoined(ctx);
        _manager.onPlayerLeft += ctx => this.OnPlayerLeft(ctx);

        Object.DontDestroyOnLoad(this);
    }

    private void OnPlayerJoined(PlayerInput input)
    {
        input.gameObject.transform.parent = this.transform;
        input.gameObject.name = "PlayerInput " + this._inputs.Count;

        var inputObj = new Input(this._inputs.Count, input);
        this._inputs.Add(inputObj);
        this.onJoin.Invoke(input);
    }

    private void OnPlayerLeft(PlayerInput input)
    {
        print("Player " + this._inputs.Find(x => x.playerInput == input).index + " has disconnected");
        // this.inputs.Remove(input);
    }

    public PlayerInput GetPlayerInput(int index)
    {
        return _inputs.Exists(i => i.index == index) ? _inputs.First(i => i.index == index).playerInput : null;
    }
}
