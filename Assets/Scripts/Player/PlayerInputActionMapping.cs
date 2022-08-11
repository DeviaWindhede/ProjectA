using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static HelperFunctions;
public interface IPlayerInputCallbacks
{
    void MoveCallback(Vector2 input);
    void ChargeCallback(float value);
    void BreakCallback(float value);
    void PauseCallback(float value);
}

public class PlayerInputActionMapping
{
    private InputActionMap _inputActions;
    private IPlayerInputCallbacks _playerInput;
    private InputAction _actionMove;
    private InputAction _actionCharge;
    private InputAction _actionBreak;
    private InputAction _actionPause;

    public PlayerInputActionMapping(InputActionMap actionMap) // TODO: use builder pattern
    {
        _inputActions = actionMap;
        _actionMove = actionMap.FindAction("Move", true);
        _actionCharge = actionMap.FindAction("Charge", true);
        _actionBreak = actionMap.FindAction("Break", true);
        _actionPause = actionMap.FindAction("Pause", true);
    }

    public void Subscribe(IPlayerInputCallbacks input)
    {
        this.Unsubscribe();
        _playerInput = input;

        SubscribeToInputAction(_actionMove, context => _playerInput.MoveCallback(context.ReadValue<Vector2>()), true);
        SubscribeToInputAction(_actionCharge, context => _playerInput.ChargeCallback(context.ReadValue<float>()), true);
        SubscribeToInputAction(_actionBreak, context => _playerInput.BreakCallback(context.ReadValue<float>()), true);
        SubscribeToInputAction(_actionPause, context => _playerInput.PauseCallback(context.ReadValue<float>()), true);
    }

    public void Unsubscribe()
    {
        if (_playerInput != null)
        {
            UnsubscribeFromInputAction(_actionMove, context => _playerInput.MoveCallback(context.ReadValue<Vector2>()), true);
            UnsubscribeFromInputAction(_actionCharge, context => _playerInput.ChargeCallback(context.ReadValue<float>()), true);
            UnsubscribeFromInputAction(_actionBreak, context => _playerInput.BreakCallback(context.ReadValue<float>()), true);
            UnsubscribeFromInputAction(_actionPause, context => _playerInput.PauseCallback(context.ReadValue<float>()), true);
            _playerInput = null;
        }
    }

    private void SubscribeToInputAction(InputAction inputAction, Action<InputAction.CallbackContext> callbackAction, bool isButton = true)
    {
        inputAction.Enable();
        inputAction.performed += c => callbackAction(c);
        if (isButton) inputAction.canceled += c => callbackAction(c);
    }
    private void UnsubscribeFromInputAction(InputAction inputAction, Action<InputAction.CallbackContext> callbackAction, bool isButton = true)
    {
        inputAction.performed -= c => callbackAction(c);
        if (isButton) inputAction.canceled -= c => callbackAction(c);
        _actionBreak.Disable();
    }
}
