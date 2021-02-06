
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Rails.Track;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace Rails.Player.Movement
{
    public class GrindDetector : UdonSharpBehaviour
    {
        [Header("Controller")]
        public PlayerController controller;

        [Header("Track Properties")]
        [HideInInspector] public int trackLayer = 0;

        // Player States (we have to keep a copy here because of udon)
        public const int STATE_GROUNDED = 0;
        public const int STATE_AERIAL = 1;
        public const int STATE_WALLRIDE = 2;
        public const int STATE_SNAPPING = 3;
        public const int STATE_GRINDING = 4;

        public void OnTriggerStay(Collider other)
        {
            if (other.gameObject.layer == trackLayer)
            {
                if (controller.GetPlayerState() == STATE_AERIAL && controller.IsFalling() && !controller.IsGrindingOnCooldown())
                {
                    GameObject g = other.transform.parent.gameObject;

                    BezierTrack track = g.GetComponent<BezierTrack>();
                    if (track != null)
                    {
                        int p;
                        if (int.TryParse(other.gameObject.name, out p))
                        {
                            controller.GrindRail(track, p);
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
            base.OnInspectorGUI();

            GrindDetector detector = target as GrindDetector;

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