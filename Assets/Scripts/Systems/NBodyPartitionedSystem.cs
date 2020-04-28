using System;
using System.Text;
using TJ.Camera;
using TJ.Components;
using TJ.Systems.JobTypes;
using TJ.Utility;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TJ.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class NBodyPartitionedSystem : JobComponentSystem
    {
        private const int CollisionBucketSize = 10;
        private const int NBodyBucketSize = 30;
        
        [BurstCompile]
        struct CollisionJob : IJobNativeMultiHashMapVisitKeyAllValues<int3, int>
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;
            public NativeArray<bool> DestroyedEntities;
            public NativeArray<float> MassCopies;
            [ReadOnly] public NativeArray<double3> PositionCopies;
            [ReadOnly] public NativeArray<float> ScaleCopies;
            [ReadOnly] public NativeArray<Entity> Entities;

            public void Execute(int3 key, NativeArray<int> scratchValues, int count)
            {
                for (int j = 0; j < count; ++j)
                {
                    var jIdx = scratchValues[j];
                    var myPosition = PositionCopies[jIdx];
                    if (!DestroyedEntities[jIdx])
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            var iIdx = scratchValues[i];
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
        struct PartitionAverageMasses : IJobNativeMultiHashMapVisitKeyAllValues<int3, int>
        {
            [ReadOnly] public NativeArray<bool> DestroyedEntities;
            [ReadOnly] public NativeArray<float> MassCopies;
            [ReadOnly] public NativeArray<double3> PositionCopies;
            public NativeArray<float3> ForceCopies;

            public void Execute(int3 key, NativeArray<int> scratchValues, int count)
            {
                float totalMass = 0f;
                int counted = 0;
                for (int j = 0; j < count; ++j)
                {
                    var jIdx = scratchValues[j];
                    if (!DestroyedEntities[jIdx])
                    {
                        totalMass += MassCopies[jIdx];
                        ++counted;
                    }
                }

                var theirGrid = key;
                var theirMass = totalMass / counted;
                var theirPosition = (key * NBodyBucketSize);

                var numEntities = PositionCopies.Length;
                for (int i = 0; i < numEntities; ++i)
                {
                    var myPosition = PositionCopies[i];
                    var myGrid = (int3) math.floor(myPosition / NBodyBucketSize);
                    if (!(myGrid.Equals(theirGrid)))
                    {
                        var myMass = MassCopies[i];
                        var delta = theirPosition - myPosition;
                        var distSq = math.distancesq(myPosition, theirPosition);
                        var f = (myMass * theirMass) / (distSq + 10f);
                        ForceCopies[i] += (float3) (f * delta / math.sqrt(distSq)); // TODO: get rid of sqrt?
                    }
                }
            }
        }

        [BurstCompile]
        struct ApplyGriddedNBodyForces : IJobNativeMultiHashMapVisitKeyAllValues<int3, int>
        {
            [ReadOnly] public NativeArray<bool> DestroyedEntities;
            [ReadOnly] public NativeArray<float> MassCopies;
            [ReadOnly] public NativeArray<double3> PositionCopies;
            public NativeArray<float3> Forces;

            public void Execute(int3 key, NativeArray<int> scratchValues, int count)
            {
                for (int j = 0; j < count; ++j)
                {
                    var jIdx = scratchValues[j];
                    var myPosition = PositionCopies[jIdx];
                    if (!DestroyedEntities[jIdx])
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            var iIdx = scratchValues[i];
                            if (i != j && !DestroyedEntities[iIdx])
                            {
                                var theirPosition = PositionCopies[iIdx];
                                var theirMass = MassCopies[iIdx];
                                var myMass = MassCopies[jIdx];
                                var delta = theirPosition - myPosition;
                                var distSq = math.distancesq(myPosition, theirPosition);
                                var f = (myMass * theirMass) / (distSq + 10f);
                                Forces[jIdx] += (float3)(f * delta / math.sqrt(distSq)); // TODO: get rid of sqrt?
                            }
                        }
                    }
                }
            }
        }
        
        private EntityQuery m_EntityQuery;
        private EndSimulationEntityCommandBufferSystem m_EndSimulationEntityCommandBufferSystem;

            
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
            var collisionPartitions = new NativeMultiHashMap<int3, int>(allEntities.Length, Allocator.TempJob);
            var nBodyPartitions = new NativeMultiHashMap<int3, int>(allEntities.Length, Allocator.TempJob);
           
            var copyMassScaleTranslationJob = Entities.ForEach((Entity e, int entityInQueryIndex, in MassComponent myMass, in ForceComponent myForce, in Scale myScale, in PositionComponent myPosition) =>
            {
                scaleCopies[entityInQueryIndex] = myScale.Value;
                massCopies[entityInQueryIndex] = myMass.Value;
                forceCopies[entityInQueryIndex] = myForce.Value;
                positionCopies[entityInQueryIndex] = myPosition.Value;
            }).WithName("CopyMassScaleTranslation").Schedule(inputDeps);

            var parallelCollisionPartitionWriter = collisionPartitions.AsParallelWriter();
            var parallelnBodyPartitionsPartitionWriter = nBodyPartitions.AsParallelWriter();
            var determinePartitionsJob = Entities.ForEach((Entity e, int entityInQueryIndex, in PositionComponent myPosition) =>
            {
                var pos = myPosition.Value;
                parallelCollisionPartitionWriter.Add((int3)math.floor(pos / CollisionBucketSize), entityInQueryIndex);
                parallelnBodyPartitionsPartitionWriter.Add((int3)math.floor(pos / CollisionBucketSize), entityInQueryIndex);
            }).WithName("DeterminePartitions").Schedule(inputDeps);

            var initalSteps = JobHandle.CombineDependencies(copyMassScaleTranslationJob, determinePartitionsJob);
                
            var processCollisionsNSquaredJob = new CollisionJob
            {
                CommandBuffer = commandBuffer,
                DestroyedEntities = destroyedEntities,
                Entities = allEntities,
                MassCopies = massCopies,
                PositionCopies = positionCopies,
                ScaleCopies = scaleCopies,
            }.Schedule(collisionPartitions, 1, initalSteps);
            m_EndSimulationEntityCommandBufferSystem.AddJobHandleForProducer(processCollisionsNSquaredJob);

            var zeroForces = Entities.
                ForEach((ref ForceComponent force) => { force.Value = float3.zero; })
                .WithName("ZeroForces")
                .WithBurst()
                .Schedule(inputDeps);

            var waitZeroAndCollisionJobs = JobHandle.CombineDependencies(zeroForces, processCollisionsNSquaredJob);

            var calculatePartitionMasses = new PartitionAverageMasses
            {
                DestroyedEntities = destroyedEntities, 
                MassCopies = massCopies,
                ForceCopies = forceCopies,
                PositionCopies = positionCopies
            }.Schedule(nBodyPartitions, 
                1, waitZeroAndCollisionJobs);
            
            var aggregateForcesNSquaredJob = new ApplyGriddedNBodyForces
            {
                DestroyedEntities = destroyedEntities,
                MassCopies = massCopies,
                PositionCopies = positionCopies,
                Forces = forceCopies
            }.Schedule(nBodyPartitions, 1, calculatePartitionMasses);
            
            var updateScaleFromMassJob = Entities.
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
                .Schedule(aggregateForcesNSquaredJob);

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
                .Schedule(aggregateForcesNSquaredJob);

            var disposalJob = allEntities.Dispose(JobHandle.CombineDependencies(updateScaleFromMassJob, aggregateForcesNSquaredJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, destroyedEntities.Dispose(aggregateForcesNSquaredJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, scaleCopies.Dispose(aggregateForcesNSquaredJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, massCopies.Dispose(aggregateForcesNSquaredJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, forceCopies.Dispose(aggregateForcesNSquaredJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, positionCopies.Dispose(aggregateForcesNSquaredJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, collisionPartitions.Dispose(aggregateForcesNSquaredJob));
            return JobHandle.CombineDependencies(disposalJob, simulateJob);
        }

        protected override void OnCreate()
        {
            m_EntityQuery = GetEntityQuery(typeof(MassComponent));
            m_EndSimulationEntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
    }
}