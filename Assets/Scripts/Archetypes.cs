using TJ.Components;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace TJ
{
    public static class Archetypes
    {
        public static readonly EntityArchetype BaseBodyArchetype;
        
        static Archetypes()
        {
            BaseBodyArchetype = World.DefaultGameObjectInjectionWorld.EntityManager.CreateArchetype(
                typeof(VelocityVector),
                
                typeof(LocalToWorld),
                typeof(WorldToLocal),
                typeof(BuiltinMaterialPropertyUnity_RenderingLayer),
                typeof(BuiltinMaterialPropertyUnity_WorldTransformParams),
                typeof(BuiltinMaterialPropertyUnity_LightData),
                typeof(Scale),
                typeof(Translation),
                typeof(RenderBounds),
                typeof(Rotation),
                typeof(RenderMesh));
        }
    }
}