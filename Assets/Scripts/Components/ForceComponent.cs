using Unity.Entities;
using Unity.Mathematics;

namespace TJ.Components
{
    public struct ForceComponent : IComponentData
    {
        public float3 Value;
    }
}