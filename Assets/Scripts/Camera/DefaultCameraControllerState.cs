using TJ.Input;
using TJ.Utility;
using Unity.Mathematics;

namespace TJ.Camera
{
    public class DefaultCameraControllerState : IStateBase<CameraControllerStates, CameraData>
    {
        public CameraControllerStates Update(float dt, ref CameraData data)
        {
            if (CanDragStart)
            {
                return CameraControllerStates.MouseDragMovement;
            }

            var input = InputController.Instance.CurrentFrame;
            
            double3 force;
            force.x = 0.0;
            force.y = 0.0;
            force.z = 0.0;
            var cameraForward = data.Forward;
            cameraForward.y = 0;
            cameraForward = math.normalize(cameraForward);
            float3 cameraRight = math.normalize(math.cross(cameraForward, math.up()));
            force -= (cameraRight * input.PrimaryAxis.x * data.Velocity * dt);
            force += (cameraForward * input.PrimaryAxis.y * data.Velocity * dt);
            data.LookAtPosition += force;
            data.Distance -= (data.ZoomVelocity) * input.SecondaryAxis.x;
            data.Distance = math.clamp(data.Distance, data.MinMaxDistance.x, data.MinMaxDistance.y);
            data.EyePosition = data.LookAtPosition + (data.Distance * -data.Forward);

            var rotation = quaternion.RotateY(dt * -math.PI * 0.025f);
            data.Forward = math.rotate(rotation, data.Forward); 
            return NextState;
        }

        static CameraControllerStates NextState => CameraControllerStates.DefaultKeyboardMouse;

        private static bool CanDragStart => InputController.Instance.CurrentFrame.MouseLeftDragStarted;
        
        public void Started(CameraControllerStates previousState, ref CameraData data)
        {
        }

        public void Stopped(ref CameraData data)
        {
        }
    }
}