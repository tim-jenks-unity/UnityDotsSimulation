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
        private const float G = 0.00000000006673f;
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            float dt = UnityEngine.Time.deltaTime;

            var commandBuffer = m_EndSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
            
            var allEntities = m_EntityQuery.ToEntityArray(Allocator.TempJob);
            var destroyedEntities = new NativeArray<bool>(allEntities.Length, Allocator.TempJob);
            var scaleCopies = new NativeArray<float>(allEntities.Length, Allocator.TempJob);
            var massCopies = new NativeArray<float>(allEntities.Length, Allocator.TempJob);
            var translationCopies = new NativeArray<float3>(allEntities.Length, Allocator.TempJob);

            var copyMassScaleTranslationJob = Entities.ForEach((Entity e, int entityInQueryIndex,
                in MassComponent myMass, in Scale myScale, in Translation myPosition) =>
            {
                scaleCopies[entityInQueryIndex] = myScale.Value;
                massCopies[entityInQueryIndex] = myMass.Value;
                translationCopies[entityInQueryIndex] = myPosition.Value;
            }).WithName("CopyMassScaleTranslation").Schedule(inputDeps);
                
            var processCollisionsNSquaredJob = Entities.ForEach((Entity e, int entityInQueryIndex, ref MassComponent myMass, ref Scale myScale, in Translation myPosition) =>
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
                                var theirPosition = translationCopies[i];
                                var theirMass = massCopies[i];
                                var theirScale = scaleCopies[i];
                                var dist = math.distance(myPosition.Value, theirPosition);
                                var radius = (myScale.Value + theirScale);
                                if (dist < radius)
                                {
                                    myMass.Value += theirMass;
                                    myScale.Value += theirScale;
                                    destroyedEntities[i] = true;
                                    commandBuffer.DestroyEntity(entityInQueryIndex, other);
                                }
                            }
                        }
                    }
                }
            })
            .WithName("ProcessCollisionsNSquared")
            .WithReadOnly(scaleCopies)
            .WithReadOnly(massCopies)
            .WithReadOnly(translationCopies)
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

            var aggregateForcesNSquaredJob = Entities.
                ForEach((Entity e, int entityInQueryIndex, ref ForceComponent force, in Translation myPosition, in MassComponent myMass) =>
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
                                    var theirPosition = translationCopies[i];
                                    var theirMass = massCopies[i];
                                    var delta = theirPosition - myPosition.Value;
                                    var distSq = math.distancesq(myPosition.Value, theirPosition);
                                    var f = (myMass.Value * theirMass) / (distSq + 10f);
                                    force.Value += f * delta / math.sqrt(distSq); // TODO: get rid of sqrt?
                                }
                            }
                        }
                    }
                })
                .WithName("AggregateForcesNSquared")
                .WithBurst()
                .WithReadOnly(massCopies)
                .WithReadOnly(translationCopies)
                .WithReadOnly(allEntities)
                .Schedule(waitZeroAndCollisionJobs);

            var disposalJob = allEntities.Dispose(aggregateForcesNSquaredJob);
            disposalJob = JobHandle.CombineDependencies(disposalJob, destroyedEntities.Dispose(aggregateForcesNSquaredJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, scaleCopies.Dispose(aggregateForcesNSquaredJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, massCopies.Dispose(aggregateForcesNSquaredJob));
            disposalJob = JobHandle.CombineDependencies(disposalJob, translationCopies.Dispose(aggregateForcesNSquaredJob));
            
            var simulateJob = Entities.
                ForEach((ref Translation translation,
                    ref Scale scale,
                    ref VelocityComponent velocity,
                    in ForceComponent force,
                    in MassComponent mass) =>
                {
                    velocity.Value += dt * force.Value / mass.Value;
                    translation.Value += dt * velocity.Value;
                })
                .WithBurst()
                .Schedule(aggregateForcesNSquaredJob);

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