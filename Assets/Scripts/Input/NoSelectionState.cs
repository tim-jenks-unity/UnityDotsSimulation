using TJ.Utility;
using Unity.Entities;

namespace TJ.Input
{
    public class NoSelectionState : IStateBase<SelectionStateTypes, SelectionState>
    {
        private EntityManager m_Em;

        public NoSelectionState()
        {
            m_Em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
        
        public SelectionStateTypes Update(float dt, ref SelectionState data)
        {
            return SelectionStateTypes.NoSelection;
        }

        public void Started(SelectionStateTypes previousState, ref SelectionState data)
        {
        }

        public void Stopped(ref SelectionState data)
        {
        }
    }
}