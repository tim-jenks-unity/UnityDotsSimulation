using Unity.Mathematics;

namespace TJ.Input
{
    public struct InputState
    {
        public double3 TopLeftWorldPosition;
        public float3 TopLeftLocalPosition;
        
        public double3 MouseWorldPosition;
        public double3 MouseWorldDelta;
        public float3 MouseLocalPosition;
        public float3 MouseLocalDelta;
        public float3 MouseScreenPosition;
        public float3 MouseScreenDelta;
        
        public float3 MouseScreenPositionLeftDown;
        public float3 MouseScreenPositionRightDown;
        public float3 MouseScreenPositionMiddleDown;

        public bool PrimaryAction;
        public bool SecondaryAction;
        public bool TertiaryAction;
        public bool QuaternaryAction;
        
        public bool[] SubActions;
        public bool CancelAction;
        
        public bool CycleForwardAction;
        public bool CycleBackwardAction;
        
        public float2 PrimaryAxis;
        public float2 SecondaryAxis;
        public float2 TertiaryAxis;
        public float2 QuaternaryAxis;
        
        public bool MouseLeftClicked;
        public bool MouseRightClicked;
        public bool MouseMiddleClicked;
        
        public bool MouseLeftDragging;
        public bool MouseLeftDragStarted;
        public bool MouseLeftDragStopped;
        
        public bool MouseRightDragging;
        public bool MouseRightDragStarted;
        public bool MouseRightDragStopped;        
        
        public bool MouseLeftHeld;
        public bool MouseLeftDown;
        public bool MouseLeftUp;

        public bool MouseRightHeld;
        public bool MouseRightDown;
        public bool MouseRightUp;
        
        public bool MouseMiddleHeld;
        public bool MouseMiddleDown;
        public bool MouseMiddleUp;
    }
}