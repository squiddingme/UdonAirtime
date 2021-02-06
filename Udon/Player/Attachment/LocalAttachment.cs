
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Rails.Player.Attachment
{
    public class LocalAttachment : UdonSharpBehaviour
    {
        public bool attached = false;

        public Vector3 offset = Vector3.zero;

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
                    transform.position = localPlayer.GetPosition() + offset;
                    transform.rotation = localPlayer.GetRotation();
                }
            }
        }
    }
}
