
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Rails.Player.Attachment
{
    public class LocalTrackingAttachment : UdonSharpBehaviour
    {
        public bool attached = false;
        public VRCPlayerApi.TrackingDataType point;

        private VRCPlayerApi localPlayer;
        private bool localPlayerCached = false;

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
                if (attached)
                {
                    VRCPlayerApi.TrackingData data = localPlayer.GetTrackingData(point);
                    transform.position = data.position;
                    transform.rotation = data.rotation;
                }
            }
        }
    }
}
