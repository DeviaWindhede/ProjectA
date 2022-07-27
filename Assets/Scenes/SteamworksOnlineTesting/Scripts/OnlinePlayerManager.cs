using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using Mirror;

public class OnlinePlayerManager : Mirror.NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject _playerCameraPrefab;

    public const int PLAYER_CAMERA_BASE_LAYER = 9;
    public int GetCullingMask(Player player) {
        return PLAYER_CAMERA_BASE_LAYER + player.PlayerIndex;
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
        if (isServer) {
            SpawnPlayersRpc();
        }
    }

    public override void OnStartClient() {
        base.OnStartClient();
        if (isClient) {
            Player player = GameObject.FindObjectsOfType<Player>().First(p => p.ownerId == Mirror.NetworkClient.localPlayer.netId);
            if (player) {
                GameObject camera = Instantiate(this._playerCameraPrefab, transform);
                Camera c = camera.GetComponent<Camera>();
                c.cullingMask = c.cullingMask | 1 << (player.PlayerIndex + PLAYER_CAMERA_BASE_LAYER);
                camera.layer = GetCullingMask(player);
                camera.GetComponent<PlayerUIHandler>().SetPlayer(player);
                _cameras.Add(camera);
            }
        }
    }

    [ServerRpc]
    private void SpawnPlayersRpc() {
        foreach (var owner in NetworkManager.GamePlayers) {
            var go = Instantiate(playerPrefab);
            go.transform.position = Vector3.up;
            Player player = go.GetComponent<Player>();
            player.GetFollowVirtualCamera.layer = GetCullingMask(player);
            player.GetFollowVirtualCamera.SetActive(owner.hasAuthority);
            player.ownerId = owner.netId;
            NetworkServer.Spawn(go, owner.gameObject); // register ownership och spawna enbart för de som inte har en owner
        }
    }

    public GameObject GetCamera(int playerIndex) {
        return _cameras.Find(x => x.GetComponent<PlayerUIHandler>().PlayerIndex == playerIndex);
    }
}
