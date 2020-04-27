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
        private const float G = 0.00000000006673f;
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            float dt = UnityEngine.Time.deltaTime;
            var allEntities = EntityManager.GetAllEntities(Allocator.TempJob);
                        
            var zeroForces = Entities.
                ForEach((ref ForceComponent force) => { force.Value = float3.zero; })
                .WithBurst()
                .Schedule(inputDeps);

            var massCDFE = GetComponentDataFromEntity<MassComponent>();
            var translationCDFE = GetComponentDataFromEntity<Translation>();
            
            var aggregateForcesJob = Entities.
                ForEach((Entity e, ref ForceComponent force, in Translation myPosition, in MassComponent myMass) =>
                {
                    for (int i = 0; i < allEntities.Length; ++i)
                    {
                        var other = allEntities[i];
                        if (other != e)
                        {
                            if (translationCDFE.HasComponent(other))
                            {
                                var theirPosition = translationCDFE[other];
                                var theirMass = massCDFE[other];
                                var delta = theirPosition.Value - myPosition.Value;
                                var distSq = math.distancesq(myPosition.Value, theirPosition.Value);
                                var f = (myMass.Value * theirMass.Value) / (distSq + 10f);
                                force.Value += f * delta / math.sqrt(distSq); // TODO: get rid of sqrt?
                            }
                        }
                    }
                })
                .WithBurst()
                .WithReadOnly(massCDFE)
                .WithReadOnly(translationCDFE)
                .WithReadOnly(allEntities)
                .Schedule(zeroForces);

            var disposalJob = allEntities.Dispose(aggregateForcesJob);
            
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
                .Schedule(aggregateForcesJob);

            return JobHandle.CombineDependencies(disposalJob, simulateJob);
        }
    }
}