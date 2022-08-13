using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using static ResourceManager;

public class BreakableBox : NetworkBehaviour {

    [SerializeField]
    private GameObject _pickupPrefab;

    [SerializeField, Min(0)]
    private int minStatAmount;

    [SerializeField, Min(0)]
    private int maxStatAmount;

    [SerializeField]
    private float _gravityScale = 2;

    [SerializeField]
    private float _initialDownwardsSpeed = 5;

    [SerializeField]
    private float _initialSpawnSpeed = 5;

    [SerializeField]
    private Vector3 _spawnCenter;

    [SerializeField]
    private Vector3 _spawnSize;

    [SerializeField]
    private LayerMask _playerLayer;

    [SerializeField]
    private LayerMask _groundLayer;

    private Rigidbody _body;
    private bool _useGravity = true;
    private Material _material;
    private PlayerStats _stats;

    private void Awake() {
        _body = GetComponent<Rigidbody>();
        _body.velocity = Vector3.down * _initialDownwardsSpeed * Time.fixedDeltaTime;
    }

    // Gives pickup to player instead of otherway around to prevent edge-case
    private void FixedUpdate() {
        if (_useGravity && (isServer || (NetworkManager.singleton as CustomNetworkManager).IsLocalPlay)) {
            _body.AddForce(Vector3.down * _gravityScale * Time.fixedDeltaTime);
        }
    }

    private void OnCollisionEnter(Collision other) {
        if (_useGravity && other.gameObject.layer == Mathf.Log(_groundLayer.value, 2)) {
            _useGravity = false;
            _body.velocity = Vector3.zero;
        }
        if (other.gameObject.layer == Mathf.Log(_playerLayer.value, 2)) // TODO: Make it take a few hits
        {
            SpawnPickups();
            if (NetworkClient.active && isServer) BoxManager.DestroyBoxRpc(this);
            else if (!NetworkClient.active) Destroy(gameObject);
        }
    }

    [Unity.Netcode.ServerRpc]
    private void SpawnPickups() {
        int amount = Random.Range(minStatAmount, maxStatAmount);
        Vector3 min = transform.position + _spawnCenter - _spawnSize / 2;
        Vector3 max = transform.position + _spawnCenter + _spawnSize / 2;
        for (int i = 0; i < amount; i++) {
            Vector3 randomPos = new Vector3(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y),
                Random.Range(min.z, max.z)
            );
            var go = Instantiate(_pickupPrefab, randomPos, Quaternion.identity);
            var pu = go.GetComponent<Pickupable>();
            pu.AddForce(Vector3.up * _initialSpawnSpeed);

            System.Array stats = System.Enum.GetValues(typeof(StatType));
            StatType randomStat = (StatType)stats.GetValue(Random.Range(1, stats.Length));
            pu.SetStatType(randomStat);

            if (NetworkClient.active) NetworkServer.Spawn(go);
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + _spawnCenter, _spawnSize);
    }
}
