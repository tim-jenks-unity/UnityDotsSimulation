using TJ.Utility;

namespace TJ.Input
{
    public class SelectionController : StateMachineMonoBehaviour<SelectionStateTypes, SelectionState, SelectionController>
    {
        protected override SelectionController Provide()
        {
            return this;
        }
        
        protected override void StateMachineAwake(ref SelectionState data)
        {
            RegisterState<NoSelectionState>(SelectionStateTypes.NoSelection);
            SetActiveState(SelectionStateTypes.NoSelection);
        }
    }
}