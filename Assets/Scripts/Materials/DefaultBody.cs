using System;
using TJ.Components;
using TJ.Utility;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TJ.Materials
{
    public class DefaultBody : SingletonMonoBehaviour<DefaultBody>
    {
        public Material Material;
        public Mesh Mesh;
        public RenderMesh RenderMesh { get; private set; }
        public AABB Bounds { get; private set; }
        
        protected override DefaultBody Provide()
        {
            return this;
        }

        protected override void SingletonAwake()
        {
            RenderMesh = new RenderMesh
            {
                mesh = Mesh,
                material = Material
            };
            Bounds = Mesh.bounds.ToAABB();

            var EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
/*            const int NumberOfBodies = 100;
            var random = new Unity.Mathematics.Random(UInt32.MaxValue);
            var entities = EntityManager.CreateEntity(Archetypes.BaseBodyArchetype, NumberOfBodies, Allocator.Temp);
            for (int i = 0; i < entities.Length; ++i)
            {
                var entity = entities[i];
                EntityManager.SetSharedComponentData(entity, RenderMesh);
                EntityManager.SetComponentData(entity, new Scale {Value = 1f});
                EntityManager.SetComponentData(entity, new RenderBounds {Value = Bounds});
                EntityManager.SetComponentData(entity, new BuiltinMaterialPropertyUnity_RenderingLayer
                {
                    Value = new uint4(1, 0, 0, 0)
                });
                EntityManager.SetComponentData(entity, new BuiltinMaterialPropertyUnity_WorldTransformParams
                {
                    Value = new float4(0, 0, 0, 1)
                });
                EntityManager.SetComponentData(entity, new BuiltinMaterialPropertyUnity_LightData
                {
                    Value = new float4(0, 0, 1, 0)
                });
                var translation = new Translation {Value = ((random.NextFloat3() * 2f) - 1f) * 100f};
                EntityManager.SetComponentData(entity, translation);
                EntityManager.SetComponentData(entity, new VelocityComponent { Value = float3.zero } );
                EntityManager.SetComponentData(entity, new MassComponent { Value = 100f } );
            }
            entities.Dispose(); */

            TestBed();
        }

        private void TestBed()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var entityA = entityManager.CreateEntity(Archetypes.BaseBodyArchetype);
            var entityB = entityManager.CreateEntity(Archetypes.BaseBodyArchetype);
            var entityC = entityManager.CreateEntity(Archetypes.BaseBodyArchetype);
            var entityD = entityManager.CreateEntity(Archetypes.BaseBodyArchetype);
            
            CreateBody(entityManager, entityA, new float3(10, 0, 0), 100f);
            CreateBody(entityManager, entityB, new float3(-10, 0, 0), 100f);
            CreateBody(entityManager, entityC, new float3(-10, 0, -10), 100f);
            CreateBody(entityManager, entityD, new float3(-10, 0, 20), 100f);
        }

        private void CreateBody(EntityManager EntityManager, in Entity entity, in float3 position, in float mass)
        {
            EntityManager.SetSharedComponentData(entity, RenderMesh);
            EntityManager.SetComponentData(entity, new Scale {Value = 1f});
            EntityManager.SetComponentData(entity, new RenderBounds {Value = Bounds});
            EntityManager.SetComponentData(entity, new BuiltinMaterialPropertyUnity_RenderingLayer
            {
                Value = new uint4(1, 0, 0, 0)
            });
            EntityManager.SetComponentData(entity, new BuiltinMaterialPropertyUnity_WorldTransformParams
            {
                Value = new float4(0, 0, 0, 1)
            });
            EntityManager.SetComponentData(entity, new BuiltinMaterialPropertyUnity_LightData
            {
                Value = new float4(0, 0, 1, 0)
            });
            var translation = new Translation {Value = position};
            EntityManager.SetComponentData(entity, translation);
            EntityManager.SetComponentData(entity, new VelocityComponent {Value = float3.zero});
            EntityManager.SetComponentData(entity, new MassComponent { Value = mass });
        }
    }
}