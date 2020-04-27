using System.Collections.Generic;
using UnityEngine;

namespace TJ.Utility
{
    public struct StateMachine<TStateEnum, TStateData>  where TStateData : struct
    {
        public TStateData Data;
        
        TStateEnum m_CurrentStateType; 
        IStateBase<TStateEnum, TStateData> m_CurrentState;
        private Dictionary<TStateEnum, IStateBase<TStateEnum, TStateData>> m_States;

        public StateMachine(Dictionary<TStateEnum, IStateBase<TStateEnum, TStateData>> states)
        {
            Data = default;
            m_CurrentStateType = default;
            m_CurrentState = null;
            m_States = states;
        }
        
        public void SetData(ref TStateData data)
        {
            Data = data;
        }

        public void Update(float dt)
        {
            var result = m_CurrentState.Update(dt, ref Data);
            if (!result.Equals(m_CurrentStateType))
            {
                SetActiveState(result);
            }
        }

        public void RegisterState<TStateBase>(TStateEnum type) where TStateBase : IStateBase<TStateEnum, TStateData>, new()
        {
            m_States.Add(type, new TStateBase());
        }

        public void RegisterState<TStateBase>(TStateBase state, TStateEnum type)
            where TStateBase : IStateBase<TStateEnum, TStateData>
        {
            m_States.Add(type, state);
        }

        public void SetActiveState(TStateEnum type)
        {
            m_CurrentState?.Stopped(ref Data);
            m_CurrentState = m_States[type];
            var previousState = m_CurrentStateType;
            m_CurrentStateType = type;
            m_CurrentState.Started(previousState, ref Data);
        }

        public TStateBase GetState<TStateBase>(TStateEnum type) where TStateBase : class, IStateBase<TStateEnum, TStateData>
        {
            return m_States[type] as TStateBase;
        }

        public bool TryGetCurrentState<TStateBase>(out TStateBase state)
            where TStateBase : class, IStateBase<TStateEnum, TStateData>
        {
            state = m_CurrentState as TStateBase;
            return state != null;
        }

        public TStateEnum CurrentState => m_CurrentStateType;
    }
}