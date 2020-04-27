using TJ.Camera;
using TJ.Utility;
using Unity.Mathematics;
using UnityEngine;
using I = UnityEngine.Input;

namespace TJ.Input
{
    public class InputController : SingletonMonoBehaviour<InputController>
    {
        public const int NumActionKeys = 36;
        public InputState CurrentFrame;
        public InputState LastFrame;
        
        private RaycastHit[] m_Results = new RaycastHit[NumActionKeys];
            
        protected override InputController Provide()
        {
            CurrentFrame.SubActions = new bool[36];
            return this;
        }
        
        void Update()
        {
            LastFrame = CurrentFrame;

            CurrentFrame.MouseLeftHeld = I.GetMouseButton(0);
            CurrentFrame.MouseLeftDown = I.GetMouseButtonDown(0);
            CurrentFrame.MouseLeftUp = I.GetMouseButtonUp(0);
            
            CurrentFrame.MouseRightHeld = I.GetMouseButton(1);
            CurrentFrame.MouseRightDown = I.GetMouseButtonDown(1);
            CurrentFrame.MouseRightUp = I.GetMouseButtonUp(1);
            
            CurrentFrame.MouseMiddleHeld = I.GetMouseButton(2);
            CurrentFrame.MouseMiddleDown = I.GetMouseButtonDown(2);
            CurrentFrame.MouseMiddleUp = I.GetMouseButtonUp(2);

            CurrentFrame.MouseScreenPosition = UnityEngine.Input.mousePosition;
            CurrentFrame.MouseScreenDelta = CurrentFrame.MouseScreenPosition - LastFrame.MouseScreenPosition;

            CurrentFrame.MouseLeftDragging = CurrentFrame.MouseLeftHeld;
            CurrentFrame.MouseLeftDragStarted = CurrentFrame.MouseLeftDragging && !LastFrame.MouseLeftDragging;
            CurrentFrame.MouseLeftDragStopped = !CurrentFrame.MouseLeftDragging && LastFrame.MouseLeftDragging;
            
            CurrentFrame.MouseRightDragging = CurrentFrame.MouseRightHeld;
            CurrentFrame.MouseRightDragStarted = CurrentFrame.MouseRightDragging && !LastFrame.MouseRightDragging;
            CurrentFrame.MouseRightDragStopped = !CurrentFrame.MouseRightDragging && LastFrame.MouseRightDragging;            
            
            var mainCamera = CameraController.Instance;
            var ray = mainCamera.MainCamera.ScreenPointToRay(CurrentFrame.MouseScreenPosition);

            CurrentFrame.MouseScreenPositionLeftDown = CurrentFrame.MouseLeftDown ? CurrentFrame.MouseScreenPosition : LastFrame.MouseScreenPositionLeftDown;
            CurrentFrame.MouseScreenPositionRightDown = CurrentFrame.MouseRightDown ? CurrentFrame.MouseScreenPosition : LastFrame.MouseScreenPositionRightDown;
            CurrentFrame.MouseScreenPositionMiddleDown = CurrentFrame.MouseMiddleDown ? CurrentFrame.MouseScreenPosition : LastFrame.MouseScreenPositionMiddleDown;

            if (Physics.RaycastNonAlloc(ray, m_Results) > 0)
            {
                CurrentFrame.MouseLocalPosition = m_Results[0].point / mainCamera.Data.Units;
                CurrentFrame.MouseLocalDelta = CurrentFrame.MouseLeftDragStarted ? float3.zero : CurrentFrame.MouseLocalPosition - LastFrame.MouseLocalPosition;

                CurrentFrame.MouseWorldPosition = mainCamera.Data.LookAtPosition + CurrentFrame.MouseLocalPosition;
                CurrentFrame.MouseWorldDelta = CurrentFrame.MouseLeftDragStarted ? double3.zero : CurrentFrame.MouseWorldPosition - LastFrame.MouseWorldPosition;
            }
            else
            {
                CurrentFrame.MouseLocalPosition = LastFrame.MouseLocalPosition;
                CurrentFrame.MouseLocalDelta = LastFrame.MouseLocalDelta;
                CurrentFrame.MouseWorldPosition = LastFrame.MouseWorldPosition;
                CurrentFrame.MouseWorldDelta = LastFrame.MouseWorldDelta;
            }
            
            var topLeft = mainCamera.MainCamera.ScreenPointToRay(new Vector3(Screen.width, Screen.height, 0f));
            if (Physics.RaycastNonAlloc(topLeft, m_Results) > 0)
            {
                CurrentFrame.TopLeftLocalPosition = m_Results[0].point / mainCamera.Data.Units;
                CurrentFrame.TopLeftWorldPosition = mainCamera.Data.LookAtPosition + CurrentFrame.TopLeftLocalPosition;
            }
            else
            {
                CurrentFrame.TopLeftLocalPosition = LastFrame.TopLeftLocalPosition;
                CurrentFrame.TopLeftWorldPosition = LastFrame.TopLeftWorldPosition;
            }

            CurrentFrame.PrimaryAxis = new float2(I.GetAxis("Horizontal"), I.GetAxis("Vertical"));
            CurrentFrame.SecondaryAxis = new float2(I.GetAxis("Mouse ScrollWheel"), 0f);

            CurrentFrame.MouseLeftClicked = CurrentFrame.MouseLeftUp && LastFrame.MouseLeftHeld &&
                                            math.lengthsq(CurrentFrame.MouseScreenPosition - CurrentFrame.MouseScreenPositionLeftDown) < 2;

            CurrentFrame.MouseRightClicked = CurrentFrame.MouseRightUp && LastFrame.MouseRightHeld &&
                                            math.lengthsq(CurrentFrame.MouseScreenPosition - CurrentFrame.MouseScreenPositionRightDown) < 2;
            
            CurrentFrame.MouseMiddleClicked = CurrentFrame.MouseMiddleUp && LastFrame.MouseMiddleHeld &&
                                            math.lengthsq(CurrentFrame.MouseScreenPosition - CurrentFrame.MouseScreenPositionMiddleDown) < 2;

            CurrentFrame.PrimaryAction = I.GetAxis("Fire1") > 0f;
            CurrentFrame.SecondaryAction = I.GetAxis("Fire2") > 0f;
            CurrentFrame.TertiaryAction = I.GetAxis("Fire3") > 0f;
            CurrentFrame.QuaternaryAction = I.GetAxis("Jump") > 0f;

            CurrentFrame.CycleForwardAction = I.GetKeyUp(KeyCode.Equals);
            CurrentFrame.CycleBackwardAction = I.GetKeyUp(KeyCode.Minus);

            CurrentFrame.CancelAction = I.GetKeyUp(KeyCode.Escape);

            CurrentFrame.SubActions[0] = I.GetKeyUp(KeyCode.F1);
            CurrentFrame.SubActions[1] = I.GetKeyUp(KeyCode.F2);
            CurrentFrame.SubActions[2] = I.GetKeyUp(KeyCode.F3);
            CurrentFrame.SubActions[3] = I.GetKeyUp(KeyCode.F4);
            CurrentFrame.SubActions[4] = I.GetKeyUp(KeyCode.F5);
            CurrentFrame.SubActions[5] = I.GetKeyUp(KeyCode.F6);
            CurrentFrame.SubActions[6] = I.GetKeyUp(KeyCode.F7);
            CurrentFrame.SubActions[7] = I.GetKeyUp(KeyCode.F8);
            CurrentFrame.SubActions[8] = I.GetKeyUp(KeyCode.F9);
            CurrentFrame.SubActions[9] = I.GetKeyUp(KeyCode.F10);
            CurrentFrame.SubActions[10] = I.GetKeyUp(KeyCode.F11);
            CurrentFrame.SubActions[11] = I.GetKeyUp(KeyCode.F12);
        }
    }
}