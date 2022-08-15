using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using Mirror;

public class BoxManager : Mirror.NetworkBehaviour {
    private BoxFactory boxFactory;
    private BoxFactory BoxFactory {
        get {
            if (boxFactory != null) return boxFactory;
            return boxFactory = FindObjectOfType<BoxFactory>();
        }
    }

    private Timer timer;

    public override void OnStartServer() {
        base.OnStartServer();
        if (isServer) {
            SpawnBoxesRpc();
            timer = new Timer(BoxFactory.GetRandomSpawnTime()); // TODO: Improve factory and online manager flow
        }
    }

    public void FixedUpdate() {
        if (!isServer) return;

        timer += Time.fixedDeltaTime;
        if (timer.Expired) {
            SpawnBoxesRpc();
            timer = new Timer(BoxFactory.GetRandomSpawnTime());
        }
    }

    [ServerRpc]
    private void SpawnBoxesRpc() {
        float boxCount = BoxFactory.GetRandomBoxSpawnCount();
        for (int i = 0; i < boxCount; i++) {
            GameObject box = BoxFactory.SpawnBox(
                BoxFactory.GetRandomSpawnPosition(),
                HelperFunctions.GetRandomRotation()
            );
            NetworkServer.Spawn(box);
        }
    }

    [ServerRpc]
    public static void DestroyBoxRpc(BreakableBox box) {
        if (box != null) NetworkServer.Destroy(box.gameObject);
    }
}
