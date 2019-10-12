using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class SpearMovementSystem : ComponentSystem
{
    EntityQuery m_spearQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_spearQuery = World.Active.EntityManager.CreateEntityQuery(typeof(SpearStateData));
    }
    protected override void OnUpdate()
    {
        var entities = m_spearQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < entities.Length; ++i)
        {
            if (EntityManager.GetComponentData<Translation>(entities[i]).Value.y < 0)
                PostUpdateCommands.DestroyEntity(entities[i]);
        }
        entities.Dispose();
    }
}
