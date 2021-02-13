
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Airtime.Input
{
    public class PlayerInputManager : UdonSharpBehaviour
    {
        [Header("Input Rebinding")]
        public string[] scannedDesktopInputs;
        public string[] scannedVRInputs;

        [Header("Jumping")]
        public string jumpInput;
        [HideInInspector] public bool rebindingJumpInput = false;

        [Header("UI")]
        public Text warningText;
        public Text bindingText;

        private bool jumpInputBound = false;

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
                if (localPlayer.IsUserInVR())
                {
                    if (rebindingJumpInput)
                    {
                        for (int i = 0; i < scannedVRInputs.Length; i++)
                        {
                            if (UnityEngine.Input.GetButtonDown(scannedVRInputs[i]))
                            {
                                jumpInput = scannedVRInputs[i];

                                warningText.color = Color.green;
                                warningText.text = "Bound";
                                bindingText.text = jumpInput;

                                rebindingJumpInput = false;
                                jumpInputBound = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (rebindingJumpInput)
                    {
                        for (int i = 0; i < scannedDesktopInputs.Length; i++)
                        {
                            if (UnityEngine.Input.GetButtonDown(scannedDesktopInputs[i]))
                            {
                                jumpInput = scannedDesktopInputs[i];

                                warningText.color = Color.green;
                                warningText.text = "Bound";
                                bindingText.text = jumpInput;

                                rebindingJumpInput = false;
                                jumpInputBound = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void RebindJump()
        {
            jumpInput = string.Empty;

            warningText.color = Color.white;
            warningText.text = "Rebinding, press a button...";

            rebindingJumpInput = true;
        }

        public bool GetJumpIsBound()
        {
            return jumpInputBound;
        }

        public bool GetJump()
        {
            if (jumpInput == string.Empty)
            {
                return false;
            }
            else
            {
                return UnityEngine.Input.GetButton(jumpInput);
            }
        }

        public bool GetJumpDown()
        {
            if (jumpInput == string.Empty)
            {
                return false;
            }
            else
            {
                return UnityEngine.Input.GetButtonDown(jumpInput);
            }
        }

        public Vector2 GetDirection()
        {
            return new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
        }

        public Vector3 GetDirection3D()
        {
            return new Vector3(UnityEngine.Input.GetAxisRaw("Horizontal"), 0.0f, UnityEngine.Input.GetAxisRaw("Vertical"));
        }
    }
}