using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Steamworks;
using Mirror.FizzySteam;

public class CustomNetworkManager : NetworkManager {
    [Header("Custom fields")]
    public const string LOBBY_SCENE_NAME = "Lobby";
    [SerializeField] private PlayerObjectController _gamePlayerPrefab;
    public List<PlayerObjectController> GamePlayers { get; } = new List<PlayerObjectController>();

    public override void Start() {
        base.Start();
        Transport.activeTransport = SteamDataManager.Instance.FizzySteamworks;
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
        NetworkServer.DisconnectAll();
        StopHost();
        base.OnApplicationQuit();
    }
}
