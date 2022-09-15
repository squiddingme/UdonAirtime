
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Airtime.Track
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BezierExampleWalker : UdonSharpBehaviour
    {
        public BezierWalker walker;

        public bool walking = true;
        public bool rotating = true;

        public float speed = 0.5f;

        private Vector3 position;
        private Quaternion rotation;

        public void Update()
        {
            if (walking)
            {
                transform.position = walker.GetPointAfterDistance(speed);
            }

            rotation = transform.rotation;
            if (rotating)
            {
                transform.rotation = walker.track.GetRotationByDistance(walker.trackPosition);
            }
        }
    }
}