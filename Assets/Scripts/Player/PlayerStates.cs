using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static PlayerController;
using static HelperFunctions;

public class PlayerStates {

}

public class PlayerState {
    public virtual void OnEnter(PlayerController controller) { }
    public virtual void OnUpdate(PlayerController controller, PlayerData data) { }
    public virtual void OnExit(PlayerController controller) { }
}

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

public class PlayerAirBorneState : PlayerState {
    private Timer _airBorneTimer;
    private Timer _groundedCooldownTimer = new Timer(0.2f);
    private float airRollRotationAnglePerSecond = 360f;
    private float airVerticalReductionAngle;

    public PlayerAirBorneState(PlayerController controller, PlayerData data) {
        InitializeAirborneTimer(controller, data);

        data.OnStatUpdate += () => {
            float time = _airBorneTimer.Time;
            InitializeAirborneTimer(controller, data);
            _airBorneTimer += time;
        };
    }

    private void InitializeAirborneTimer(PlayerController controller, PlayerData data) {
        _airBorneTimer = new Timer(
            data.maxAirTime * controller.GlideMultiplier + data.Stats.Glide / 2
        );
    }

    public override void OnEnter(PlayerController controller) {
        var euler = controller.MeshRotation.eulerAngles;
        controller.verticalRotation = Quaternion.Euler(euler.x, 0, 0);
        controller.rollRotation = Quaternion.Euler(0, 0, euler.z);
        controller.gravitySpeed = 0;
        controller.velocityDirection = Vector3.zero;

        controller.lastNormal = Vector3.zero;
        controller.currentNormal = Vector3.zero;
        _groundedCooldownTimer.Reset();
    }

    public override void OnExit(PlayerController controller) {
        _airBorneTimer.Reset();
        _groundedCooldownTimer.Reset();
    }

    public override void OnUpdate(PlayerController controller, PlayerData data) {
        // Transition
        float time = Time.fixedDeltaTime;
        _groundedCooldownTimer.Time += time; // Cooldown until player is able to touch ground again
        if (_groundedCooldownTimer.Expired) {
            if (controller.countAsGroundHit && controller.IsSurfaceClimbable(controller.groundHitInfo.normal, Vector3.up)) {
                controller.CurrentState = PlayerPhysicsState.Grounded;
                return;
            }
        }

        // Update
        HandleRotation(controller, data, time);
        controller.velocityDirection = controller.vecForward;

        // Speed acceleration
        // TODO: Make acceleration non-linear
        float reductionMultiplier = 1;
        if (controller.velocityDirection.y > 0) {
            reductionMultiplier =
                data.lookAirMaxRotationalBasedSpeedMultiplier
                + (1 - data.lookAirMaxRotationalBasedSpeedMultiplier) * (1 - controller.airVerticalReductionAngle); // TODO: Add variable to this
        }

        float maxSpeed = data.maxForwardAirSpeed * controller.WeightSpeedMultiplier * controller.TopSpeedMultiplier * reductionMultiplier;
        var acceleration = time / data.secondsToReachFullAirSpeed * maxSpeed;
        controller.speed += acceleration;

        if (controller.speed > maxSpeed) {
            controller.speed = Mathf.MoveTowards(controller.speed, maxSpeed, acceleration * data.speedCorrectionFactor);
        }

        Vector3 finalVelocity = controller.velocityDirection.normalized * controller.speed * time;

        finalVelocity += Vector3.up * controller.GlideMultiplier * time;
        finalVelocity += Vector3.down * controller.WeightGlideMultiplier * time;

        _airBorneTimer += time;
        if (controller.useGravity && _airBorneTimer.Expired) {
            controller.gravitySpeed += data.gravityScale;

            // Counteracts upwards velocity
            if (Mathf.Sign(controller.velocityDirection.normalized.y) == 1)
                finalVelocity += Vector3.down * finalVelocity.y;

            finalVelocity += Vector3.down * controller.gravitySpeed * time;
        }

        OnCharge(controller, data);
        finalVelocity += controller.chargeForce * time;

        controller.body.velocity = finalVelocity;
        controller.chargeForce = Vector3.zero;
    }

    private void OnCharge(PlayerController controller, PlayerData data) {
        float time = Time.fixedDeltaTime;
        if (data.input.isCharging) {
            controller.chargeForce = Vector3.down * controller.speed + Vector3.down * data.gravityScale * 2;
        }

        // Timer
        controller.chargeTimer += data.passiveAirChargeGain * time;  // Unexpirable charge gain
        controller.chargeRatio = controller.chargeTimer.Ratio;
        controller.expirationTimer.Reset();

        // TODO
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

    private void HandleRotation(PlayerController controller, PlayerData data, float time) {
        // Vertical Rotation
        Vector3 vertRotationAmount = Vector3.right * data.lookAirVerticalRotationDegsPerSecond * data.input.direction.y * time;
        Quaternion deltaPitchRotation = Quaternion.Euler(vertRotationAmount);
        float angle = 90 - Vector3.Angle(controller.verticalRotation * deltaPitchRotation * Vector3.forward, Vector3.up);

        // Faster decent angle
        if (angle <= 0 && Mathf.Sign(data.input.direction.y) == 1)
            deltaPitchRotation = Quaternion.Euler(vertRotationAmount * 2f);

        // Angle cap
        if (angle <= -data.minAirborneAngle)
            controller.verticalRotation = Quaternion.Euler(Vector3.right * data.minAirborneAngle);
        else if (angle >= data.maxAirborneAngle)
            controller.verticalRotation = Quaternion.Euler(Vector3.right * -data.maxAirborneAngle);
        else
            controller.verticalRotation = controller.verticalRotation * deltaPitchRotation;

        // Horizontal Rotation
        float reductionAngle = Mathf.Sign(angle) >= 0 ? data.maxAirborneAngle : -data.minAirborneAngle;
        controller.airVerticalReductionAngle = angle / reductionAngle;
        float reductionMultiplier =
            data.lookAirMaxRotationalBasedSpeedMultiplier
            + (1 - data.lookAirMaxRotationalBasedSpeedMultiplier) * controller.airVerticalReductionAngle;
        Quaternion horizontalDelta = Quaternion.Euler(
            Vector3.up
                * data.lookAirHorizontalRotationDegsPerSecond
                * reductionMultiplier
                * controller.TurnMultiplier
                * time
                * data.input.direction.x
        );
        controller.horizontalRotation = controller.horizontalRotation * horizontalDelta;

        // Roll Rotation
        // TODO: Make roll rotation dependent on air friction and torque
        controller.rollRotation = Quaternion.RotateTowards(
            controller.rollRotation,
            Quaternion.Euler(
                Vector3.forward
                    * data.lookAirRollRotationMaxRotationAngle
                    * -data.input.direction.x
                    * reductionMultiplier
            ),
            data.airRollRotationAnglePerSecond * time
        );

        controller.finalRotation = controller.horizontalRotation * controller.verticalRotation * controller.rollRotation;
        controller.vecForward = controller.finalRotation * Vector3.forward;
    }
}
