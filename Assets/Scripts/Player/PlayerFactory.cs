using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerFactory : MonoBehaviour {
    public Transform[] spawnPoints;
    [SerializeField, Range(1, 4)] private int playerCount = 1;
    [SerializeField] private GameObject playerPrefab;
    public static List<Player> players;

    private CustomNetworkManager networkManager;
    private CustomNetworkManager NetworkManager {
        get {
            if (networkManager != null) return networkManager;
            return networkManager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    void Start() {
        players = new List<Player>();
        if (NetworkManager.isLocalPlay) {
            var inputManager = FindObjectOfType<InputManager>();
            if (inputManager != null && inputManager.InputCount > 0) playerCount = inputManager.InputCount;

            if (NetworkManager.isLocalPlay) SpawnPlayersLocally();
        }
    }

    private void SpawnPlayersLocally() {
        if (NetworkManager.isLocalPlay) {
            var inputManager = FindObjectOfType<InputManager>();
            if (inputManager != null && inputManager.InputCount > 0) playerCount = inputManager.InputCount;

            for (int index = 0; index < playerCount; index++) {
                SpawnPlayer(index);
            }
        }
        else {
            Debug.LogWarning("SpawnPlayersLocally was called during online play", this);
        }
    }

    public void SpawnPlayer(int playerIndex, PlayerObjectController owner = null) {
        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;
        if (spawnPoints.Length >= playerIndex && spawnPoints[playerIndex]) {
            spawnPosition = spawnPoints[playerIndex].position;
            spawnRotation = spawnPoints[playerIndex].rotation;
        }

        GameObject go = Instantiate(playerPrefab, spawnPosition, spawnRotation, this.transform);
        go.transform.position = Vector3.up;
        Player player = go.GetComponent<Player>();

        if (NetworkManager.isLocalPlay) {
            player.SetPlayerIndex(playerIndex);
            players.Add(player);
        }
        else if (owner != null) {
            player.GetFollowVirtualCamera.layer = HelperFunctions.GetCullingMask(player);
            player.GetFollowVirtualCamera.SetActive(owner.hasAuthority);
            player.ownerId = owner.netId;
            NetworkServer.Spawn(go, owner.gameObject); // register ownership och spawna enbart för de som inte har en owner
        }
        else {
            Debug.LogWarning("SpawnPlayer was called without an owner during online play", this);
        }
    }
}
