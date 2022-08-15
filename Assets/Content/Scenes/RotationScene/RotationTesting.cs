using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationTesting : MonoBehaviour
{
    InputActions inputActions;
    public Vector2 direction;
    [Min(0)] public float rotationSpeed = 70;
    Quaternion _horizontalRotation;

    // Start is called before the first frame update
    void Start()
    {
        inputActions = new InputActions();
        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Move.performed += ctx => direction = ctx.ReadValue<Vector2>();

        this._horizontalRotation = Quaternion.Euler(
            Vector3.up * this.transform.rotation.eulerAngles.y
        );
        this._finalRotation = transform.rotation;
        velocityDirection = _finalRotation * Vector3.forward;
    }

    Quaternion _finalRotation;
    public float lookRotationDegsPerSecond;
    private Vector3 finalVelocityDirection;
    void Update() {// Horizontal Rotation
        float time = Time.deltaTime;
        // Vector3 force = _finalRotation * Vector3.forward;

        // Quaternion horizontalDelta = Quaternion.Euler(
        //     Vector3.up * direction.x * rotationSpeedDegPerSecond * time * Mathf.Deg2Rad
        // );
        Quaternion horizontalDelta = Quaternion.Euler(
            Vector3.up * lookRotationDegsPerSecond * time * direction.x
        );
        this._horizontalRotation = this._horizontalRotation * horizontalDelta;
        this._finalRotation = _horizontalRotation;
        Vector3 force = _horizontalRotation * Vector3.forward;
        float dot = Vector3.Dot(force.normalized, velocityDirection.normalized);
        if (dot >= 1 - 0.0001f && rotationSpeed > 0) {
            velocityDirection = force.normalized;
        }
        else {
            Vector3 vectorA = velocityDirection;
            Vector3 vectorB = force;
            float angle = Vector3.Angle(vectorA, vectorB);
            Vector3 cross = Vector3.Cross(vectorA, vectorB);
            if (cross.y < 0) angle = -angle;
            Quaternion extra = Quaternion.Euler(
                Vector3.up * 45 / (0.01f + rotationSpeed) * Mathf.Sign(angle)
            );
            velocityDirection += _horizontalRotation * extra * Vector3.forward * rotationSpeed * time;
        }
        if (velocityDirection.magnitude > maxForwardSpeed) {
            velocityDirection = velocityDirection.normalized * maxForwardSpeed;
        }
    }

    Vector3 velocityDirection = Vector3.zero;
    public float maxForwardSpeed = 300;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(Vector3.zero, velocityDirection);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(Vector3.zero, _finalRotation * Vector3.forward);

        Gizmos.color = Color.black;
        Quaternion horizontalDelta = Quaternion.Euler(
            Vector3.up * 45 * Mathf.Sign(direction.x)
        );
        Gizmos.DrawRay(Vector3.zero, _horizontalRotation * horizontalDelta * Vector3.forward);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(Vector3.zero, finalVelocityDirection);
    }
}
