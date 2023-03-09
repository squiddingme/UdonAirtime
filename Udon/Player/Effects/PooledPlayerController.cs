
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Airtime.Player.Movement;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
using Airtime;
#endif

namespace Airtime.Player.Effects
{
    // PooledPlayerController
    // requires CyanLaser's CyanPlayerObjectPool
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class PooledPlayerController : UdonSharpBehaviour
    {
        [HideInInspector] public PlayerController controller;
        protected bool controllerCached = false;

        [Header("Animation")]
        public bool useAnimator;
        public Animator animator;
        public string animatorVelocityParam = "Velocity";
        public string animatorWallridingParam = "IsWallriding";
        public string animatorGrindingParam = "IsGrinding";

        [Header("Effects")]
        public Transform grindTransform;
        public Transform wallrideTransform;
        public AudioSource jumpSound;
        public AudioSource doubleJumpSound;
        public AudioSource wallJumpSound;
        public AudioSource grindStartSound;
        public AudioSource grindStopSound;

        // VRC Stuff
        public VRCPlayerApi Owner;
        protected bool ownerCached = false;
        protected VRCPlayerApi localPlayer;
        protected bool localPlayerCached = false;

        // Built-in Player States
        public const string STATE_STOPPED = "Stopped";
        public const string STATE_GROUNDED = "Grounded";
        public const string STATE_AERIAL = "Aerial";
        public const string STATE_WALLRIDE = "Wallride";
        public const string STATE_SNAPPING = "Snapping";
        public const string STATE_GRINDING = "Grinding";

        // Networked Effects
        [UdonSynced] protected string networkPlayerState;
        [UdonSynced(UdonSyncMode.Linear)] protected float networkPlayerScaledVelocity;
        [UdonSynced(UdonSyncMode.Linear)] protected Quaternion networkPlayerGrindDirection;

        public virtual void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                localPlayerCached = true;
            }

            if (controller != null)
            {
                controllerCached = true;
            }
        }

        public virtual void LateUpdate()
        {
            if (ownerCached && localPlayerCached && Utilities.IsValid(localPlayer))
            {
                // if owner, use synced variables to display useful information
                if (controllerCached && Owner.isLocal)
                {
                    networkPlayerState = controller.GetPlayerState();
                    networkPlayerScaledVelocity = controller.GetScaledVelocity();
                    networkPlayerGrindDirection = controller.GetGrindDirection();
                }

                transform.SetPositionAndRotation(Owner.GetPosition(), Owner.GetRotation());

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

        public virtual void _OnOwnerSet()
        {
            if (Owner != null)
            {
                if (Owner.isLocal && controller != null)
                {
                    Component behaviour = GetComponent(typeof(UdonBehaviour));
                    controller.RegisterEventHandler((UdonBehaviour)behaviour);
                }

                ownerCached = true;
            }
        }

        public virtual void _OnCleanup()
        {
            ownerCached = false;
        }

        public virtual void _Jump()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedJump");
        }

        public void NetworkedJump()
        {
            jumpSound.Play();
        }

        public virtual void _DoubleJump()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedDoubleJump");
        }

        public void NetworkedDoubleJump()
        {
            doubleJumpSound.PlayOneShot(doubleJumpSound.clip);
        }

        public virtual void _WallJump()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedWallJump");
        }

        public void NetworkedWallJump()
        {
            wallJumpSound.PlayOneShot(wallJumpSound.clip);
        }

        public virtual void _StartGrind()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedStartGrind");
        }

        public void NetworkedStartGrind()
        {
            grindStartSound.PlayOneShot(grindStartSound.clip);
        }

        public virtual void _StopGrind()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedStopGrind");
        }

        public virtual void _SwitchGrindDirection()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkedStopGrind");
        }

        public void NetworkedStopGrind()
        {
            grindStopSound.PlayOneShot(grindStopSound.clip);
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(PooledPlayerController))]
    public class PooledPlayerControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            PooledPlayerController player = target as PooledPlayerController;

            GUILayout.Label("Player Controller (Required)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            PlayerController newController = (PlayerController)EditorGUILayout.ObjectField("Player Controller", player.controller, typeof(PlayerController), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(player, "Change Player Controller");
                player.controller = newController;
                EditorUtility.SetDirty(player);
            }

            if (player.controller == null)
            {
                SerializedProperty controllerProp = serializedObject.FindProperty("controller");
                controllerProp.objectReferenceValue = AirtimeEditorUtility.AutoConfigurePlayerController();
                serializedObject.ApplyModifiedProperties();
            }

            GUILayout.Label("Script", EditorStyles.boldLabel);

            base.OnInspectorGUI();
        }
    }
#endif
}
