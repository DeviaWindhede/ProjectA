using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Implement custom editor
public class Pickupable : MonoBehaviour
{
    // TODO: Implement powerups and ability pickups

    [SerializeField]
    private PlayerStats _stats;

    [SerializeField]
    private float _hitboxRadius = 1f;

    [SerializeField]
    private LayerMask _playerLayer;

    // Gives pickup to player instead of otherway around to prevent edge-case
    private void FixedUpdate()
    {
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
            for (int i = 1; i < hits.Length; i++) {
                if (hits[i].distance < closestDistance) {
                    closestDistance = hits[i].distance;
                    closestPlayer = hits[i].collider.GetComponent<Player>();
                }
            }
            closestPlayer.UpdatePlayerStats(_stats);
            Destroy(gameObject); // TODO: Animation
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _hitboxRadius);
    }
}
