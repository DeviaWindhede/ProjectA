using UnityEngine;
using static PlayerController;

public class PlayerAirBorneState : PlayerState {
    private Timer _airBorneTimer;
    private Timer _groundedCooldownTimer = new Timer(0.2f);
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
            if (controller.countAsGroundHit && HelperFunctions.IsSurfaceClimbable(controller.groundHitInfo.normal, Vector3.up, data.maxClimbableSlopeAngle)) {
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
                + (1 - data.lookAirMaxRotationalBasedSpeedMultiplier) * (1 - airVerticalReductionAngle); // TODO: Add variable to this
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
        airVerticalReductionAngle = angle / reductionAngle;
        float reductionMultiplier =
            data.lookAirMaxRotationalBasedSpeedMultiplier
            + (1 - data.lookAirMaxRotationalBasedSpeedMultiplier) * airVerticalReductionAngle;
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
