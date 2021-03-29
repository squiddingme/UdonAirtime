
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Airtime.Player.Movement;

namespace Airtime.Player.Effects
{
    // PooledPlayerController
    // requires Phasedragon's SimpleObjectPool
    public class PooledPlayerController : UdonSharpBehaviour
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
            if (ownerCached && localPlayerCached && localPlayer.IsValid())
            {
                // if owner, use synced variables to display useful information
                if (controllerCached && owner == localPlayer)
                {
                    networkPlayerState = controller.GetPlayerState();
                    networkPlayerScaledVelocity = controller.GetScaledVelocity();
                    networkPlayerGrindDirection = controller.GetGrindDirection();
                }

                transform.SetPositionAndRotation(owner.GetPosition(), owner.GetRotation());

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

                if (owner == localPlayer)
                {
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
                        Debug.LogError("PooledPlayerController could not find a PlayerController in the scene");
                    }
                }
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

        public void DoubleJump()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedDoubleJump");
        }

        public void NetworkedDoubleJump()
        {
            doubleJumpSound.PlayOneShot(doubleJumpSound.clip);
        }

        public void WallJump()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedWallJump");
        }

        public void NetworkedWallJump()
        {
            wallJumpSound.PlayOneShot(wallJumpSound.clip);
        }

        public void StartGrind()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedStartGrind");
        }

        public void NetworkedStartGrind()
        {
            grindStartSound.PlayOneShot(grindStartSound.clip);
        }

        public void StopGrind()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedStopGrind");
        }

        public void SwitchGrindDirection()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedStopGrind");
        }

        public void NetworkedStopGrind()
        {
            grindStopSound.PlayOneShot(grindStopSound.clip);
        }
    }
}
