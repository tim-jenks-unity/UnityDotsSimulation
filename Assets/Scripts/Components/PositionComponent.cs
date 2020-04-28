using Unity.Entities;
using Unity.Mathematics;

namespace TJ.Components
{
    public struct PositionComponent : IComponentData
    {
        public double3 Value;
    }
}