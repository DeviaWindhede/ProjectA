using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    [SerializeField]
    private Button startServerButton;

    [SerializeField]
    private Button startHostButton;

    [SerializeField]
    private Button startClientButton;

    [SerializeField]
    private TextMeshProUGUI playersInGameText;

    private void Awake() {
        Cursor.visible = true;
    }

    private void Start() {
        startHostButton.onClick.AddListener(() => {
            if (NetworkManager.Singleton.StartHost()) {
                Logger.Instance.LogInfo("Host started");
            }
            else {
                Logger.Instance.LogWarning("Host could not be started");
            }
        });

        startServerButton.onClick.AddListener(() => {
            if (NetworkManager.Singleton.StartServer()) {
                Logger.Instance.LogInfo("Server started");
            }
            else {
                Logger.Instance.LogWarning("Server could not be started");
            }
        });

        startClientButton.onClick.AddListener(() => {
            if (NetworkManager.Singleton.StartClient()) {
                Logger.Instance.LogInfo("Client started");
            }
            else {
                Logger.Instance.LogWarning("Client could not be started");
            }
        });
    }

    private void Update() {
        playersInGameText.text = $"Players in game: {NetworkPlayerManager.Instance.PlayerCount}";
    }
}
