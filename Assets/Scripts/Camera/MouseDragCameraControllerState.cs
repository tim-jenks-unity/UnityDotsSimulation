using TJ.Input;
using TJ.Utility;
using Unity.Mathematics;

namespace TJ.Camera
{
    public class MouseDragCameraControllerState : IStateBase<CameraControllerStates, CameraData>
    {
        private float3 m_LastDragPoint;
        
        public CameraControllerStates Update(float dt, ref CameraData data)
        {
            if (DragStop)
            {
                return CameraControllerStates.DefaultKeyboardMouse;
            }

            data.LookAtPosition -= InputController.Instance.CurrentFrame.MouseLocalDelta;
            return CameraControllerStates.MouseDragMovement;
        }

        public void Started(CameraControllerStates previousState, ref CameraData data)
        {
        }

        public void Stopped(ref CameraData data)
        {
        }
        
        static bool DragStop
        {
            get
            {
                return InputController.Instance.CurrentFrame.MouseLeftDragStopped;
            }
        }
    }
}