using System;
using UnityEngine;

namespace TJ.Utility
{
    public interface ISingletonScriptableObject
    {
        void Initialize();
        void Ready();
    }
    
    public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject, ISingletonScriptableObject {
        protected static T ms_instance = null;
        public static T Instance => ms_instance;

        public void Ready()
        {
            if (ms_instance == null)
                ms_instance = this as T;
            
            if (ms_instance != null)
                ms_instance.Initialize();
        }
    }
}