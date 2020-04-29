using System;
using TJ.Components;
using TJ.Utility;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

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

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            const int NumberOfBodies = 10000;
            var random = new Unity.Mathematics.Random(UInt32.MaxValue);
            var entities = entityManager.CreateEntity(Archetypes.BaseBodyArchetype, NumberOfBodies, Allocator.Temp);
            for (int i = 0; i < entities.Length; ++i)
            {
                var entity = entities[i];
                var position = (double3)math.normalize((random.NextFloat3() * 2f) - 1f) * 25f;
                CreateBody(entityManager, entity, position, 0.25f);
            }
            entities.Dispose();

            //TestBed();
        }

        private void TestBed()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var entityA = entityManager.CreateEntity(Archetypes.BaseBodyArchetype);
            var entityB = entityManager.CreateEntity(Archetypes.BaseBodyArchetype);
            var entityC = entityManager.CreateEntity(Archetypes.BaseBodyArchetype);
            var entityD = entityManager.CreateEntity(Archetypes.BaseBodyArchetype);
            
            CreateBody(entityManager, entityA, new double3(10, 0, 0), 100f);
            CreateBody(entityManager, entityB, new double3(-10, 0, 0), 100f);
            CreateBody(entityManager, entityC, new double3(-10, 0, -10), 100f);
            CreateBody(entityManager, entityD, new double3(-10, 0, 20), 100f);
        }

        private void CreateBody(EntityManager EntityManager, in Entity entity, in double3 initialposition, in float mass)
        {
            EntityManager.SetSharedComponentData(entity, RenderMesh);
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
            var position = new PositionComponent {Value = initialposition};
            EntityManager.SetComponentData(entity, position);
            EntityManager.SetComponentData(entity, new VelocityComponent { Value = (float3)math.normalize(position.Value)*5f });
            //EntityManager.SetComponentData(entity, new VelocityComponent { Value = float3.zero });
            EntityManager.SetComponentData(entity, new MassComponent { Value = mass });
        }
    }
}