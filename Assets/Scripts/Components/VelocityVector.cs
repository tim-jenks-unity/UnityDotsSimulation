using Unity.Entities;
using Unity.Mathematics;

namespace TJ.Components
{
    public struct VelocityVector : IComponentData
    {
        public float3 Value;
    }
}