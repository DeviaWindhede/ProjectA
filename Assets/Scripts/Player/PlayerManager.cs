using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField, Range(1, 4)] private int playerCount = 1;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject playerPrefab;
    public static List<Player> players;
    void Start()
    {
        var inputManager = FindObjectOfType<InputManager>();
        if (inputManager != null && inputManager.AvailableInputs > 0) playerCount = inputManager.AvailableInputs;

        players = new List<Player>();
        for (int i = 0; i < playerCount; i++) {
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;
            if (spawnPoints.Length >= playerCount && spawnPoints[i]) {
                spawnPosition = spawnPoints[i].position;
                spawnRotation = spawnPoints[i].rotation;
            }

            Player player = Instantiate(playerPrefab, spawnPosition, spawnRotation, this.transform).GetComponent<Player>();
            player.SetPlayerIndex(i);
            players.Add(player);
        }
    }
}
