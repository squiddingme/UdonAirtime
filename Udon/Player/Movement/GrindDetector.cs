
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Airtime.Track;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
using Airtime;
#endif

namespace Airtime.Player.Movement
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GrindDetector : UdonSharpBehaviour
    {
        [HideInInspector] public PlayerController controller;
        private bool controllerCached = false;

        [HideInInspector] public int trackLayer = 0;

        // Built-in Player States
        public const string STATE_STOPPED = "Stopped";
        public const string STATE_GROUNDED = "Grounded";
        public const string STATE_AERIAL = "Aerial";
        public const string STATE_WALLRIDE = "Wallride";
        public const string STATE_SNAPPING = "Snapping";
        public const string STATE_GRINDING = "Grinding";

        public void Start()
        {
            if (controller != null)
            {
                controllerCached = true;
            }
        }

        public void OnTriggerStay(Collider other)
        {
            if (other != null)
            {
                if (other.gameObject.layer == trackLayer && controller.GetPlayerState() == STATE_AERIAL && (!controller.grindingMustFall || controller.GetIsFalling()) && !controller.GetIsGrindingOnCooldown())
                {
                    GameObject g = other.transform.parent.gameObject;

                    BezierTrack track = g.GetComponent<BezierTrack>();
                    if (track != null)
                    {
                        int p;
                        if (int.TryParse(other.gameObject.name, out p))
                        {
                            controller.StartGrind(track, p);
                        }
                        else
                        {
                            Debug.LogError(string.Format("Track sample point {0} under {1} is incorrectly named and could not be parsed", other.gameObject.name, g.name));
                        }
                    }
                    else
                    {
                        Debug.LogError(string.Format("Track sample point {0} was detected without a BezierTrack", other.gameObject.name));
                    }
                }
            }
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(GrindDetector))]
    public class DetectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            GrindDetector detector = target as GrindDetector;

            GUILayout.Label("Player Controller (Required)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            PlayerController newController = (PlayerController)EditorGUILayout.ObjectField("Player Controller", detector.controller, typeof(PlayerController), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(detector, "Change Player Controller");
                detector.controller = newController;
                EditorUtility.SetDirty(detector);
            }

            if (detector.controller == null)
            {
                SerializedProperty controllerProp = serializedObject.FindProperty("controller");
                controllerProp.objectReferenceValue = AirtimeEditorUtility.AutoConfigurePlayerController();
                serializedObject.ApplyModifiedProperties();
            }

            GUILayout.Label("Script", EditorStyles.boldLabel);

            base.OnInspectorGUI();

            GUILayout.Label("Track Detection", EditorStyles.boldLabel);

            int newLayer = EditorGUILayout.LayerField("Track Layer", detector.trackLayer);
            if (newLayer != detector.trackLayer)
            {
                Undo.RecordObject(detector, "Change Track Layer");
                detector.trackLayer = newLayer;
                EditorUtility.SetDirty(detector);
            }
        }
    }
#endif
}