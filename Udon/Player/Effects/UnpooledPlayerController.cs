﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Airtime.Player.Movement;

namespace Airtime.Player.Effects
{
    public class UnpooledPlayerController : UdonSharpBehaviour
    {
        [Header("Player Controller")]
        public string playerControllerName = "PlayerController";
        private PlayerController controller;
        private bool controllerCached = false;

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

            GameObject search = GameObject.Find(playerControllerName);
            if (search != null)
            {
                Component component = search.GetComponent(typeof(UdonBehaviour));
                if (component != null)
                {
                    controller = (PlayerController)component;

                    Component behaviour = GetComponent(typeof(UdonBehaviour));
                    controller.RegisterEventHandler((UdonBehaviour)behaviour);

                    controllerCached = true;
                }
                else
                {
                    Debug.LogError("There was an object named PlayerController in the scene but it has no UdonBehaviour");
                }
            }
            else
            {
                Debug.LogError("UnpooledPlayerController could not find a PlayerController in the scene");
            }
        }

        public void LateUpdate()
        {
            if (localPlayerCached && localPlayer.IsValid())
            {
                if (controllerCached)
                {
                    playerState = controller.GetPlayerState();
                    playerScaledVelocity = controller.GetScaledVelocity();
                }

                transform.SetPositionAndRotation(localPlayer.GetPosition(), localPlayer.GetRotation());

                // set transform of grind particles
                grindTransform.rotation = controller.GetGrindDirection();

                // animator effects
                if (useAnimator)
                {
                    animator.SetFloat(animatorVelocityParam, playerScaledVelocity);
                    animator.SetBool(animatorWallridingParam, playerState == STATE_WALLRIDE);
                    animator.SetBool(animatorGrindingParam, playerState == STATE_GRINDING);
                }
            }
        }

        public void DoubleJump()
        {
            doubleJumpSound.PlayOneShot(doubleJumpSound.clip);
        }

        public void WallJump()
        {
            wallJumpSound.PlayOneShot(wallJumpSound.clip);
        }

        public void StartGrind()
        {
            grindStartSound.PlayOneShot(grindStartSound.clip);
        }

        public void StopGrind()
        {
            grindStopSound.PlayOneShot(grindStopSound.clip);
        }

        public void SwitchGrindDirection()
        {
            grindStopSound.PlayOneShot(grindStopSound.clip);
        }
    }
}
