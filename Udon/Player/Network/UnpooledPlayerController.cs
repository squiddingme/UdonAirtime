
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Airtime.Player.Movement;

namespace Airtime.Player.Network
{
    public class UnpooledPlayerController : UdonSharpBehaviour
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

        // Effects
        private int playerState;
        private float playerScaledVelocity;

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
            if (localPlayerCached)
            {
                playerState = controller.GetPlayerState();
                playerScaledVelocity = controller.GetScaledVelocity();

                transform.position = localPlayer.GetPosition();
                transform.rotation = localPlayer.GetRotation();

                // set transform of grind particles
                grindTransform.rotation = controller.GetGrindDirection();

                // animator effects
                if (useAnimator)
                {
                    animator.SetFloat(animatorVelocityParam, playerScaledVelocity);
                    animator.SetBool(animatorWallridingParam, playerState == STATE_WALLRIDE);
                    animator.SetBool(animatorGrindingParam, playerState == STATE_GRINDING);
                }

                if (controller.GetEventFlag(EVENT_JUMP_DOUBLE))
                {
                    doubleJumpSound.PlayOneShot(doubleJumpSound.clip);
                }

                if (controller.GetEventFlag(EVENT_JUMP_WALL))
                {
                    wallJumpSound.PlayOneShot(wallJumpSound.clip);
                }

                if (controller.GetEventFlag(EVENT_GRIND_START))
                {
                    grindStartSound.PlayOneShot(grindStartSound.clip);
                }

                if (controller.GetEventFlag(EVENT_GRIND_STOP))
                {
                    grindStopSound.PlayOneShot(grindStopSound.clip);
                }
            }
        }
    }
}
