using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using static HelperFunctions;

public class LobbyController : Singleton<LobbyController> {
    public TextMeshProUGUI lobbyNameText;

    public GameObject playerListViewContent;
    public GameObject playerListItemPrefab;
    public GameObject localPlayerObject;

    public ulong currentLobbyId;
    public bool playerItemCreated = false;
    private List<PlayerListItem> playerListItems = new List<PlayerListItem>();
    public PlayerObjectController localPlayerController;

    private CustomNetworkManager networkManager;
    private CustomNetworkManager NetworkManager {
        get {
            if (networkManager != null) return networkManager;
            return networkManager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    public void UpdateLobbyName() {
        currentLobbyId = NetworkManager.GetComponent<SteamLobby>().currentLobbyId;
        lobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(currentLobbyId), "name");
    }

    public void UpdatePlayerList() {
        if (!playerItemCreated) CreateHostPlayerItem();
        if (playerListItems.Count < NetworkManager.GamePlayers.Count) CreateClientPlayerItem();
        if (playerListItems.Count > NetworkManager.GamePlayers.Count) RemovePlayerItem();
        if (playerListItems.Count == NetworkManager.GamePlayers.Count) UpdatePlayerItem();
    }

    public void FindLocalPlayer() {
        localPlayerObject = GameObject.Find(PlayerObjectController.OBJECT_NAME);
        localPlayerController = localPlayerObject.GetComponent<PlayerObjectController>();
    }

    public void CreateHostPlayerItem() {
        foreach (PlayerObjectController player in NetworkManager.GamePlayers) {
            InstantiatePlayerItem(player);
            this.playerItemCreated = true;
        }
    }

    public void CreateClientPlayerItem() {
        foreach (PlayerObjectController player in NetworkManager.GamePlayers) {
            if (!playerListItems.Any(p => p.connectionId == player.connectionId)) {
                InstantiatePlayerItem(player);
            }
        }
    }
    private void InstantiatePlayerItem(PlayerObjectController player) {
        if (!playerListItemPrefab) return;

        GameObject go = Instantiate(playerListItemPrefab) as GameObject;
        PlayerListItem playerItem = go.GetComponent<PlayerListItem>();

        playerItem.playerName = player.playerName;
        playerItem.connectionId = player.connectionId;
        playerItem.playerSteamId = player.playerSteamId;
        playerItem.SetPlayerValues();

        go.transform.SetParent(playerListViewContent.transform);
        go.transform.localScale = Vector3.one;

        playerListItems.Add(playerItem);
    }

    public void UpdatePlayerItem() {
        foreach (PlayerObjectController player in NetworkManager.GamePlayers) {
            foreach (PlayerListItem item in playerListItems) {
                if (item.connectionId == player.connectionId) {
                    item.playerName = player.playerName;
                    item.SetPlayerValues();
                }
            }
        }
    }

    public void RemovePlayerItem() {
        for (int i = playerListItems.Count - 1; i >= 0; i--) {
            var item = playerListItems[i];
            if (NetworkManager.GamePlayers.Any(p => p.connectionId == item.connectionId)) {
                playerListItems.Remove(item);
                Destroy(item.gameObject);
                item = null;
            }
        }
    }

    public void StartGame(string sceneName) {
        localPlayerController.CanStartGame(sceneName);
    }
}
