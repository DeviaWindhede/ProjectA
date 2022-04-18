using UnityEngine;
using UnityEngine.InputSystem;

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

    public PlayerInputActionMapping(InputActionMap actionMap)
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

        _actionMove.Enable();
        _actionMove.performed += context => _playerInput.MoveCallback(context.ReadValue<Vector2>());
        _actionMove.canceled += context => _playerInput.MoveCallback(context.ReadValue<Vector2>());

        _actionCharge.Enable();
        _actionCharge.performed += context =>
            _playerInput.ChargeCallback(context.ReadValue<float>());
        _actionCharge.canceled += context =>
            _playerInput.ChargeCallback(context.ReadValue<float>());

        _actionBreak.Enable();
        _actionBreak.performed += context => _playerInput.BreakCallback(context.ReadValue<float>());
        _actionBreak.canceled += context => _playerInput.BreakCallback(context.ReadValue<float>());

        _actionPause.Enable();
        _actionPause.performed += context => _playerInput.BreakCallback(context.ReadValue<float>());
        _actionPause.canceled += context => _playerInput.BreakCallback(context.ReadValue<float>());
    }

    public void Unsubscribe()
    {
        if (_playerInput != null)
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

            _actionPause.performed -= context =>
                _playerInput.PauseCallback(context.ReadValue<float>());
            _actionPause.canceled -= context =>
                _playerInput.PauseCallback(context.ReadValue<float>());
            _actionPause.Disable();

            _playerInput = null;
        }
    }
}
