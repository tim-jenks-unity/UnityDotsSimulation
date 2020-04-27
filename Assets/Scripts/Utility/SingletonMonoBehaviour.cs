using UnityEngine;

namespace TJ.Utility
{
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour
    {
        public static T Instance { get; private set; }

        void Awake()
        {
            SingletonAwake();
            if (Instance == null)
            {
                Instance = Provide();
            }
        }

        protected abstract T Provide();

        protected virtual void SingletonAwake()
        {
        }
    }
}