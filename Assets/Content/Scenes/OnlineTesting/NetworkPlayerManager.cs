using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static HelperFunctions;

public class NetworkPlayerManager : NetworkSingleton<NetworkPlayerManager> {
    private NetworkVariable<int> _playerCount = new NetworkVariable<int>();
    public int PlayerCount { get { return _playerCount.Value; } }

    private void Start() {
        NetworkManager.Singleton.OnClientConnectedCallback += id => {
            if (NetworkManager.Singleton.IsServer) {
                Logger.Instance.LogInfo($"{id} connected");
                _playerCount.Value++;
            }
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += id => {
            if (NetworkManager.Singleton.IsServer) {
                Logger.Instance.LogInfo($"{id} disconnected");
                _playerCount.Value--;
            }
        };
    }

    // Update is called once per frame
    void Update() {

    }
}
