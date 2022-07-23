using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using TMPro;
using static HelperFunctions;

public class SteamLobby : Singleton<MonoBehaviour> {
    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequest;
    protected Callback<LobbyEnter_t> LobbyEntered;

    public ulong currentLobbyId;
    private const string HOST_ADDRESS_KEY = "HostAddress";
    private CustomNetworkManager networkManager;

    //public GameObject hostButton;
    //public TextMeshProUGUI lobbyNameText;

    private void Start() {
        if (!SteamManager.Initialized)
            return;

        networkManager = GetComponent<CustomNetworkManager>();

        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        JoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostLobby() {
        SteamMatchmaking.CreateLobby(
            ELobbyType.k_ELobbyTypeFriendsOnly,
            networkManager.maxConnections
        );
    }

    private void OnLobbyCreated(LobbyCreated_t callback) {
        if (callback.m_eResult != EResult.k_EResultOK)
            return;

        log("Lobby successfully created!");

        networkManager.StartHost();

        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(
            lobbyId,
            HOST_ADDRESS_KEY,
            SteamUser.GetSteamID().ToString()
        );

        string lobbyName = SteamFriends.GetPersonaName().ToString() + "'s\nlobby";
        SteamMatchmaking.SetLobbyData(
            lobbyId,
            "lobbyName",
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
        if (NetworkServer.active)
            return;

        networkManager.networkAddress = SteamMatchmaking.GetLobbyData(
            lobbyId,
            HOST_ADDRESS_KEY
        );

        networkManager.StartClient();
    }
}
