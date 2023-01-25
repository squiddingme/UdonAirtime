
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace Airtime.Input
{
    [DefaultExecutionOrder(-50)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerInputManager : UdonSharpBehaviour
    {
        protected bool inputEnabled = true;

        protected Vector2 inputMove = new Vector2();
        protected Vector3 inputMove3D = new Vector3();
        protected bool inputJump = false;
        protected bool inputJumped = false;

        public virtual void Update()
        {
            inputJumped = false;
        }

        public virtual void OnDisable()
        {
            inputJump = false;
            inputJumped = false;
        }

        public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
        {
            inputMove.x = value;
            inputMove3D.x = value;
        }

        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            inputMove.y = value;
            inputMove3D.z = value;
        }

        public Vector2 GetDirection()
        {
            return enabled && inputEnabled ? Vector2.ClampMagnitude(inputMove, 1.0f) : Vector2.zero;
        }

        public Vector3 GetDirection3D()
        {
            return enabled && inputEnabled ? Vector3.ClampMagnitude(inputMove3D, 1.0f) : Vector3.zero;
        }

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            inputJump = value;
            inputJumped = value;
        }

        public bool GetJump()
        {
            return enabled && inputEnabled && inputJump;
        }

        public bool GetJumpDown()
        {
            return enabled && inputEnabled && inputJumped;
        }

        public void StartInput()
        {
            inputEnabled = true;
        }

        public void StopInput()
        {
            inputEnabled = false;
        }
    }
}