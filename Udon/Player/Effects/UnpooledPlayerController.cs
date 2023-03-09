
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
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UnpooledPlayerController : UdonSharpBehaviour
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
        protected VRCPlayerApi localPlayer;
        protected bool localPlayerCached = false;

        // Built-in Player States
        public const string STATE_STOPPED = "Stopped";
        public const string STATE_GROUNDED = "Grounded";
        public const string STATE_AERIAL = "Aerial";
        public const string STATE_WALLRIDE = "Wallride";
        public const string STATE_SNAPPING = "Snapping";
        public const string STATE_GRINDING = "Grinding";

        // Effects
        protected string playerState;
        protected float playerScaledVelocity;

        public virtual void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                localPlayerCached = true;
            }

            if (controller != null)
            {
                Component behaviour = GetComponent(typeof(UdonBehaviour));
                controller.RegisterEventHandler((UdonBehaviour)behaviour);

                controllerCached = true;
            }
        }

        public virtual void LateUpdate()
        {
            if (localPlayerCached && Utilities.IsValid(localPlayer))
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

        public virtual void _Jump()
        {
            jumpSound.Play();
        }

        public virtual void _DoubleJump()
        {
            doubleJumpSound.PlayOneShot(doubleJumpSound.clip);
        }

        public virtual void _WallJump()
        {
            wallJumpSound.PlayOneShot(wallJumpSound.clip);
        }

        public virtual void _StartGrind()
        {
            grindStartSound.PlayOneShot(grindStartSound.clip);
        }

        public virtual void _StopGrind()
        {
            grindStopSound.PlayOneShot(grindStopSound.clip);
        }

        public virtual void _SwitchGrindDirection()
        {
            grindStopSound.PlayOneShot(grindStopSound.clip);
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(UnpooledPlayerController))]
    public class UnpooledPlayerControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            UnpooledPlayerController player = target as UnpooledPlayerController;

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
