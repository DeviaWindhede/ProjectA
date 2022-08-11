using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Steamworks;
using Mirror.FizzySteam;

public class CustomNetworkManager : NetworkManager {
    [Header("Custom fields")]
    [SerializeField] private bool _isLocalPlay = false;
    public bool IsLocalPlay {
        get { return _isLocalPlay; }
        set {
            _isLocalPlay = value;
            var steamworks = GetComponent<FizzySteamworks>();
            var steamManager = GetComponent<SteamManager>();
            var steamLobby = GetComponent<SteamLobby>();

            var isOnlinePlay = !value;
            if (steamManager) steamManager.enabled = isOnlinePlay;
            if (steamLobby) steamLobby.enabled = isOnlinePlay;
            if (steamworks) steamworks.enabled = isOnlinePlay;

            transport = isOnlinePlay ? steamworks : null;
        }
    }
    public const string LOBBY_SCENE_NAME = "Lobby";
    [SerializeField] private PlayerObjectController _gamePlayerPrefab;
    public List<PlayerObjectController> GamePlayers { get; } = new List<PlayerObjectController>();

    public override void Awake() {
        base.Awake();
        //IsLocalPlay = _isLocalPlay;
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn) {
        if (SceneManager.GetActiveScene().name == LOBBY_SCENE_NAME) {
            PlayerObjectController instance = Instantiate(_gamePlayerPrefab);
            instance.connectionId = conn.connectionId;
            instance.playerIdNumber = GamePlayers.Count + 1;
            instance.playerSteamId = (ulong)SteamMatchmaking.GetLobbyMemberByIndex(
                (CSteamID)SteamLobby.Instance.currentLobbyId,
                GamePlayers.Count
            );

            NetworkServer.AddPlayerForConnection(conn, instance.gameObject);
        }
    }

    public void StartGame(string sceneName) {
        ServerChangeScene(sceneName);
    }

    public override void OnApplicationQuit() {
        base.OnApplicationQuit();
        NetworkServer.DisconnectAll();
    }
}
