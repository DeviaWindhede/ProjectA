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

    private List<Input> inputs;
    private PlayerInputManager manager;


    private void Awake()
    {
        inputs = new List<Input>();

        manager = GetComponent<PlayerInputManager>();
        manager.onPlayerJoined += ctx => this.OnPlayerJoined(ctx);
        manager.onPlayerLeft += ctx => this.OnPlayerLeft(ctx);
    }

    public void OnPlayerJoined(PlayerInput input)
    {
        input.gameObject.transform.parent = this.transform;
        input.gameObject.name = "PlayerInput " + this.inputs.Count;

        var inputObj = new Input(this.inputs.Count, input);
        this.inputs.Add(inputObj);
        this.onJoin.Invoke(input);
    }

    public void OnPlayerLeft(PlayerInput input)
    {
        print("Player " + this.inputs.Find(x => x.playerInput == input).index + " has disconnected");
        // this.inputs.Remove(input);
    }

    public PlayerInput GetPlayerInput(int index)
    {
        return inputs.Exists(i => i.index == index) ? inputs.First(i => i.index == index).playerInput : null;
    }
}
