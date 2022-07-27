using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;

public class PlayerObjectController : NetworkBehaviour {
    [SerializeField] private GameObject test;
    public const string OBJECT_NAME = "LocalGamePlayer";
    [HideInInspector] public GameObject playerObject;
    [SyncVar] public int connectionId;
    [SyncVar] public int playerIdNumber;
    [SyncVar] public ulong playerSteamId;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string playerName;

    private CustomNetworkManager networkManager;

    private CustomNetworkManager NetworkManager {
        get {
            if (networkManager != null) return networkManager;
            return networkManager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    private void Start() {
        DontDestroyOnLoad(this.gameObject); // TODO interaction with real player
    }

    public override void OnStartAuthority() {
        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        gameObject.name = OBJECT_NAME;
        LobbyController.Instance.FindLocalPlayer();
        LobbyController.Instance.UpdateLobbyName();
    }

    public override void OnStartClient() {
        NetworkManager.GamePlayers.Add(this);
        LobbyController.Instance.UpdateLobbyName();
        LobbyController.Instance.UpdatePlayerList();
    }

    public override void OnStopClient() {
        NetworkManager.GamePlayers.Remove(this);
        LobbyController.Instance.UpdatePlayerList();
    }

    [Command]
    private void CmdSetPlayerName(string playerName) {
        this.PlayerNameUpdate(this.playerName, playerName);
    }

    public void PlayerNameUpdate(string oldValue, string newValue) {
        if (isServer) {
            this.playerName = newValue;
        }
        if (isClient) {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    public void CanStartGame(string sceneName) {
        if (hasAuthority) CmdCanStartGame(sceneName);
    }

    [Command]
    public void CmdCanStartGame(string sceneName) {
        networkManager.StartGame(sceneName);
    }
}
