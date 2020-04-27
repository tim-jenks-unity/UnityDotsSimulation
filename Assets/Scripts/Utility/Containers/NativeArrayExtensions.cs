using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;

namespace TJ.Utility.Containers
{
    public static class NativeArrayExtensions
    {
#if UNITY_EDITOR
        public static string ToDebugString<T>(this NativeArray<T> array) where T : struct
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < array.Length; ++i)
            {
                T t = array[i];
                builder.Append(t.ToString());
            }
            return builder.ToString();
        }
        
        public static string ToDebugString<T>(this NativeList<T> list) where T : struct
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < list.Length; ++i)
            {
                T t = list[i];
                builder.Append(t.ToString());
            }
            return builder.ToString();
        }
        
        public static string ToDebugString<T, V>(this NativeHashMap<T, V> hashMap) where T : struct, IEquatable<T> where V : struct
        {
            var keys = hashMap.GetKeyArray(Allocator.Temp);
            return keys.ToDebugString();
        }
#endif        

        public static void DifferenceSorted<T>(this NativeArray<T> a, ref NativeArray<T> b, ref NativeList<T> resultCapacityOfA)
            where T : struct, IComparable<T>
        {
            var i = 0;
            var j = 0;
            while (i < a.Length && j < b.Length)
            {
                if (a[i].CompareTo(b[j]) < 0)
                {
                    resultCapacityOfA.Add(a[i]);
                    i++;
                }
                else if (b[j].CompareTo(a[i]) < 0)
                {
                    j++;
                }
                else
                {
                    i++;
                    j++;
                }
            }

            while (i < a.Length)
            {
                resultCapacityOfA.Add(a[i]);
                ++i;
            }
        }
        
        public static void AddManagedRangeFromBeginning<T>(this NativeArray<T> a, IEnumerable<T> range)
            where T : struct
        {
            var count = 0;
            foreach (T t in range)
            {
                a[count] = t;
                ++count;
            }
        }
    }
}