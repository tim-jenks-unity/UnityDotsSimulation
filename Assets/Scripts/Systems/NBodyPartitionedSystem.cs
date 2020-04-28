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
            var positionCopies = new NativeArray<double3>(allEntities.Length, Allocator.TempJob);
            var partitions = new NativeMultiHashMap<int3, int>(allEntities.Length, Allocator.TempJob);
           
            var copyMassScaleTranslationJob = Entities.ForEach((Entity e, int entityInQueryIndex, in MassComponent myMass, in Scale myScale, in PositionComponent myPosition) =>
            {
                scaleCopies[entityInQueryIndex] = myScale.Value;
                massCopies[entityInQueryIndex] = myMass.Value;
                positionCopies[entityInQueryIndex] = myPosition.Value;
            }).WithName("CopyMassScaleTranslation").Schedule(inputDeps);

            var parallelPartitionWriter = partitions.AsParallelWriter();
            var determinePartitionsJob = Entities.ForEach((Entity e, int entityInQueryIndex, in PositionComponent myPosition) =>
            {
                var pos = myPosition.Value;
                parallelPartitionWriter.Add((int3)pos/10, entityInQueryIndex);
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
            }.Schedule(partitions, 1, initalSteps);
            m_EndSimulationEntityCommandBufferSystem.AddJobHandleForProducer(processCollisionsNSquaredJob);

            var zeroForces = Entities.
                ForEach((ref ForceComponent force) => { force.Value = float3.zero; })
                .WithName("ZeroForces")
                .WithBurst()
                .Schedule(inputDeps);

            var waitZeroAndCollisionJobs = JobHandle.CombineDependencies(zeroForces, processCollisionsNSquaredJob); 

            var updateScaleFromMassJob = Entities.
                ForEach((Entity e, int entityInQueryIndex, ref Scale scale, ref MassComponent myMass) =>
                {
                    myMass.Value = massCopies[entityInQueryIndex];
                    var volume = myMass.Value / 4f;
                    scale.Value = math.pow(volume * 0.75f * (1 / math.PI), 1/3f);
                })
                .WithName("UpdateScaleFromMassJob")
                .WithReadOnly(massCopies)
                .WithBurst()
                .Schedule(waitZeroAndCollisionJobs);
            
            var aggregateForcesNSquaredJob = Entities.
                ForEach((Entity e, int entityInQueryIndex, ref ForceComponent force, in PositionComponent myPosition, in MassComponent myMass) =>
                {
                    var pos = myPosition.Value;
                    var myPartition = (int3)pos/10;
                    if (!destroyedEntities[entityInQueryIndex])
                    {
                        for (int i = 0; i < allEntities.Length; ++i)
                        {
                            if (!destroyedEntities[i])
                            {
                                var other = allEntities[i];
                                if (other != e)
                                {
                                    var theirPosition = positionCopies[i];
                                    var theirMass = massCopies[i];
                                    var delta = theirPosition - myPosition.Value;
                                    var distSq = math.distancesq(myPosition.Value, theirPosition);
                                    var f = (myMass.Value * theirMass) / (distSq + 10f);
                                    force.Value += (float3)(f * delta / math.sqrt(distSq)); // TODO: get rid of sqrt?
                                }
                            }
                        }
                    }
                })
                .WithName("AggregateForcesNSquared")
                .WithBurst()
                .WithReadOnly(massCopies)
                .WithReadOnly(positionCopies)
                .WithReadOnly(allEntities)
                .Schedule(waitZeroAndCollisionJobs);

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
            disposalJob = JobHandle.CombineDependencies(disposalJob, positionCopies.Dispose(aggregateForcesNSquaredJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, partitions.Dispose(aggregateForcesNSquaredJob));
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