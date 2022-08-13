using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using TMPro;
using Mirror.FizzySteam;
using static HelperFunctions;

[RequireComponent(typeof(FizzySteamworks), typeof(SteamMatchmaking))]
public class SteamLobby : Singleton<SteamLobby> {
    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequest;
    protected Callback<LobbyEnter_t> LobbyEntered;

    public ulong currentLobbyId;
    private const string HOST_ADDRESS_KEY = "HostAddress";
    private CustomNetworkManager networkManager;
    private CustomNetworkManager NetworkManager {
        get {
            if (networkManager != null) return networkManager;
            return networkManager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    //public GameObject hostButton;
    //public TextMeshProUGUI lobbyNameText;

    private void Start() {
        if (!SteamManager.Initialized)
            return;

        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        JoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostLobby() {
        SteamMatchmaking.CreateLobby(
            ELobbyType.k_ELobbyTypeFriendsOnly,
            NetworkManager.maxConnections
        );
    }

    public void CloseLobby() {
        var isHost = NetworkManager.GamePlayers.Find(p => p.isLocalPlayer && p.isServer);
        if (isHost) NetworkManager.StopHost();
        else LeaveLobby();
    }

    public void LeaveLobby() {
        SteamMatchmaking.LeaveLobby(new CSteamID(currentLobbyId));
    }

    private void OnLobbyCreated(LobbyCreated_t callback) {
        if (callback.m_eResult != EResult.k_EResultOK)
            return;

        log("Lobby successfully created!");

        NetworkManager.StartHost();

        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(
            lobbyId,
            HOST_ADDRESS_KEY,
            SteamUser.GetSteamID().ToString()
        );

        string lobbyName = SteamFriends.GetPersonaName().ToString() + "'s\nlobby";
        SteamMatchmaking.SetLobbyData(
            lobbyId,
            "name",
            lobbyName
        );
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback) {
        log("Request to join lobby");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback) {
        currentLobbyId = callback.m_ulSteamIDLobby;
        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        // Clients
        if (NetworkServer.active) return;

        NetworkManager.networkAddress = SteamMatchmaking.GetLobbyData(
            lobbyId,
            HOST_ADDRESS_KEY
        );

        NetworkManager.StartClient();
    }
}
