using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField, Range(1, 4)] private int playerCount = 1;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject playerPrefab;
    void Start()
    {
        for (int i = 0; i < playerCount; i++) {
            var player = Instantiate(playerPrefab, this.transform);
            player.GetComponent<Player>().SetPlayerIndex(i);
            if (spawnPoints.Length >= playerCount && spawnPoints[i]) {
                player.transform.position = spawnPoints[i].position;
                player.transform.rotation = spawnPoints[i].rotation;
            }
        }
    }
}
