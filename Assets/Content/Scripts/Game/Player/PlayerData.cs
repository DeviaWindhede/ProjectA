using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour {
    public delegate void PlayerDataUpdate();
    public event PlayerDataUpdate OnStatUpdate;

    public PlayerInputValues input = new PlayerInputValues();

    [SerializeField] private PlayerStats _stats;
    public PlayerStats Stats {
        get { return _stats; }
        set { 
            _stats = value;
            OnStatUpdate();
        }
    }

    [Min(0)] public float maxAirTime = 0.75f;
    [Min(0)] public float maxClimbableSlopeAngle = 80;

    [Header("Velocity")]
    [Min(0f)] public float secondsToReachFullGroundSpeed = 1f;
    [Min(0.01f)] public float secondsToReachFullAirSpeed = 0.3f;
    [Min(0f)] public float maxForwardGroundSpeed = 600f;
    [Min(0.01f)] public float maxForwardAirSpeed = 750f;
    [Min(0f)] public float gravityScale = 2.5f;
    [Min(0.01f)] public float speedCorrectionFactor = 10;

    [Header("Rotation")]
    [Min(0f)] public float lookGroundedRotationDegsPerSecond = 50;
    [Min(0f)] public float lookAirHorizontalRotationDegsPerSecond = 160;
    [Min(0f)] public float lookAirVerticalRotationDegsPerSecond = 80;
    [Range(0f, 90f)] public float lookAirRollRotationMaxRotationAngle = 45;
    [Range(0f, 1f)] public float lookAirMaxRotationalBasedSpeedMultiplier = 0.3f;
    [Min(0)] public float rideRotationSpeed = 4;
    [Min(0)] public float chargeRotationSpeedExtra = 70;
    [Min(0.01f)] public float maxTurnMagnitude = 3;
    //[Min(0)] public float _airRotationSpeed = 50f; // TODO
    [Range(0f, 80f)] public float maxAirborneAngle = 80;
    [Range(0f, 80f)] public float minAirborneAngle = 55;
    [Min(0)] public float followGroundRotationAnglePerSecond = 110f;
    [Min(0)] public float airRollRotationAnglePerSecond = 360f;

    [Header("Charge")]
    [Min(0)] public float _chargeTime = 1.5f;
    [Min(0)] public float chargeTimeToStopWhenTurningPercentageDenominator = 20f;
    [Min(0)] public float _chargeExpirationTime = 2f;
    [Min(0)] public float _chargeBurnoutTime = 3f;
    [Min(0)] public float boostSpeed = 1000f;
    [Min(0.01f)] public float groundBreakSpeed = 1f;
    [Min(0f)] public float passiveAirChargeGain = 0.1f;
}
