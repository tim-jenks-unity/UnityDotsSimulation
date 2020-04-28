using TJ.Camera;
using TJ.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace TJ.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class NBodySystem : JobComponentSystem
    {
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

            var copyMassScaleTranslationJob = Entities.ForEach((Entity e, int entityInQueryIndex, in MassComponent myMass, in Scale myScale, in PositionComponent myPosition) =>
            {
                scaleCopies[entityInQueryIndex] = myScale.Value;
                massCopies[entityInQueryIndex] = myMass.Value;
                positionCopies[entityInQueryIndex] = myPosition.Value;
            }).WithName("CopyMassScaleTranslation").Schedule(inputDeps);
                
            var processCollisionsNSquaredJob = Entities.ForEach((Entity e, int entityInQueryIndex, ref MassComponent myMassComponent, in Scale myScale, in PositionComponent myPosition) =>
            {
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
                                var myMass = massCopies[entityInQueryIndex];
                                if (theirMass <= myMass)
                                {
                                    var theirScale = scaleCopies[i];
                                    var distSq = math.distancesq(myPosition.Value, theirPosition);
                                    var radiusSq = math.pow(myScale.Value*0.5f, 2) + math.pow(theirScale*0.5f, 2);
                                    if (distSq < radiusSq)
                                    {
                                        myMass += theirMass;
                                        massCopies[entityInQueryIndex] = myMass;
                                        myMassComponent.Value = myMass;
                                        destroyedEntities[i] = true;
                                        commandBuffer.DestroyEntity(entityInQueryIndex, other);
                                    }
                                }
                            }
                        }
                    }
                }
            })
            .WithName("ProcessCollisionsNSquared")
            .WithReadOnly(scaleCopies)
            .WithReadOnly(positionCopies)
            .WithReadOnly(allEntities)
            .WithBurst()
            .Schedule(copyMassScaleTranslationJob);
            
            m_EndSimulationEntityCommandBufferSystem.AddJobHandleForProducer(processCollisionsNSquaredJob);

            var zeroForces = Entities.
                ForEach((ref ForceComponent force) => { force.Value = float3.zero; })
                .WithName("ZeroForces")
                .WithBurst()
                .Schedule(inputDeps);

            var waitZeroAndCollisionJobs = JobHandle.CombineDependencies(zeroForces, processCollisionsNSquaredJob); 

            var updateScaleFromMassJob = Entities.
                ForEach((ref Scale scale, in MassComponent myMass) =>
                {
                    var volume = myMass.Value / 4f;
                    scale.Value = math.pow(volume * 0.75f * (1 / math.PI), 1/3f);
                })
                .WithName("UpdateScaleFromMassJob")
                .WithBurst()
                .Schedule(waitZeroAndCollisionJobs);
            
            var aggregateForcesNSquaredJob = Entities.
                ForEach((Entity e, int entityInQueryIndex, ref ForceComponent force, in PositionComponent myPosition, in MassComponent myMass) =>
                {
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