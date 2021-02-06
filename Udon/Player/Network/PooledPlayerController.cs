
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Rails.Player.Movement;

namespace Rails.Player.Network
{
    // PooledPlayerController
    // requires Phasedragon's SimpleObjectPool
    public class PooledPlayerController : UdonSharpBehaviour
    {
        [Header("Player Controller")]
        public PlayerController controller;

        [Header("Animation")]
        public bool useAnimator;
        public Animator animator;
        public string animatorVelocityParam = "Velocity";
        public string animatorWallridingParam = "IsWallriding";
        public string animatorGrindingParam = "IsGrinding";

        [Header("Effects")]
        public Transform grindTransform;
        public Transform wallrideTransform;
        public AudioSource doubleJumpSound;
        public AudioSource wallJumpSound;
        public AudioSource grindStartSound;
        public AudioSource grindStopSound;

        // VRC Stuff
        private VRCPlayerApi owner;
        private bool ownerCached = false;
        private VRCPlayerApi localPlayer;
        private bool localPlayerCached = false;

        // Player States (we have to keep a copy here because of udon)
        public const int STATE_GROUNDED = 0;
        public const int STATE_AERIAL = 1;
        public const int STATE_WALLRIDE = 2;
        public const int STATE_SNAPPING = 3;
        public const int STATE_GRINDING = 4;

        // Event Flags (same story here)
        public const int EVENT_JUMP_DOUBLE = 2;
        public const int EVENT_JUMP_WALL = 4;
        public const int EVENT_GRIND_START = 8;
        public const int EVENT_GRIND_STOP = 16;

        // Networked Effects
        [UdonSynced] private int networkPlayerState;
        [UdonSynced(UdonSyncMode.Linear)] private float networkPlayerScaledVelocity;
        [UdonSynced(UdonSyncMode.Linear)] private Quaternion networkPlayerGrindDirection;

        public void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                localPlayerCached = true;
            }
        }

        public void LateUpdate()
        {
            if (ownerCached)
            {
                // if owner, use synced variables to display useful information
                if (owner == localPlayer)
                {
                    if (controller.GetEventFlag(EVENT_JUMP_DOUBLE))
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedDoubleJumped");
                    }

                    if (controller.GetEventFlag(EVENT_JUMP_WALL))
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedWallJumped");
                    }

                    if (controller.GetEventFlag(EVENT_GRIND_START))
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedStartGrinding");
                    }

                    if (controller.GetEventFlag(EVENT_GRIND_STOP))
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedStopGrinding");
                    }

                    networkPlayerState = controller.GetPlayerState();
                    networkPlayerScaledVelocity = controller.GetScaledVelocity();
                    networkPlayerGrindDirection = controller.GetGrindDirection();
                }

                transform.position = owner.GetPosition();
                transform.rotation = owner.GetRotation();

                // set transform of grind particles
                grindTransform.rotation = networkPlayerGrindDirection;

                // animator effects
                if (useAnimator)
                {
                    animator.SetFloat(animatorVelocityParam, networkPlayerScaledVelocity);
                    animator.SetBool(animatorWallridingParam, networkPlayerState == STATE_WALLRIDE);
                    animator.SetBool(animatorGrindingParam, networkPlayerState == STATE_GRINDING);
                }
            }
        }

        public void UpdateOwner()
        {
            owner = Networking.GetOwner(gameObject);
            if (owner != null)
            {
                ownerCached = true;
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (owner == player)
            {
                owner = null;
                ownerCached = false;
            }
        }

        public void NetworkedDoubleJumped()
        {
            doubleJumpSound.PlayOneShot(doubleJumpSound.clip);
        }

        public void NetworkedWallJumped()
        {
            wallJumpSound.PlayOneShot(wallJumpSound.clip);
        }

        public void NetworkedStartGrinding()
        {
            grindStartSound.PlayOneShot(grindStartSound.clip);
        }

        public void NetworkedStopGrinding()
        {
            grindStopSound.PlayOneShot(grindStopSound.clip);
        }
    }
}
