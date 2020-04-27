using UnityEngine;

namespace TJ.Utility
{
    public static class Log
    {
        public static void D(object message)
        {
            UnityEngine.Debug.Log(message);
        }
    }
}