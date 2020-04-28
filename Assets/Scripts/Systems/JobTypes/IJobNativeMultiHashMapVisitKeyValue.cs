using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace TJ.Systems.JobTypes
{
    [JobProducerType(typeof(JobNativeMultiHashMapVisitKeyAllValues.JobNativeMultiHashMapVisitKeyAllValuesProducer<, ,>))]
    public interface IJobNativeMultiHashMapVisitKeyAllValues<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        void Execute(TKey key, NativeArray<TValue> values, int count);
    }

    public static class JobNativeMultiHashMapVisitKeyAllValues
    {
        internal struct JobNativeMultiHashMapVisitKeyAllValuesProducer<TJob, TKey, TValue>
            where TJob : struct, IJobNativeMultiHashMapVisitKeyAllValues<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            [ReadOnly] public NativeMultiHashMap<TKey, TValue> HashMap;
            
            internal TJob JobData;

            static IntPtr s_JobReflectionData;

            internal static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobNativeMultiHashMapVisitKeyAllValuesProducer<TJob, TKey, TValue>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return s_JobReflectionData;
            }

            internal delegate void ExecuteJobFunction(ref JobNativeMultiHashMapVisitKeyAllValuesProducer<TJob, TKey, TValue> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref JobNativeMultiHashMapVisitKeyAllValuesProducer<TJob, TKey, TValue> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                NativeArray<TValue> valueArray = new NativeArray<TValue>(10000, Allocator.Temp);

                while (true)
                {
                    TKey lastKey = default;
                    int count = 0;
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        valueArray.Dispose();
                        return;
                    }

                    var hashMapData = producer.HashMap.GetUnsafeBucketData();
                    var buckets = (int*)hashMapData.buckets;
                    var nextPtrs = (int*)hashMapData.next;
                    var keys = hashMapData.keys;
                    var values = hashMapData.values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];
                        TKey currentKey = default;
                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);
                            currentKey = key;
                            var value = UnsafeUtility.ReadArrayElement<TValue>(values, entryIndex);
                            valueArray[count++] = value;
                            entryIndex = nextPtrs[entryIndex];
                        }

                        if (!lastKey.Equals(currentKey))
                        {
                            producer.JobData.Execute(currentKey, valueArray, count);
                            currentKey = lastKey;
                            count = 0;
                        }
                    }
                }
            }
        }

        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(this TJob jobData, NativeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobNativeMultiHashMapVisitKeyAllValues<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var jobProducer = new JobNativeMultiHashMapVisitKeyAllValuesProducer<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer)
                , JobNativeMultiHashMapVisitKeyAllValuesProducer<TJob, TKey, TValue>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
            );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.GetUnsafeBucketData().bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }
}