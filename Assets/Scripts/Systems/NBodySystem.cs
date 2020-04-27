using TJ.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace TJ.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class NBodySystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            float dt = UnityEngine.Time.deltaTime;
            return Entities.
                ForEach((ref Translation translation, in VelocityVector velocity) =>
                {
                    translation.Value += velocity.Value * dt;
                })
                .WithBurst()
                .Schedule(inputDeps);
        }
    }
}