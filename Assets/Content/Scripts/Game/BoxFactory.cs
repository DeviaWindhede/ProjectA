using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using static ResourceManager;

public class BoxFactory : MonoBehaviour {
    [SerializeField]
    private GameObject _breakableBoxPrefab;

    [SerializeField]
    private Vector3 _spawnCenter;

    [SerializeField]
    private Vector2 _spawnArea;

    [SerializeField, Min(0)]
    private float _minTime;

    [SerializeField, Min(0)]
    private float _maxTime;

    [SerializeField, Min(0)]
    private int _minBoxesToSpawn;

    [SerializeField, Min(0)]
    private int _maxBoxesToSpawn;

    private Timer timer;

    private CustomNetworkManager networkManager;
    private CustomNetworkManager NetworkManager {
        get {
            if (networkManager != null) return networkManager;
            return networkManager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    private void Start() {
        if (NetworkClient.active || !NetworkManager.IsLocalPlay) return;

        timer = new Timer(GetRandomSpawnTime());
        SpawnBoxesLocally();
    }

    private void SpawnBoxesLocally() {
        float boxCount = GetRandomBoxSpawnCount();
        for (int i = 0; i < boxCount; i++) {
            SpawnBox(GetRandomSpawnPosition(), HelperFunctions.GetRandomRotation());
        }
    }

    // Update is called once per frame
    private void FixedUpdate() {
        if (NetworkClient.active || !NetworkManager.IsLocalPlay) return;

        timer += Time.fixedDeltaTime;
        if (timer.Expired) {
            SpawnBoxesLocally();
            timer = new Timer(GetRandomSpawnTime());
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_spawnCenter, new Vector3(_spawnArea.x, 0, _spawnArea.y));
    }
    public float GetRandomSpawnTime() {
        return Random.Range(_minTime, _maxTime + 1f);
    }

    public float GetRandomBoxSpawnCount() {
        return Random.Range(_minBoxesToSpawn, _maxBoxesToSpawn + 1f);
    }

    public Vector3 GetRandomSpawnPosition() {
        Vector3 spawnSize = new Vector3(_spawnArea.x, 0, _spawnArea.y);
        Vector3 min = _spawnCenter - spawnSize / 2;
        Vector3 max = _spawnCenter + spawnSize / 2;

        return new Vector3(
            Random.Range(min.x, max.x),
            Random.Range(min.y, max.y),
            Random.Range(min.z, max.z)
        );
    }

    public GameObject SpawnBox(Vector3 position, float rotation) {
        return Instantiate(_breakableBoxPrefab, position, Quaternion.Euler(0, rotation, 0), transform);
    }
}
