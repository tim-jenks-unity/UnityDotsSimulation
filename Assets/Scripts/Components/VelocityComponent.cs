using Unity.Entities;
using Unity.Mathematics;

namespace TJ.Components
{
    public struct VelocityComponent : IComponentData
    {
        public float3 Value;
    }
}