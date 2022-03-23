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
        players = new List<Player>();
        for (int i = 0; i < playerCount; i++) {
            GameObject go = Instantiate(playerPrefab, this.transform);
            Player p = go.GetComponent<Player>();
            p.SetPlayerIndex(i);
            if (spawnPoints.Length >= playerCount && spawnPoints[i]) {
                go.transform.position = spawnPoints[i].position;
                go.transform.rotation = spawnPoints[i].rotation;
            }
            players.Add(p);
        }
    }
}
