using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerJoinInputMenuHandler : MonoBehaviour
{
    [SerializeField] private List<TextMeshProUGUI> text;
    private InputManager _manager;
    private InputAction _p1ActionMap;

    void Start()
    {
        _manager = FindObjectOfType<InputManager>();
        _manager.onJoin += ctx => {
            text[ctx.playerIndex].SetText(ctx.user.pairedDevices[0].displayName);
            if (_p1ActionMap == null) {
                var i = _manager.GetPlayerInput(0);

                InputActionMap gameplayMap = i.actions.actionMaps
                    .ToArray()
                    .First(m => m.name == InputManager.GAMEPLAY_MAPPING_NAME);
                if (gameplayMap != null)
                {
                    _p1ActionMap = gameplayMap.FindAction("Pause", true);
                    _p1ActionMap.Enable();
                    _p1ActionMap.performed += _ => HandleStartGame();
                }
            }
        };
    }

    public void HandleStartGame()
    {
        _p1ActionMap.performed -= _ => HandleStartGame();
        _p1ActionMap.Disable();
        SceneManager.LoadScene("TestGameScene");
    }
}
