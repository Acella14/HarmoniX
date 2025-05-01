using System;
using UnityEngine;

public static class default_Models {
    #region - Player -
    public enum PlayerStance {
        Stand,
        Crouch
    }

    [Serializable]
    public class PlayerSettingsModel {
        [Header("View Settings")]
        public float ViewXSensitivity;
        public float ViewYSensitivity;
        public bool ViewXInverted;
        public bool ViewYInverted;

        [Header("Movement Settings")]
        public bool SprintingHold;
        public float MovementSmoothing;

        [Header("Movement - Walking")]
        public float WalkingForwardSpeed;
        public float WalkingBackwardSpeed;
        public float WalkingStrafeSpeed;
        
        [Header("Movement - Running")]
        public float RunningForwardSpeed;
        public float RunningStrafeSpeed;

        [Header("Jumping")]
        public int MaxAirJumps = 0;
        public float FallingSmoothing;
        [Range(0f, 100f)] public float JumpMovementInfluence = 1f;
        [Range(0f, 5f)] public float JumpTiltIntensity = 1f;
        [Range(0f, 10f)] public float JumpMomentumDecayRate = 3f;


        [Header("Speed Effectors")]
        public float SpeedEffector = 1;
        public float CrouchSpeedEffector;
        public float FallingSpeedEffector;
        [Range(1f, 5f)] public float GravityFallMultiplier = 2.5f;

    }

    [Serializable]  
    public class CharacterStance {
        public float CameraHeight;
        public CapsuleCollider StanceCollider;
    }
    
    #endregion

    #region - Weapons -

    [Serializable]
    public class WeaponSettingsModel
    {
        [Header("Weapon Sway")]
        public float SwayAmount;
        public float SwaySmoothing;
        public float SwayResetSmoothing;
        public float SwayClampX;
        public float SwayClampY;
        public bool SwayYInverted;
        public bool SwayXInverted;

        [Header("Weapon Movement Sway")]
        public float MovementSwayX;
        public float MovementSwayY;
        public float MovementSwaySmoothing;
        public bool MovementSwayYInverted;
        public bool MovementSwayXInverted;
    }

    #endregion
}
