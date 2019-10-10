using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MobMovementSystem : JobComponentSystem
{
    JobHandle mobMovementJH;
    EntityQuery m_mobQuery;
    NativeArray<RaycastCommand> na_rayCommands;
    NativeArray<RaycastHit> na_rayHits;

    [BurstCompile]
    struct SetupRaycastJob : IJobForEachWithEntity<Translation, MobTargetPos>
    {
        [ReadOnly]
        public int layerMask;

        [ReadOnly]
        public float3 wallPos;

        [WriteOnly]
        public NativeArray<RaycastCommand> rayCommands;

        public void Execute(Entity entity, int index, ref Translation translation, ref MobTargetPos mobTargetPos)
        {
            float3 shootTarget = new float3(mobTargetPos.Value.x, wallPos.y, mobTargetPos.Value.z);
            float3 dir = shootTarget - translation.Value;
            rayCommands[index] = new RaycastCommand(translation.Value, dir, SettingsManager.ShootingRange, layerMask);
        }
    }

    [BurstCompile]
    struct MobMovementJob : IJobForEachWithEntity <Translation, MobStartPos, MobTargetPos, MobStateData>
    {
        [ReadOnly]
        public float deltaTime;

        [ReadOnly]
        public NativeArray<RaycastHit> hits;

        public void Execute(Entity entity, int index, ref Translation translation, [ReadOnly] ref MobStartPos mobStartPos, 
                            [ReadOnly] ref MobTargetPos mobTargetPos, ref MobStateData mobStateData)
        {
            if (mobStateData.Value == MobState.ToTarget && hits[index].normal != Vector3.zero)
                mobStateData.Value = MobState.Throw;

            switch (mobStateData.Value)
            {
                case MobState.ToTarget:
                    {
                        translation.Value = Vector3.MoveTowards(translation.Value, mobTargetPos.Value, deltaTime);

                        if (Vector3.Distance(translation.Value, mobTargetPos.Value) < 2.0f)
                            mobStateData.Value = MobState.FromTarget;
                    }
                    break;

                case MobState.FromTarget:
                    {
                        translation.Value = Vector3.MoveTowards(translation.Value, mobStartPos.Value, deltaTime);

                        if (Vector3.Distance(translation.Value, mobStartPos.Value) < 2.0f)
                            mobStateData.Value = MobState.ToTarget;
                    }
                    break;
            };
        }
    }

    protected override void OnCreate()
    {
        base.OnCreate();

        m_mobQuery = World.Active.EntityManager.CreateEntityQuery(typeof(MobStartPos), typeof(MobTargetPos), typeof(MobStateData));
        na_rayCommands = new NativeArray<RaycastCommand>(0, Allocator.Persistent);
        na_rayHits = new NativeArray<RaycastHit>(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        mobMovementJH.Complete();
        na_rayCommands.Dispose();
        na_rayHits.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int curEntityCnt = m_mobQuery.CalculateEntityCount();

        if (na_rayCommands.Length != curEntityCnt)
        {
            mobMovementJH.Complete();

            na_rayCommands.Dispose();
            na_rayCommands = new NativeArray<RaycastCommand>(curEntityCnt, Allocator.Persistent);

            na_rayHits.Dispose();
            na_rayHits = new NativeArray<RaycastHit>(curEntityCnt, Allocator.Persistent);
        }

        var setupRaycastJob = new SetupRaycastJob
        {
            layerMask = 1 << LayerMask.NameToLayer("Wall"),
            rayCommands = na_rayCommands,
            wallPos = GameManager.instance.Target.transform.position
        };

        var setupJH = setupRaycastJob.Schedule(this, inputDeps);
        var raycastJH = RaycastCommand.ScheduleBatch(na_rayCommands, na_rayHits, 100, setupJH);

        var mobMovementJob = new MobMovementJob
        {
            deltaTime = Time.deltaTime,
            hits = na_rayHits
        };

        mobMovementJH = mobMovementJob.Schedule(this, raycastJH);
        mobMovementJH.Complete();

        var entities = m_mobQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < entities.Length; ++i)
        {
            if (EntityManager.GetComponentData<MobStateData>(entities[i]).Value == MobState.Throw)
            {
                Vector3 curPos = EntityManager.GetComponentData<Translation>(entities[i]).Value;
                SpearBehavior spear = PoolManager.instance.SpearPool.SpawnObject(curPos + SettingsManager.ThrowingPoint,
                                                                                 Quaternion.Euler(SettingsManager.ThrowingRotation)) as SpearBehavior;
                
                spear.Throw();
                EntityManager.SetComponentData(entities[i], new MobStateData { Value = MobState.FromTarget });
            }
        }
        entities.Dispose();

        return mobMovementJH;
    }
}
