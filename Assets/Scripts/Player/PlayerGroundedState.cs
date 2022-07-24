using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static PlayerController;

public class PlayerGroundedState : PlayerState {

    public PlayerGroundedState(PlayerController controller, PlayerData data) {
    }
    public override void OnEnter(PlayerController controller) {
        controller.velocityDirection.y = 0;
        controller.gravitySpeed = 0;
        controller.velocityDirection = Vector3.zero;
        controller.verticalRotation = Quaternion.identity;
    }
    public override void OnExit(PlayerController controller) {
        controller.lastNormal = Vector3.zero;
        controller.currentNormal = Vector3.zero;
    }
    public override void OnUpdate(PlayerController controller, PlayerData data) {
        // Transition
        if (!controller.groundHit) {
            controller.CurrentState = PlayerPhysicsState.Airborne;
            return;
        }
        else {
            if (controller.lastNormal != controller.currentNormal) // currentNormal will always have a value when groundHit is truthy
            {
                // entering here means the player is trying to move from a surface to a new one or was flying and has now hit a surface
                Vector3 vec = controller.lastNormal == Vector3.zero ? Vector3.up : controller.lastNormal; // If first time touching ground
                if (!controller.IsSurfaceClimbable(controller.groundHitInfo.normal, vec)) {
                    controller.CurrentState = PlayerPhysicsState.Airborne;
                    return;
                }
            }
        }

        HandleGroundedRotation(controller, data);

        float time = Time.fixedDeltaTime;

        // Horizontal Velocity
        Vector3 horizontalDirection = controller.horizontalRotation * Vector3.forward;
        Quaternion rotationExtra = Quaternion.Euler(
            Vector3.up
                * 45
                / (0.01f + data.rideRotationSpeed)
                * Mathf.Sign(controller.GetNegativeAngle(controller.velocityDirection, horizontalDirection))
        );
        controller.velocityDirection +=
            controller.horizontalRotation
            * rotationExtra
            * Vector3.forward
            * data.rideRotationSpeed
            * controller.WeightTurnMultiplier
            * time;
        if (controller.velocityDirection.magnitude > data.maxTurnMagnitude) {
            controller.velocityDirection = controller.velocityDirection.normalized * data.maxTurnMagnitude;
        }

        // Speed acceleration
        // TODO: Make acceleration non-linear
        float maxSpeed = data.maxForwardGroundSpeed * controller.WeightSpeedMultiplier * controller.TopSpeedMultiplier;
        var acceleration = time / data.secondsToReachFullGroundSpeed * maxSpeed;
        controller.speed += acceleration;
        if (controller.speed > maxSpeed) {
            controller.speed = Mathf.MoveTowards(controller.speed, maxSpeed, acceleration * data.speedCorrectionFactor);
        }

        // TODO: Implement breaking (_data.input.isBreaking)

        OnCharge(controller, data);

        Vector3 finalVelocity = controller.verticalRotation * controller.velocityDirection.normalized * controller.speed * time;
        controller.body.velocity = finalVelocity;
    }

    private void OnCharge(PlayerController controller, PlayerData data) {
        float time = Time.fixedDeltaTime;
        if (data.input.isCharging) {
            var acceleration =
                time
                / data.secondsToReachFullGroundSpeed
                * data.maxForwardGroundSpeed
                * controller.WeightSpeedMultiplier
                * controller.TopSpeedMultiplier;
            // TODO: create acceleration member variable
            controller.speed -= acceleration; // Remove ground speed addition

            Vector3 horizontalDirection = controller.horizontalRotation * Vector3.forward;

            float dot = Mathf.Clamp01(
                Vector3.Dot(horizontalDirection.normalized, controller.velocityDirection.normalized)
            );
            float stopOnTurn = 1 + data.chargeTimeToStopWhenTurningPercentageDenominator * (1 - dot);
            float maxDelta = acceleration * data.groundBreakSpeed / stopOnTurn * controller.WeightChargeMultiplier;

            controller.speed = Mathf.MoveTowards(controller.speed, 0, maxDelta);
        }
        else if (controller.chargeRatio != 0) {
            float boostSpeed = data.boostSpeed * Mathf.Clamp01(controller.chargeRatio) * controller.BoostMultiplier;

            float limitMultiplier = 1.5f;
            float maxSpeed = data.maxForwardGroundSpeed * controller.WeightSpeedMultiplier * controller.TopSpeedMultiplier;
            if (controller.speed <= maxSpeed * limitMultiplier)
                controller.speed += boostSpeed;

            controller.velocityDirection = controller.horizontalRotation * Vector3.forward;
        }

        // Timer
        if (data.input.isCharging && !controller.expirationTimer.Expired) // When you can charge
        {
            controller.chargeTimer += time * controller.ChargeMultiplier;
            controller.chargeRatio = controller.chargeTimer.Ratio;
            if (controller.chargeTimer.Expired) controller.expirationTimer += time;
        }
        else if (controller.chargeRatio != 0) // On release
        {
            controller.chargeRatio = 0;
            controller.chargeBurnoutTimer.Reset();
            controller.expirationTimer.Reset();
            controller.chargeTimer.Reset();
        }

        if (controller.expirationTimer.Expired) {
            controller.chargeRatio = 0;
            controller.chargeBurnoutTimer += time;
            if (controller.chargeBurnoutTimer.Expired) {
                controller.chargeBurnoutTimer.Reset();
                controller.expirationTimer.Reset();
                controller.chargeTimer.Reset();
            }
        }
    }

    private int rotationRayCount = 10;
    private void HandleGroundedRotation(PlayerController controller, PlayerData data) {
        float time = Time.fixedDeltaTime;
        // Horizontal Rotation
        Quaternion horizontalDelta = Quaternion.Euler(
            Vector3.up
                * data.lookGroundedRotationDegsPerSecond
                * controller.TurnMultiplier
                * time
                * data.input.direction.x
                + Vector3.up
                    * data.chargeRotationSpeedExtra
                    * time
                    * data.input.direction.x
                    * (data.input.isCharging ? 1 : 0)
        );
        controller.horizontalRotation = controller.horizontalRotation * horizontalDelta;

        Vector3 averageNormal = controller.groundHitInfo.normal;
        float angleIncrement = 360f / controller.rotationRayCount;
        int incrementCount = 1;
        for (int i = 0; i < controller.rotationRayCount; i++) {
            RaycastHit rayData;
            var offset =
                controller.finalRotation * Quaternion.Euler(0, -angleIncrement * i, 0) * Vector3.forward;
            if (
                Physics.Raycast(
                    controller.transform.position + offset,
                    Vector3.down,
                    out rayData,
                    controller.groundRayDistance + controller.groundRotationRayExtraDistance,
                    controller.collidableLayer
                )
            ) {
                Vector3 vec = controller.currentNormal == Vector3.zero ? Vector3.up : controller.currentNormal; // If first time touching ground
                if (controller.IsSurfaceClimbable(rayData.normal, vec)) {
                    averageNormal += rayData.normal;
                    incrementCount++;
                }
            }
        }
        averageNormal /= incrementCount;

        Vector3 upVec = controller.groundHit ? averageNormal : Vector3.up;
        // TODO: Increase speed the larger the angle diff is
        controller.verticalRotation = Quaternion.RotateTowards(
            controller.verticalRotation,
            Quaternion.FromToRotation(Vector3.up, upVec),
            data.followGroundRotationAnglePerSecond * time
        );
        controller.finalRotation = controller.verticalRotation * controller.horizontalRotation;
        controller.vecForward = controller.finalRotation * Vector3.forward;
    }
}

