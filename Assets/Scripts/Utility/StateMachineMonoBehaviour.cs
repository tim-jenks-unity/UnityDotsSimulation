using System.Collections.Generic;
using UnityEngine;

namespace TJ.Utility
{
    public interface IStateBase<TStateEnum, TStateData>
    {
        TStateEnum Update(float dt, ref TStateData data);
        void Started(TStateEnum previousState, ref TStateData data);
        void Stopped(ref TStateData data);
    }
    
    public abstract class StateMachineMonoBehaviour<TStateEnum, TStateData, TSingleton> : 
        SingletonMonoBehaviour<TSingleton>
        where TStateData : struct
    {
        public TStateData Data => m_StateMachine.Data;
        private StateMachine<TStateEnum, TStateData> m_StateMachine = new StateMachine<TStateEnum, TStateData>(new Dictionary<TStateEnum, IStateBase<TStateEnum, TStateData>>());
        public virtual void SetData(ref TStateData data)
        {
            m_StateMachine.SetData(ref data);
        }

        void Update()
        {
            var dt = Time.deltaTime;
            StateMachinePreUpdate(dt, ref m_StateMachine.Data);
            m_StateMachine.Update(dt);
            StateMachinePostUpdate(dt, ref m_StateMachine.Data);
        }
        
        protected override void SingletonAwake()
        {
            StateMachineAwake(ref m_StateMachine.Data);
        }

        protected virtual void StateMachineAwake(ref TStateData data)
        {
        }

        public void SetActiveState(TStateEnum type)
        {
            m_StateMachine.SetActiveState(type);
        }

        public void RegisterState<TStateBase>(TStateEnum type) where TStateBase : IStateBase<TStateEnum, TStateData>, new()
        {
            m_StateMachine.RegisterState<TStateBase>(type);
        }

        public void RegisterState<TStateBase>(TStateBase state, TStateEnum type)
            where TStateBase : IStateBase<TStateEnum, TStateData>
        {
            m_StateMachine.RegisterState(state, type);
        }

        public TStateBase GetState<TStateBase>(TStateEnum type) where TStateBase : class, IStateBase<TStateEnum, TStateData>
        {
            return m_StateMachine.GetState<TStateBase>(type);
        }

        public bool TryGetCurrentState<TStateBase>(out TStateBase state)
            where TStateBase : class, IStateBase<TStateEnum, TStateData>
        {
            return m_StateMachine.TryGetCurrentState(out state);
        }
        

        public TStateEnum CurrentState => m_StateMachine.CurrentState;
        
        protected virtual void StateMachinePreUpdate(float dt, ref TStateData data)
        {
        }

        protected virtual void StateMachinePostUpdate(float dt, ref TStateData data)
        {
        }
        
        protected virtual void StateMachineLateUpdate(ref TStateData data)
        {
        }
    }
}