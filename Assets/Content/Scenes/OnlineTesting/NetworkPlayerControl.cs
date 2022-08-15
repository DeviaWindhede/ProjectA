using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerControl : NetworkBehaviour {
    [SerializeField]
    private float walkSpeed = 3.5f;

    [SerializeField]
    private Vector2 defaultPositionRange = new Vector2(-4, 4);

    [SerializeField]
    private NetworkVariable<float> forwardBackPosition = new NetworkVariable<float>();

    [SerializeField]
    private NetworkVariable<float> leftRightPosition = new NetworkVariable<float>();

    // client cashing
    private float oldForwardBackPosition;
    private float oldLeftRightPosition;

    private void Start() {
        transform.position = new Vector3(Random.Range(defaultPositionRange.x, defaultPositionRange.y), 0, Random.Range(defaultPositionRange.x, defaultPositionRange.y));
    }

    private void Update() {
        if (NetworkManager.Singleton.IsServer) {
            UpdateServer();
        }

        if (IsClient && IsOwner) {
            UpdateClient();
        }
    }
    private void UpdateServer() {
        transform.position = new Vector3(transform.position.x + leftRightPosition.Value, transform.position.y, transform.position.z + forwardBackPosition.Value);
    }

    private void UpdateClient() {
        float forwardBackward = 0;
        float leftRight = 0;

        int verticalDir = (UnityEngine.Input.GetKey(KeyCode.W) ? 1 : 0) + (UnityEngine.Input.GetKey(KeyCode.S) ? -1 : 0);
        forwardBackward += verticalDir * walkSpeed * Time.deltaTime;

        int horizontalDir = (UnityEngine.Input.GetKey(KeyCode.D) ? 1 : 0) + (UnityEngine.Input.GetKey(KeyCode.A) ? -1 : 0);
        leftRight += horizontalDir * walkSpeed * Time.deltaTime;

        if (oldForwardBackPosition != forwardBackward || oldLeftRightPosition != leftRight) {
            oldForwardBackPosition = forwardBackward;
            oldLeftRightPosition = leftRight;

            // update the server
            UpdateClientPositionServerRpc(forwardBackward, leftRight);
        }
    }

    [ServerRpc] // executed server-side
    private void UpdateClientPositionServerRpc(float forwardBackward, float leftRight) {
        forwardBackPosition.Value = forwardBackward;
        leftRightPosition.Value = leftRight;
    }
}