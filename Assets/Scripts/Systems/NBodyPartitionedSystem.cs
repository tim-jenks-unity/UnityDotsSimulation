using TJ.Camera;
using TJ.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace TJ.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class NBodyPartitionedSystem : JobComponentSystem
    {
        private const int BucketSizes = 0;
        
        [BurstCompile]
        struct CollisionJob : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;
            
            [NativeDisableParallelForRestriction] public NativeArray<bool> DestroyedEntities;
            [NativeDisableParallelForRestriction] public NativeArray<float> MassCopies;
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<double3> PositionCopies;
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<float> ScaleCopies;
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<Entity> Entities;
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<int> BucketIndices;
            [ReadOnly] public NativeArray<Bucket> Buckets;

            public void Execute(int index)
            {
                var bucket = Buckets[index];
                if (!bucket.Allocated)
                    return;
                
                for (int j = 0; j < bucket.Count; ++j)
                {
                    var jIdx = BucketIndices[bucket.Offset+j];
                    var myPosition = PositionCopies[jIdx];
                    if (!DestroyedEntities[jIdx])
                    {
                        for (int i = 0; i < bucket.Count; ++i)
                        {
                            var iIdx = BucketIndices[bucket.Offset+i];
                            if (i != j && !DestroyedEntities[iIdx])
                            {
                                var theirPosition = PositionCopies[iIdx];
                                var theirMass = MassCopies[iIdx];
                                var myMass = MassCopies[jIdx];
                                if (theirMass <= myMass)
                                {
                                    var theirScale = ScaleCopies[iIdx];
                                    var myScale = ScaleCopies[jIdx];
                                    var distSq = math.distancesq(myPosition, theirPosition);
                                    var radiusSq =
                                        math.pow(myScale * 0.5f, 2) + math.pow(theirScale * 0.5f, 2);
                                    if (distSq < radiusSq)
                                    {
                                        myMass += theirMass;
                                        MassCopies[jIdx] = myMass;
                                        DestroyedEntities[iIdx] = true;
                                        CommandBuffer.DestroyEntity(j, Entities[iIdx]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        [BurstCompile]
        struct PartitionAverageMasses : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<bool> DestroyedEntities;
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<float> MassCopies;
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<int> BucketIndices;
            public NativeArray<Bucket> Buckets;
            
            public void Execute(int index)
            {
                float totalMass = 0f;
                var bucket = Buckets[index];
                if (!bucket.Allocated)
                    return;
                
                for (int j = 0; j < bucket.Count; ++j)
                {
                    var jIdx = BucketIndices[bucket.Offset+j];
                    if (!DestroyedEntities[jIdx])
                    {
                        totalMass += MassCopies[jIdx];
                    }
                }

                bucket.Mass = totalMass;
                Buckets[index] = bucket;
            }
        }

        [BurstCompile]
        struct ApplyGriddedNBodyForces : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<bool> DestroyedEntities;
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<float> MassCopies;
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<double3> PositionCopies;
            [NativeDisableParallelForRestriction] public NativeArray<float3> Forces;
            [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<int> BucketIndices;
            [ReadOnly] public NativeArray<Bucket> Buckets;

            public void Execute(int index)
            {
                var bucket = Buckets[index];
                if (!bucket.Allocated)
                    return;
                for (int j = 0; j < bucket.Count; ++j)
                {
                    var jIdx = BucketIndices[bucket.Offset+j];
                    var myPosition = PositionCopies[jIdx];
                    var myMass = MassCopies[jIdx];
                    if (!DestroyedEntities[jIdx])
                    {
                        // N^2 particles in this bucket for accuracy
                        for (int i = 0; i < bucket.Count; ++i)
                        {
                            var iIdx = BucketIndices[bucket.Offset+i];
                            if (i != j && !DestroyedEntities[iIdx])
                            {
                                var theirPosition = PositionCopies[iIdx];
                                var theirMass = MassCopies[iIdx];
                                var delta = theirPosition - myPosition;
                                var distSq = math.distancesq(myPosition, theirPosition);
                                var f = (myMass * theirMass) / (distSq + 10f);
                                Forces[jIdx] += (float3)(f * delta / math.sqrt(distSq)); // TODO: get rid of sqrt?
                            }
                        }

                        // Apply average masses for other buckets at the bucket CoM
                        for (int k = 0; k < Buckets.Length; ++k)
                        {
                            var otherBucket = Buckets[k];
                            if (otherBucket.Allocated && !otherBucket.Grid.Equals(bucket.Grid))
                            {
                                var theirPosition = otherBucket.CenterOfMass;
                                var theirMass = otherBucket.Mass;
                                var delta = theirPosition - myPosition;
                                var distSq = math.distancesq(myPosition, theirPosition);
                                var f = (myMass * theirMass) / (distSq + 10f);
                                Forces[jIdx] += (float3) (f * delta / math.sqrt(distSq)); // TODO: get rid of sqrt?
                            }
                        }
                    }
                }
            }
        }
        
        private EntityQuery m_EntityQuery;
        private EndSimulationEntityCommandBufferSystem m_EndSimulationEntityCommandBufferSystem;

        public struct Bucket
        {
            public int3 Grid;
            public double3 CenterOfMass;
            public float Mass;
            public int Offset;
            public int Count;
            public bool Allocated;
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            float dt = UnityEngine.Time.deltaTime;

            var commandBuffer = m_EndSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
            
            var allEntities = m_EntityQuery.ToEntityArray(Allocator.TempJob);
            var destroyedEntities = new NativeArray<bool>(allEntities.Length, Allocator.TempJob);
            var scaleCopies = new NativeArray<float>(allEntities.Length, Allocator.TempJob);
            var massCopies = new NativeArray<float>(allEntities.Length, Allocator.TempJob);
            var forceCopies = new NativeArray<float3>(allEntities.Length, Allocator.TempJob);
            var positionCopies = new NativeArray<double3>(allEntities.Length, Allocator.TempJob);
            

            const int NumBuckets = 200;
            const int IndexPerBucket = 200;
            
            var gridToBucketIndex = new NativeHashMap<int3, int>(allEntities.Length, Allocator.TempJob);
            var bucketIndices = new NativeArray<int>(NumBuckets * IndexPerBucket, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var buckets = new NativeArray<Bucket>(NumBuckets, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < NumBuckets; ++i)
            {
                var buffer = buckets[i];
                buffer.Count = 0;
                buffer.Offset = i * IndexPerBucket;
                buffer.Grid = int3.zero;
                buffer.Allocated = false;
                buffer.CenterOfMass = double3.zero;
                buffer.Mass = 0f;
                buckets[i] = buffer;
            }

            var partitionSizes = new NativeArray<int>(2, Allocator.TempJob);
           
            var copyMassScaleTranslationJob = Entities.ForEach((Entity e, int entityInQueryIndex, in MassComponent myMass, in ForceComponent myForce, in Scale myScale, in PositionComponent myPosition) =>
            {
                scaleCopies[entityInQueryIndex] = myScale.Value;
                massCopies[entityInQueryIndex] = myMass.Value;
                positionCopies[entityInQueryIndex] = myPosition.Value;
            }).WithName("CopyMassScaleTranslation").Schedule(inputDeps);

            var calculatePartitionSizesJob = Job.WithCode(() =>
            {
                var min = new double3(double.MaxValue, double.MaxValue, double.MaxValue);
                var max = new double3(double.MinValue, double.MinValue, double.MinValue);
                for (int i = 0; i<positionCopies.Length; ++i)
                {
                    var pos = positionCopies[i];
                    min = math.min(min, pos);
                    max = math.max(max, pos);
                }

                var distance = math.distance(min, max);
                partitionSizes[BucketSizes] = (int)distance / 8;
            }).WithName("CalculateMinMaxBounds").WithBurst().Schedule(copyMassScaleTranslationJob);

            var determinePartitionsJob = Job.WithCode(() =>
                {
                    var partitionSize = partitionSizes[BucketSizes];
                    
                    double3 halfPartitionSize = default;
                    halfPartitionSize.xyz = partitionSize * 0.5;
                    
                    var nextBucket = 0;
                    
                    for (int i = 0; i < positionCopies.Length; ++i)
                    {
                        Bucket bucket;
                        var grid = (int3) math.floor(positionCopies[i] / partitionSize);
                        var bucketIndex = 0;
                        
                        if (!gridToBucketIndex.ContainsKey(grid))
                        {
                            bucketIndex = nextBucket;
                            gridToBucketIndex.Add(grid, bucketIndex);
                            ++nextBucket;
                            
                            bucket = buckets[bucketIndex];
                            bucket.Allocated = true;
                            bucket.Count = 0;
                            bucket.Grid = grid;
                            bucket.CenterOfMass = (bucket.Grid * partitionSize) + halfPartitionSize; // TODO: Check this is correct........ 
                            buckets[bucketIndex] = bucket;
                        }
                        else
                        {
                            bucketIndex = gridToBucketIndex[grid];
                        }
                        
                        bucket = buckets[bucketIndex];
                        bucketIndices[bucket.Offset + bucket.Count] = i;
                        bucket.Count++;
                        buckets[bucketIndex] = bucket;
                    }
                }).WithBurst().WithName("PerformPartition")
                .Schedule(calculatePartitionSizesJob);
            
            var partitioningAndCopyingCompleteBarrier = determinePartitionsJob;

            var processCollisionsNSquaredJob = new CollisionJob
            {
                CommandBuffer = commandBuffer,
                DestroyedEntities = destroyedEntities,
                Entities = allEntities,
                MassCopies = massCopies,
                PositionCopies = positionCopies,
                ScaleCopies = scaleCopies,
                Buckets = buckets,
                BucketIndices = bucketIndices
            }.Schedule(buckets.Length, 0, partitioningAndCopyingCompleteBarrier);
            m_EndSimulationEntityCommandBufferSystem.AddJobHandleForProducer(processCollisionsNSquaredJob);

            var calculatePartitionMassesJob = new PartitionAverageMasses
            {
                DestroyedEntities = destroyedEntities,
                MassCopies = massCopies,
                Buckets = buckets,
                BucketIndices = bucketIndices
            }.Schedule(buckets.Length, 0, processCollisionsNSquaredJob);

            var massesAndCollisionsCompleteBarrier = calculatePartitionMassesJob;
            
            var aggregateForcesNSquaredJob = new ApplyGriddedNBodyForces
            {
                DestroyedEntities = destroyedEntities,
                MassCopies = massCopies,
                PositionCopies = positionCopies,
                Forces = forceCopies,
                Buckets = buckets,
                BucketIndices = bucketIndices
            }.Schedule(buckets.Length, 0, massesAndCollisionsCompleteBarrier);

            var allPhysicsJobsCompleteBarrier = aggregateForcesNSquaredJob;
            
            var updateScaleMassForceJob = Entities.
                ForEach((Entity e, int entityInQueryIndex, ref Scale scale, ref MassComponent myMass, ref ForceComponent myForce) =>
                {
                    myForce.Value = forceCopies[entityInQueryIndex];
                    myMass.Value = massCopies[entityInQueryIndex];
                    var volume = myMass.Value / 4f;
                    scale.Value = math.pow(volume * 0.75f * (1 / math.PI), 1/3f);
                })
                .WithName("UpdateScaleFromMassJob")
                .WithReadOnly(massCopies)
                .WithReadOnly(forceCopies)
                .WithBurst()
                .Schedule(allPhysicsJobsCompleteBarrier);

            var cameraLookAt = CameraController.Instance.Data.LookAtPosition;
            var simulateJob = Entities.
                ForEach((ref PositionComponent position,
                    ref VelocityComponent velocity,
                    ref Translation translation,
                    in ForceComponent force,
                    in MassComponent mass) =>
                {
                    velocity.Value += dt * force.Value / mass.Value;
                    position.Value += dt * velocity.Value;
                    translation.Value = (float3)(position.Value - cameraLookAt);
                })
                .WithBurst()
                .Schedule(updateScaleMassForceJob);


            var disposalJob = allEntities.Dispose(simulateJob);
            disposalJob = JobHandle.CombineDependencies(disposalJob, destroyedEntities.Dispose(simulateJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, scaleCopies.Dispose(simulateJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, massCopies.Dispose(simulateJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, forceCopies.Dispose(simulateJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, positionCopies.Dispose(simulateJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, partitionSizes.Dispose(simulateJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, bucketIndices.Dispose(simulateJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, buckets.Dispose(simulateJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, gridToBucketIndex.Dispose(simulateJob));
            return disposalJob;
        }

        protected override void OnCreate()
        {
            m_EntityQuery = GetEntityQuery(typeof(MassComponent));
            m_EndSimulationEntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
    }
}