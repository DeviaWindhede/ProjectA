using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Steamworks;

public class CustomNetworkManager : NetworkManager {
    [SerializeField] private PlayerObjectController _gamePlayerPrefab;
    public List<PlayerObjectController> GamePlayers { get; } = new List<PlayerObjectController>();

    public override void OnServerAddPlayer(NetworkConnectionToClient conn) {
        if (SceneManager.GetActiveScene().name == "Lobby") {
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
}
