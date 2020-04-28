using TJ.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace TJ.Camera
{
    public struct CameraData
    {
        public double3 LookAtPosition;
        public double3 LookAtScaledWorldPosition;
        public double3 EyePosition;
        public double3 EyeScaledWorldPosition;
        public float Distance;
        public float3 Forward;
        public float3 Up;
        public float Velocity;
        public float ZoomVelocity;
        public float Units;
        public float2 NearFarPlane;
        public float2 MinMaxDistance;
        
        public double3 StartLookAtPosition;
        public float StartDistance;
        public float3 StartForward;
        public float3 StartUp;
        public float StartCameraVelocity;
        public float StartCameraZoomVelocity;
        public float StartUnits;
        
        public double3 TargetLookAtPosition;
        public float TargetDistance;
        public float3 TargetForward;
        public float3 TargetUp;
        public float TargetCameraVelocity;
        public float TargetCameraZoomVelocity;
        public float2 TargetNearFarPlane;
        public float2 TargetMinMaxDistance;
        public float TargetUnits;
    }
    
    public class CameraController : StateMachineMonoBehaviour<CameraControllerStates, CameraData, CameraController>
    {
        public double3 LookAtPosition;
        public double3 LookAtScaledWorldPosition;
        public double3 EyePosition;
        public double3 EyeScaledWorldPosition;
        [Range(0.1f, 5000f)] public float Distance;
        public float3 Forward;
        public float3 Up;
        [Range(0.1f, 50f)] public float Velocity;
        [Range(0.1f, 50f)] public float ZoomVelocity;
        [Range(0.1f, 4f)] public float Units = 1f;
        public float2 NearFarPlane;
        public float2 MinMaxDistance;
        public GameObject MainCameraObject;
        public UnityEngine.Camera MainCamera { get; private set; }

        protected override CameraController Provide()
        {
            return this;
        }

        protected override void StateMachineAwake(ref CameraData data)
        {
            MainCamera = MainCameraObject.GetComponent<UnityEngine.Camera>();
            Forward = math.normalize(Forward);
            LookAtPosition = double3.zero;
            RegisterState<DefaultCameraControllerState>(CameraControllerStates.DefaultKeyboardMouse);
            RegisterState<MouseDragCameraControllerState>(CameraControllerStates.MouseDragMovement);
            SetActiveState(CameraControllerStates.DefaultKeyboardMouse);

            StateMachinePreUpdate(0f, ref data);
        }

        protected override void StateMachinePreUpdate(float dt, ref CameraData data)
        {
            data.LookAtPosition = LookAtPosition;
            data.EyePosition = EyePosition;
            data.Distance = Distance;
            data.Forward = Forward;
            data.Up = Up;
            data.Velocity = Velocity;
            data.ZoomVelocity = ZoomVelocity;
            data.Units = Units;
            data.MinMaxDistance = MinMaxDistance;
            data.NearFarPlane = NearFarPlane;
        }

        protected override void StateMachinePostUpdate(float dt, ref CameraData data)
        {
            data.LookAtScaledWorldPosition = data.LookAtPosition * data.Units;
            data.EyeScaledWorldPosition = data.EyePosition * data.Units;
            
            LookAtPosition = data.LookAtPosition;
            LookAtScaledWorldPosition = data.LookAtScaledWorldPosition;
            EyePosition = data.EyePosition;
            Distance = data.Distance;
            Forward = data.Forward;
            Up = data.Up;
            Velocity = data.Velocity;
            ZoomVelocity = data.ZoomVelocity;
            Units = data.Units;
            MinMaxDistance = data.MinMaxDistance;
            NearFarPlane = data.NearFarPlane; 
            MainCameraObject.transform.rotation = quaternion.LookRotation(data.Forward, data.Up);
            MainCameraObject.transform.position = (-data.Forward * data.Distance);
            MainCamera.nearClipPlane = data.NearFarPlane.x;
            MainCamera.farClipPlane = data.NearFarPlane.y;
        }
    }
}