using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using static ResourceManager;

[System.Serializable]
public struct PlayerStatPickupInfo
{
    public PlayerStats playerStats;
    public Texture2D texture;

    public PlayerStatPickupInfo(PlayerStats playerStats, Texture2D texture)
    {
        this.playerStats = playerStats;
        this.texture = texture;
    }
}

// TODO: Implement custom editor
[RequireComponent(typeof(Rigidbody))]
public class Pickupable : NetworkBehaviour
{
    // TODO: Implement powerups and ability pickups
    [SerializeField]
    private StatType _statType;

    [SerializeField]
    private float _hitboxRadius = 1f;

    [SerializeField]
    private LayerMask _playerLayer;

    [SerializeField]
    private float _gravityScale = 2;

    [SerializeField]
    private LayerMask _groundLayer;

    private Rigidbody _body;
    private bool _useGravity = true;
    private Material _material;
    private PlayerStats _stats;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();

        if (NetworkClient.active) return;

        if (_statType != StatType.None) SetStatType(_statType);
    }
    public override void OnStartClient() {
        base.OnStartClient();
        SetStatType(_statType);
    }

    public void SetStatType(StatType statType)
    {
        _statType = statType;
        var pickupStats = ResourceManager.GetPickupInfo(_statType);
        _stats = pickupStats.playerStats;
        _material = GetComponent<MeshRenderer>().material;
        _material.mainTexture = pickupStats.texture;
    }

    public void AddForce(Vector3 force)
    {
        _body.AddForce(force * Time.fixedDeltaTime);
    }

    // Gives pickup to player instead of otherway around to prevent edge-case
    private void FixedUpdate()
    {
        if (_useGravity)
        {
            AddForce(Vector3.down * _gravityScale);
        }

        var hits = Physics.SphereCastAll(
            transform.position,
            _hitboxRadius,
            Vector3.up,
            0,
            _playerLayer
        );

        if (hits.Length > 0)
        {
            float closestDistance = hits[0].distance;
            Player closestPlayer = hits[0].collider.GetComponent<Player>();
            for (int i = 1; i < hits.Length; i++)
            {
                if (hits[i].distance < closestDistance)
                {
                    closestDistance = hits[i].distance;
                    closestPlayer = hits[i].collider.GetComponent<Player>();
                }
            }
            closestPlayer.AddStats(_stats);  // TODO: Animation
            if (NetworkClient.active && isServer) DestroyObject();
            else if (!NetworkClient.active) Destroy(gameObject);
        }
    }

    [Unity.Netcode.ServerRpc]
    private void DestroyObject() {
        NetworkServer.Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == Mathf.Log(_groundLayer.value, 2))
        {
            _useGravity = false;
            _body.velocity = Vector3.zero;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _hitboxRadius);
    }
}
