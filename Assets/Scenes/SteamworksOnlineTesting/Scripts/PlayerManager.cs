using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class PlayerManager : Mirror.NetworkBehaviour {
    [SerializeField, Range(1, 4)] private int playerCount = 1;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject _playerCameraPrefab;
    public static List<Player> players = new List<Player>();
    private PlayerFactory playerFactory;
    private PlayerFactory PlayerFactory {
        get {
            if (playerFactory != null) return playerFactory;
            return playerFactory = FindObjectOfType<PlayerFactory>();
        }
    }

    private List<GameObject> _cameras = new List<GameObject>();

    private CustomNetworkManager networkManager;
    private CustomNetworkManager NetworkManager {
        get {
            if (networkManager != null) return networkManager;
            return networkManager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    public override void OnStartServer() {
        base.OnStartServer();
        if (isServer) SpawnPlayersRpc();
    }

    public override void OnStartClient() {
        base.OnStartClient();
        if (isClient) {
            Player player = GameObject.FindObjectsOfType<Player>().First(p => p.ownerId == Mirror.NetworkClient.localPlayer.netId);
            if (player) {
                GameObject camera = Instantiate(this._playerCameraPrefab, transform);
                Camera c = camera.GetComponent<Camera>();
                c.cullingMask = c.cullingMask | 1 << (player.PlayerIndex + HelperFunctions.PLAYER_CAMERA_BASE_LAYER);
                camera.layer = HelperFunctions.GetCullingMask(player);
                camera.GetComponent<PlayerUIHandler>().SetPlayer(player);
                _cameras.Add(camera);
            }
        }
    }

    [ServerRpc]
    private void SpawnPlayersRpc() {
        foreach (var owner in NetworkManager.GamePlayers) {
            PlayerFactory.SpawnPlayer(owner.playerIdNumber - 1, owner);
        }
    }

    public GameObject GetCamera(int playerIndex) {
        return _cameras.Find(x => x.GetComponent<PlayerUIHandler>().PlayerIndex == playerIndex);
    }
}
