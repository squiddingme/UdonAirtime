﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Airtime.Input;
using Airtime.Track;

namespace Airtime.Player.Movement
{
    public class PlayerController : UdonSharpBehaviour
    {
        private const float ZERO_GRAVITY = 0.0001f;
        private const float AERIAL_TIME = 0.02f;

        [Header("Dependencies")]
        [Tooltip("InputManager component used for rebinding")] public PlayerInputManager inputManager;
        [Tooltip("BezierWalker component required to compute positions when rail grinding")] public BezierWalker walker;

        [Header("VRC Player Settings")]
        [Tooltip("Walking speed")] public float walkSpeed = 2.0f;
        [Tooltip("Running speed i.e. speed at maximum analog stick input")] public float runSpeed = 4.0f;
        [Tooltip("Strafing speed")] public float strafeSpeed = 2.0f;
        [Tooltip("Default jump impulse")] public float jumpImpulse = 3.0f;
        [Tooltip("Gravity strength multiplier")] public float gravityStrength = 1.0f;

        [Header("Movement")]
        [Tooltip("Enable accelerating up to maximum speed")] public bool accelerationEnabled = false;
        [Tooltip("Rate to accelerate up to maximum speed.")] public float accelerationRate = 2.0f;
        [Tooltip("Rate the player loses accelerated speed when they have stopped.")] public float accelerationLossRate = 10.0f;
        [Tooltip("Minimum speed (as percentage of maximum speed) as soon as the player starts moving.")] [Range(0.0f, 1.0f)] public float accelerationMinimum = 0.2f;

        [Header("Custom Jump Properties")]
        [Tooltip("Extra time (in seconds) the player can hold the jump button for a higher jump")] public float bonusJumpTime = 0.0f;
        [Tooltip("Extra time after dropping off a platform that the player can still jump (coyote time)")] public float ledgeJumpTime = 0.1f;

        [Header("Double Jump Properties")]
        [Tooltip("Allow double jumping")] public bool doubleJumpEnabled = false;
        [Tooltip("Force applied to 2nd jump")] public float doubleJumpImpulse = 3.0f;
        [Tooltip("If touching the ground resets the double jump.")] public bool groundResetsDoubleJump = true;
        [Tooltip("If jumping from a wall resets the double jump.")] public bool wallResetsDoubleJump = true;
        [Tooltip("If jumping from a grind rail resets the double jump.")] public bool grindingResetsDoubleJump = true;

        [Header("Shared Wall Ride & Jump Properties")]
        [Tooltip("Size of capsule detection to scan for walls")] public float wallDetectionSize = 0.15f;
        [Tooltip("How far we can be from a wall and still count it as detected")] public float wallDetectionDistance = 0.5f;
        [Tooltip("Layers you can wall jump from")] public LayerMask wallLayers;

        [Header("Wall Ride Properties")]
        [Tooltip("Allow wall jumping")] public bool wallRideEnabled = false;
        [Tooltip("Amount the analogue stick must be pushed to trigger wallriding")] public float wallRideDeadzone = 0.4f;
        [Tooltip("Angle of analogue stick from wall that triggers wallriding")] [Range(0.0f, 360.0f)] public float wallRideAcquireAngle = 70.0f;
        [Tooltip("Angle of analogue stick from wall that holds a wallride, lets the player be sloppy with their input")] [Range(0.0f, 360.0f)] public float wallRideMaintainAngle = 110.0f;
        [Tooltip("Decceleration rate of falling speed during a wallride")] public float wallRideFriction = 10.0f;
        [Tooltip("Maximum falling speed while wallriding")] public float wallRideFallSpeed = 1.0f;
        [Tooltip("Slope normal that is tolerated as a wallride")] public float wallRideSlopeTolerance = 0.15f;

        [Header("Wall Jump Properties")]
        [Tooltip("Allow wall jumping. If wall riding is also on, wall jumping will be a part of the wallriding mechanic")] public bool wallJumpEnabled = false;
        [Tooltip("Force to push player away from wall when wall jumping")] public float wallJumpForce = 4.0f;
        [Tooltip("Force applied to wall jump")] public float wallJumpImpulse = 3.0f;
        [Tooltip("How much time after leaving a wall that we're still able to wall jump for")] public float wallJumpTime = 0.2f;
        [Tooltip("Time before a walljump can happen again, can be used to slow down wall jumping off the same wall")] public float wallJumpCooldown = 0.6f;

        [Header("Grind Properties")]
        [Tooltip("Allow rail grinding")] public bool grindingEnabled = false;
        [Tooltip("Acceleration rate towards maximum grind speed")] public float grindAcceleration = 10.0f;
        [Tooltip("Maximum speed of a grind")] public float grindMaxSpeed = 5.0f;
        [Tooltip("Speed when braking with the analogue stick")] public float grindBrakeSpeed = 2.5f;
        [Tooltip("Jump impulse from rails")] public float grindJumpImpulse = 4.0f;
        [Tooltip("Speed threshold to determine if grind direction is decided by momentum, or direction of player")] public float grindMomentumThreshold = 1.0f;
        [Tooltip("Grace period after jumping before allowing grinding again")] public float grindJumpCooldown = 0.8f;
        [Tooltip("Grace period after reaching the end of a rail before allowing grinding again")] public float grindFallCooldown = 0.2f;
        [Tooltip("Force of analog stick before braking")] [Range(0.0f, 1.1f)] public float grindSlowDeadzone = 0.1f;
        [Tooltip("Force of analog stick before switching directions")] [Range(0.0f, 1.1f)] public float grindTurnDeadzone = 0.9f;
        [Tooltip("Wait period before you can turn around, use to prevent network spamming")] public float grindTurnCooldown = 0.2f;
        [Tooltip("Angle of analog stick direction to switch directions")] [Range(0.0f, 360.0f)] public float grindTurnAngle = 120.0f;
        [Tooltip("If we exceed this distance, deem the player 'stuck' and teleport them to where they're supposed to be")] public float grindTeleportDistance = 10.0f;
        [Tooltip("Grinding temporarily disables the rail game object. Usually desired, since you can use it to disable colliders.")] public bool grindingDisablesRail = true;

        [Header("Track Properties")]
        [Tooltip("Distance from target track position before player is fully snapped to rail")] public float trackSnapEpsilon = 0.04f;
        [Tooltip("Speed to snap the player to the target rail position")] public float trackSnapSpeed = 35.0f;

        // VRC Stuff
        private VRCPlayerApi localPlayer;
        private bool localPlayerCached = false;
        private Vector3 localPlayerPosition = Vector3.zero;
        private Quaternion localPlayerRotation = Quaternion.identity;
        private Vector3 localPlayerVelocity = Vector3.zero;
        private Vector3 localPlayerCenter = Vector3.up * 0.5f;
        private Vector3 localPlayerCapsuleA = Vector3.up * 0.25f;
        private Vector3 localPlayerCapsuleB = Vector3.up * 1.5f;

        // Player States
        public const int STATE_GROUNDED = 0;
        public const int STATE_AERIAL = 1;
        public const int STATE_WALLRIDE = 2;
        public const int STATE_SNAPPING = 3;
        public const int STATE_GRINDING = 4;
        private int playerState = STATE_AERIAL; // start on aerial

        // Player Input
        private Vector3 input3D = Vector3.zero;
        private bool inputJumped = false;
        private bool inputDoubleJumped = true;
        private bool inputTurned = false;

        // STATE_GROUNDED
        private float accelerationMultiplier = 0.0f;

        // STATE_AERIAL
        private float aerialTime = 0.0f;
        private float bonusJumpTimeRemaining = 0.0f;
        private float ledgeJumpTimeRemaining = 0.0f;

        // STATE_AERIAL & STATE_WALLRIDE
        private RaycastHit wallHit = new RaycastHit();
        private float wallJumpTimeRemaining = 0.0f;
        private float wallJumpCooldownRemaining = 0.0f;

        // STATE_GRINDING
        private float previousTrackPosition = 0.0f;
        private float trackSpeed = 0.0f;
        private Vector3 currentTrackVelocity = Vector3.forward;
        private Quaternion currentTrackOrientation = Quaternion.identity;
        private float grindingCooldownRemaining = 0.0f;
        private float grindingTurnCooldownRemaining = 0.0f;
        private Vector3 lastTarget = Vector3.zero;

        // Events
        public const int EVENT_JUMP_DOUBLE = 2;
        public const int EVENT_JUMP_WALL = 4;
        public const int EVENT_GRIND_START = 8;
        public const int EVENT_GRIND_STOP = 16;
        private int eventFlags = 0;

        public void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                localPlayerCached = true;
            }
        }

        public void Update()
        {
            if (localPlayerCached)
            {
                // reset all event flags
                eventFlags = 0;

                input3D = inputManager.GetDirection3D();

                localPlayerPosition = localPlayer.GetPosition();
                localPlayerRotation = localPlayer.GetRotation();
                localPlayerVelocity = localPlayer.GetVelocity();

                switch (playerState)
                {
                    case STATE_GROUNDED:
                        PlayerStateGroundedUpdate();
                        break;
                    case STATE_AERIAL:
                        PlayerStateAerialUpdate();
                        break;
                    case STATE_WALLRIDE:
                        PlayerStateWallrideUpdate();
                        break;
                    case STATE_SNAPPING:
                        PlayerStateSnappingUpdate();
                        break;
                    case STATE_GRINDING:
                        PlayerStateGrindingUpdate();
                        break;
                    default:
                        Debug.LogError(string.Format("PlayerController {0} is set to non-existent state {1}", gameObject.name, playerState));
                        playerState = STATE_AERIAL;
                        break;
                }
            }
        }

        private void ApplyPlayerProperties()
        {
            localPlayer.SetWalkSpeed(walkSpeed);
            localPlayer.SetRunSpeed(runSpeed);
            localPlayer.SetStrafeSpeed(strafeSpeed);
            localPlayer.SetJumpImpulse(jumpImpulse);
        }

        private void ApplyPlayerPropertiesWithAcceleration()
        {
            localPlayer.SetWalkSpeed(walkSpeed * accelerationMultiplier);
            localPlayer.SetRunSpeed(runSpeed * accelerationMultiplier);
            localPlayer.SetStrafeSpeed(strafeSpeed * accelerationMultiplier);
            localPlayer.SetJumpImpulse(jumpImpulse);
        }

        private void RemovePlayerProperties()
        {
            localPlayer.SetWalkSpeed(0f);
            localPlayer.SetRunSpeed(0f);
            localPlayer.SetStrafeSpeed(0f);
            localPlayer.SetJumpImpulse(0f);
        }

        public int GetPlayerState()
        {
            return playerState;
        }

        private void SetPlayerState(int state)
        {
            switch (playerState)
            {
                case STATE_GROUNDED:
                    //PlayerStateGroundedEnd();
                    break;
                case STATE_AERIAL:
                    //PlayerStateAerialEnd();
                    break;
                case STATE_WALLRIDE:
                    PlayerStateWallrideEnd();
                    break;
                case STATE_SNAPPING:
                    PlayerStateSnappingEnd();
                    break;
                case STATE_GRINDING:
                    PlayerStateGrindingEnd();
                    break;
            }

            playerState = state;

            switch (playerState)
            {
                case STATE_GROUNDED:
                    PlayerStateGroundedStart();
                    break;
                case STATE_AERIAL:
                    PlayerStateAerialStart();
                    break;
                case STATE_WALLRIDE:
                    PlayerStateWallrideStart();
                    break;
                case STATE_SNAPPING:
                    PlayerStateSnappingStart();
                    break;
                case STATE_GRINDING:
                    PlayerStateGrindingStart();
                    break;
            }
        }

        private void PlayerStateGroundedStart()
        {
            ApplyPlayerProperties();
            localPlayer.SetGravityStrength(gravityStrength);

            // set coyote time
            ledgeJumpTimeRemaining = ledgeJumpTime;

            // set bonus jump time
            bonusJumpTimeRemaining = bonusJumpTime;

            // reset some cooldowns if we touched the ground
            grindingCooldownRemaining = 0.0f;
            wallJumpCooldownRemaining = 0.0f;

            // reset double jump
            inputDoubleJumped = !groundResetsDoubleJump;

            // set acceleration multiplier based on the velocity we arrived at
            if (accelerationEnabled)
            {
                float velocity = localPlayerVelocity.magnitude;
                if (velocity > 0.0f)
                {
                    accelerationMultiplier = Mathf.Clamp(velocity / runSpeed, accelerationMinimum, 1.0f);
                }
            }
        }

        private void PlayerStateGroundedUpdate()
        {
            // switch to aerial state
            if (!localPlayer.IsPlayerGrounded())
            {
                // this is used because grounded flag is broken on slopes
                aerialTime += Time.deltaTime;
                if (aerialTime >= AERIAL_TIME)
                {
                    SetPlayerState(STATE_AERIAL);
                }
            }
            else
            {
                aerialTime = 0.0f;

                // apply movement acceleration
                if (accelerationEnabled)
                {
                    float inputMagnitude = input3D.magnitude;
                    if (inputMagnitude > 0.0f)
                    {
                        accelerationMultiplier = Mathf.MoveTowards(accelerationMultiplier, 1.0f, accelerationRate * inputMagnitude * Time.deltaTime);
                    }
                    else
                    {
                        accelerationMultiplier = Mathf.MoveTowards(accelerationMultiplier, accelerationMinimum, accelerationLossRate * Time.deltaTime);
                    }

                    ApplyPlayerPropertiesWithAcceleration();
                }
            }
        }

        private void PlayerStateAerialStart()
        {
            ApplyPlayerProperties();
            localPlayer.SetGravityStrength(gravityStrength);
        }

        private void PlayerStateAerialUpdate()
        {
            // tick down grinding cooldown timer so we can start grinding again
            if (grindingCooldownRemaining > 0.0f)
            {
                grindingCooldownRemaining -= Time.deltaTime;
            }

            // tick down walljump cooldown timer so we can walljump again
            if (wallJumpCooldownRemaining > 0.0f)
            {
                wallJumpCooldownRemaining -= Time.deltaTime;
            }

            // switch states back to grounded if touching ground
            if (localPlayer.IsPlayerGrounded())
            {
                SetPlayerState(STATE_GROUNDED);
            }
            else
            {
                float inputMagnitude = input3D.magnitude;
                Vector3 localInput3D = localPlayerRotation * input3D;

                // detect wallriding
                if (inputMagnitude >= wallRideDeadzone &&
                    Physics.CapsuleCast(localPlayerPosition + localPlayerCapsuleA, localPlayerPosition + localPlayerCapsuleB, wallDetectionSize, localInput3D.normalized, out wallHit, wallDetectionDistance, wallLayers, QueryTriggerInteraction.Ignore) &&
                    Vector3.Angle(localPlayerRotation * input3D, wallHit.normal) > wallRideAcquireAngle &&
                    Mathf.Abs(wallHit.normal.y) <= wallRideSlopeTolerance)
                {
                    wallJumpTimeRemaining = wallJumpTime;

                    // if wallriding is enabled, wallride
                    if (wallRideEnabled)
                    {
                        SetPlayerState(STATE_WALLRIDE);

                        return;
                    }
                }

                // extra time we have after coming off a wall to walljump
                if (wallJumpTimeRemaining > 0.0f)
                {
                    wallJumpTimeRemaining -= Time.deltaTime;
                }

                // if wallriding is disabled or we have some time for wall jumping, we do walljump inputs here
                if (inputManager.GetJumpDown() && wallJumpTimeRemaining > 0.0f && wallJumpCooldownRemaining <= 0.0f)
                {
                    localPlayerVelocity = wallHit.normal * wallJumpForce;
                    localPlayerVelocity.y = wallJumpImpulse;

                    bonusJumpTimeRemaining = bonusJumpTime;
                    ledgeJumpTimeRemaining = 0.0f;
                    wallJumpTimeRemaining = 0.0f;
                    wallJumpCooldownRemaining = wallJumpCooldown;

                    // walljumping resets the double jump
                    if (wallResetsDoubleJump)
                    {
                        inputDoubleJumped = false;
                    }

                    SetEventFlag(EVENT_JUMP_WALL, true);

                    localPlayer.SetVelocity(localPlayerVelocity);
                }

                // still allow jumping a few milliseconds after stepping off a platform (coyote time)
                if (ledgeJumpTimeRemaining > 0.0f)
                {
                    ledgeJumpTimeRemaining -= Time.deltaTime;

                    if (inputManager.GetJumpDown())
                    {
                        localPlayerVelocity.y = jumpImpulse;
                        bonusJumpTimeRemaining = bonusJumpTime;

                        ledgeJumpTimeRemaining = 0.0f;
                    }
                }

                // apply variable jump height
                if (inputManager.GetJump() && bonusJumpTimeRemaining > 0.0f)
                {
                    localPlayerVelocity.y = jumpImpulse;
                    bonusJumpTimeRemaining -= Time.deltaTime;
                }
                else
                {
                    bonusJumpTimeRemaining = 0.0f;
                }

                // special jumps
                if (inputManager.GetJumpDown() && bonusJumpTimeRemaining <= 0.0f)
                {
                    // double jump
                    if (!inputDoubleJumped)
                    {
                        localPlayerVelocity.y = doubleJumpImpulse;
                        bonusJumpTimeRemaining = bonusJumpTime;

                        inputDoubleJumped = true;

                        // use to play a nice effect
                        SetEventFlag(EVENT_JUMP_DOUBLE, true);
                    }
                }

                // apply our velocity changes
                localPlayer.SetVelocity(localPlayerVelocity);
            }
        }

        private void PlayerStateWallrideStart()
        {
            ApplyPlayerProperties();
            localPlayer.SetGravityStrength(gravityStrength);

            // grinding also gives us coyote time
            ledgeJumpTimeRemaining = 0.0f;

            // reset some cooldowns if we touched the ground
            grindingCooldownRemaining = 0.0f;
        }

        private void PlayerStateWallrideUpdate()
        {
            // tick down walljump cooldown timer so we can walljump again
            if (wallJumpCooldownRemaining > 0.0f)
            {
                wallJumpCooldownRemaining -= Time.deltaTime;
            }

            if (!localPlayer.IsPlayerGrounded())
            {
                float inputMagnitude = input3D.magnitude;
                Vector3 localInput3D = localPlayerRotation * input3D;

                // detect wallriding
                if (inputMagnitude >= wallRideDeadzone &&
                    Physics.CapsuleCast(localPlayerPosition + localPlayerCapsuleA, localPlayerPosition + localPlayerCapsuleB, wallDetectionSize, localInput3D.normalized, out wallHit, wallDetectionDistance, wallLayers, QueryTriggerInteraction.Ignore) &&
                    Vector3.Angle(localPlayerRotation * input3D, wallHit.normal) > wallRideMaintainAngle &&
                    Mathf.Abs(wallHit.normal.y) <= wallRideSlopeTolerance)
                {
                    // walljump
                    if (inputManager.GetJumpDown() && wallJumpCooldownRemaining <= 0.0f)
                    {
                        localPlayerVelocity = wallHit.normal * wallJumpForce;
                        localPlayerVelocity.y = wallJumpImpulse;

                        bonusJumpTimeRemaining = bonusJumpTime;
                        ledgeJumpTimeRemaining = 0.0f;
                        wallJumpCooldownRemaining = wallJumpCooldown;

                        localPlayer.SetVelocity(localPlayerVelocity);

                        SetEventFlag(EVENT_JUMP_WALL, true);

                        SetPlayerState(STATE_AERIAL);
                    }
                    // wallride slowdown
                    else
                    {
                        localPlayerVelocity.y = Mathf.MoveTowards(localPlayerVelocity.y, -wallRideFallSpeed, wallRideFriction * Time.deltaTime);

                        localPlayer.SetVelocity(localPlayerVelocity);
                    }
                }
                else
                {
                    SetPlayerState(STATE_AERIAL);
                }
            }
            else
            {
                SetPlayerState(STATE_GROUNDED);
            }
        }

        private void PlayerStateWallrideEnd()
        {
            // extra time from coming off a wall where a walljump is still valid
            wallJumpTimeRemaining = wallJumpTime;

            // reset double jump
            if (wallResetsDoubleJump)
            {
                inputDoubleJumped = false;
            }
        }

        private void PlayerStateSnappingStart()
        {
            RemovePlayerProperties();
            localPlayer.SetGravityStrength(ZERO_GRAVITY);

            // grinding also gives us coyote time
            ledgeJumpTimeRemaining = ledgeJumpTime;

            // set bonus jump time
            bonusJumpTimeRemaining = bonusJumpTime;

            // reset double jump
            if (grindingResetsDoubleJump)
            {
                inputDoubleJumped = false;
            }
        }

        private void PlayerStateSnappingUpdate()
        {
            // jump from grind
            if (inputManager.GetJumpDown())
            {
                // use a cooldown so we don't immediately start grinding from the same position
                grindingCooldownRemaining = grindJumpCooldown;

                localPlayerVelocity.y = grindJumpImpulse;
                bonusJumpTimeRemaining = bonusJumpTime;

                localPlayer.SetVelocity(localPlayerVelocity);

                // re-enable game object
                if (grindingDisablesRail)
                {
                    walker.track.gameObject.SetActive(true);
                }

                SetPlayerState(STATE_AERIAL);
            }
            else
            {
                if (Vector3.Distance(walker.GetPoint(), localPlayerPosition) <= trackSnapEpsilon)
                {
                    SetPlayerState(STATE_GRINDING);
                }
                else
                {
                    Vector3 target = Vector3.Lerp(localPlayerPosition, walker.GetPoint(), trackSnapSpeed * Time.deltaTime);
                    localPlayer.SetVelocity((target - localPlayerPosition) / Time.deltaTime);
                }
            }
        }

        private void PlayerStateSnappingEnd()
        {
            // use to play a nice sound effect
            SetEventFlag(EVENT_GRIND_START, true);

            grindingTurnCooldownRemaining = 0.0f;
        }

        private void PlayerStateGrindingStart()
        {
            RemovePlayerProperties();
            localPlayer.SetGravityStrength(ZERO_GRAVITY);

            // grinding also gives us coyote time
            ledgeJumpTimeRemaining = ledgeJumpTime;

            // set bonus jump time
            bonusJumpTimeRemaining = bonusJumpTime;

            // reset double jump
            if (grindingDisablesRail)
            {
                inputDoubleJumped = false;
            }
        }

        private void PlayerStateGrindingUpdate()
        {
            // jump from grind
            if (inputManager.GetJumpDown())
            {
                // use a cooldown so we don't immediately start grinding from the same position
                grindingCooldownRemaining = grindJumpCooldown;

                localPlayerVelocity.y = grindJumpImpulse;
                bonusJumpTimeRemaining = bonusJumpTime;

                localPlayer.SetVelocity(localPlayerVelocity);

                // re-enable game object
                if (grindingDisablesRail)
                {
                    walker.track.gameObject.SetActive(true);
                }

                SetPlayerState(STATE_AERIAL);
            }
            else
            {
                currentTrackVelocity = walker.track.GetVelocityByDistance(walker.trackPosition);
                currentTrackOrientation = Quaternion.LookRotation(currentTrackVelocity);

                float inputMagnitude = input3D.magnitude;

                if (grindingTurnCooldownRemaining > 0.0f)
                {
                    grindingTurnCooldownRemaining -= Time.deltaTime;
                }

                if (Vector3.Angle(localPlayerRotation * input3D, walker.trackDirection * currentTrackVelocity) > grindTurnAngle)
                {
                    // slow down if we pull the stick half-way
                    if (inputMagnitude >= grindSlowDeadzone && inputMagnitude <= grindTurnDeadzone)
                    {
                        trackSpeed = Mathf.MoveTowards(trackSpeed, grindBrakeSpeed, grindAcceleration * Time.deltaTime);
                    }
                    // change directions if we pull the stick back
                    else if (inputMagnitude > grindTurnDeadzone)
                    {
                        if (!inputTurned && grindingTurnCooldownRemaining <= 0.0f)
                        {
                            walker.trackDirection = -walker.trackDirection;
                            trackSpeed = 0.0f;

                            grindingTurnCooldownRemaining = grindTurnCooldown;

                            // use to play a nice sound
                            SetEventFlag(EVENT_GRIND_STOP, true);
                        }

                        inputTurned = true;

                        trackSpeed = Mathf.MoveTowards(trackSpeed, grindMaxSpeed, grindAcceleration * Time.deltaTime);
                    }
                    else
                    {
                        inputTurned = false;

                        // accelerate towards maximum grind speed
                        trackSpeed = Mathf.MoveTowards(trackSpeed, grindMaxSpeed, grindAcceleration * Time.deltaTime);
                    }
                }
                else
                {
                    inputTurned = false;

                    // accelerate towards maximum grind speed
                    trackSpeed = Mathf.MoveTowards(trackSpeed, grindMaxSpeed, grindAcceleration * Time.deltaTime);
                }

                // compute the next track position as a constant speed by using the distance as a multiplier
                previousTrackPosition = walker.trackPosition;
                Vector3 nextTrackPoint = walker.GetPointAfterDistance(trackSpeed);

                // disconnect from track but maintain speed
                if (walker.GetIsDone())
                {
                    grindingCooldownRemaining = grindFallCooldown;

                    // use to play a nice sound
                    SetEventFlag(EVENT_GRIND_STOP, true);

                    // re-enable game object
                    if (grindingDisablesRail)
                    {
                        walker.track.gameObject.SetActive(true);
                    }

                    SetPlayerState(STATE_AERIAL);
                }
                // move forward using constant speed
                else
                {
                    lastTarget = nextTrackPoint - localPlayer.GetPosition();

                    // undeltatime to "teleport"
                    if (lastTarget.magnitude <= grindTeleportDistance)
                    {
                        localPlayer.SetVelocity((nextTrackPoint - localPlayerPosition) / Time.deltaTime);
                    }
                    // too far away -- e.g. we got stuck in an obstacle, so actually force a teleport
                    else
                    {
                        localPlayer.TeleportTo(nextTrackPoint, localPlayerRotation, VRC_SceneDescriptor.SpawnOrientation.Default, true);
                    }
                }
            }
        }

        private void PlayerStateGrindingEnd()
        {
            // use this to play a nice effect
            SetEventFlag(EVENT_GRIND_STOP, true);

            grindingTurnCooldownRemaining = 0.0f;
        }

        public void GrindRail(BezierTrack track, int samplePoint)
        {
            if (grindingEnabled && inputManager.GetJumpIsBound())
            {
                walker.SetTrack(track);

                walker.walkByDistance = true;
                walker.trackPosition = track.GetSamplePointDistance(samplePoint);

                // rail inherits speed and direction from player horizontal movement
                Vector3 velocity = localPlayer.GetVelocity();
                velocity.y = 0;
                trackSpeed = velocity.magnitude;

                // decide direction based on how fast the player is moving
                if (trackSpeed >= grindMomentumThreshold)
                {
                    walker.trackDirection = Quaternion.Angle(track.GetOrientationByDistance(walker.trackPosition), Quaternion.LookRotation(velocity)) >= 90.0f ? -1.0f : 1.0f;
                }
                // decide direction based on which way the player is facing
                else
                {
                    walker.trackDirection = Quaternion.Angle(track.GetOrientationByDistance(walker.trackPosition), localPlayer.GetRotation()) >= 90.0f ? -1.0f : 1.0f;
                }

                // disable game object
                if (grindingDisablesRail)
                {
                    track.gameObject.SetActive(false);
                }

                SetPlayerState(STATE_SNAPPING);
            }
        }

        public float GetVelocity()
        {
            if (localPlayerCached)
            {
                return localPlayerVelocity.magnitude;
            }
            else
            {
                return 0.0f;
            }
        }

        public float GetScaledVelocity()
        {
            float velocity = GetVelocity();

            if (velocity > 0.0)
            {
                return Mathf.Clamp01(velocity / grindMaxSpeed);
            }
            else
            {
                return 0.0f;
            }
        }

        public bool IsFalling()
        {
            return localPlayerVelocity.y <= 0.0f;
        }

        public bool IsGrindingOnCooldown()
        {
            return grindingCooldownRemaining > 0.0f;
        }

        public Vector3 GetWallridePoint()
        {
            return wallHit.point;
        }

        public Quaternion GetWallrideOrientation()
        {
            return Quaternion.LookRotation(wallHit.normal);
        }

        public Quaternion GetGrindDirection()
        {
            return Quaternion.LookRotation(walker.trackDirection * currentTrackVelocity);
        }

        public void SetEventFlag(int flag, bool state)
        {
            if (state)
            {
                eventFlags |= flag;
            }
            else
            {
                eventFlags &= ~flag;

            }
        }

        public bool GetEventFlag(int flag)
        {
            return (eventFlags & flag) == flag;
        }

#if !COMPILER_UDONSHARP
        public void OnDrawGizmosSelected()
        {
            // visualize the player capsule
            Gizmos.color = Color.green;
            DrawPlayerCapsule(localPlayerCapsuleA, localPlayerCapsuleB, 0.3f);

            // visualize wall detector
            Gizmos.color = Color.yellow;
            DrawPlayerCapsule(localPlayerCapsuleA, localPlayerCapsuleB, wallDetectionSize);
        }

        public void DrawPlayerCapsule(Vector3 a, Vector3 b, float size)
        {
            Gizmos.DrawWireSphere(a, size);
            Gizmos.DrawWireSphere(b, size);
            Gizmos.DrawLine(a + Vector3.left * size, b + Vector3.left * size);
            Gizmos.DrawLine(a + Vector3.right * size, b + Vector3.right * size);
            Gizmos.DrawLine(a + Vector3.forward * size, b + Vector3.forward * size);
            Gizmos.DrawLine(a + Vector3.back * size, b + Vector3.back * size);
        }
#endif
    }
}