using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public class MobMovementSystem : ComponentSystem
{
    JobHandle mobMovementJH;
    EntityQuery m_mobQuery;
    NativeArray<RaycastInput> na_rayCommands;
    NativeArray<Unity.Physics.RaycastHit> na_rayHits;

    //BuildPhysicsWorld buildPhysicsWorldSystem;

    [BurstCompile]
    public struct RaycastJob : IJobParallelFor
    {
        [ReadOnly] public CollisionWorld world;
        [ReadOnly] public NativeArray<RaycastInput> inputs;
        public NativeArray<Unity.Physics.RaycastHit> results;

        public void Execute(int index)
        {
            Unity.Physics.RaycastHit hit;
            world.CastRay(inputs[index], out hit);
            results[index] = hit;
        }
    }

    [BurstCompile]
    public static JobHandle ScheduleBatchRayCast(CollisionWorld world, NativeArray<RaycastInput> inputs, NativeArray<Unity.Physics.RaycastHit> results, JobHandle dependency)
    {
        JobHandle rcj = new RaycastJob
        {
            inputs = inputs,
            results = results,
            world = world

        }.Schedule(inputs.Length, 100, dependency);
        return rcj;
    }

    [BurstCompile]
    struct SetupRaycastJob : IJobForEachWithEntity<Translation, MobTargetPos>
    {
        [ReadOnly]
        public int layerMask;

        [ReadOnly]
        public float3 wallPos;

        [WriteOnly]
        public NativeArray<RaycastInput> rayCommands;

        public void Execute(Entity entity, int index, ref Translation translation, ref MobTargetPos mobTargetPos)
        {
            float3 shootTarget = new float3(mobTargetPos.Value.x, wallPos.y, mobTargetPos.Value.z);
            float3 dir = shootTarget - translation.Value;
            rayCommands[index] = new RaycastInput
            {
                Start = translation.Value,
                End = dir * SettingsManager.ShootingRange,
                Filter = CollisionFilter.Default
            };
        }
    }

    [BurstCompile]
    struct MobMovementJob : IJobForEachWithEntity <Translation, MobStartPos, MobTargetPos, MobStateData>
    {
        [ReadOnly]
        public float deltaTime;

        [ReadOnly]
        public NativeArray<Unity.Physics.RaycastHit> hits;

        public void Execute(Entity entity, int index, ref Translation translation, [ReadOnly] ref MobStartPos mobStartPos, 
                            [ReadOnly] ref MobTargetPos mobTargetPos, ref MobStateData mobStateData)
        {
            //float3 surface = hits[index].SurfaceNormal;
            //if (mobStateData.Value == MobState.ToTarget && (surface.x != 0 || surface.y != 0))
            if (mobStateData.Value == MobState.ToTarget && Vector3.Distance(translation.Value, mobTargetPos.Value) < 40.0f)
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

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        //buildPhysicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
    }

    protected override void OnCreate()
    {
        base.OnCreate();

        m_mobQuery = World.Active.EntityManager.CreateEntityQuery(typeof(MobStartPos), typeof(MobTargetPos), typeof(MobStateData));
        na_rayCommands = new NativeArray<RaycastInput>(0, Allocator.Persistent);
        na_rayHits = new NativeArray<Unity.Physics.RaycastHit>(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        mobMovementJH.Complete();
        na_rayCommands.Dispose();
        na_rayHits.Dispose();
    }

    protected override void OnUpdate()
    {
        int curEntityCnt = m_mobQuery.CalculateEntityCount();

        if (na_rayCommands.Length != curEntityCnt)
        {
            mobMovementJH.Complete();

            na_rayCommands.Dispose();
            na_rayCommands = new NativeArray<RaycastInput>(curEntityCnt, Allocator.Persistent);

            na_rayHits.Dispose();
            na_rayHits = new NativeArray<Unity.Physics.RaycastHit>(curEntityCnt, Allocator.Persistent);
        }

        /*var setupRaycastJob = new SetupRaycastJob
        {
            layerMask = 1 << LayerMask.NameToLayer("Wall"),
            rayCommands = na_rayCommands,
            wallPos = GameManager.instance.Target
        };*/

        //var setupJH = setupRaycastJob.Schedule(this, buildPhysicsWorldSystem.FinalJobHandle);
        //var raycastJH = ScheduleBatchRayCast(buildPhysicsWorldSystem.PhysicsWorld.CollisionWorld, na_rayCommands, na_rayHits, setupJH);

        var mobMovementJob = new MobMovementJob
        {
            deltaTime = Time.deltaTime,
            hits = na_rayHits
        };

        //mobMovementJH = mobMovementJob.Schedule(this, raycastJH);
        mobMovementJH = mobMovementJob.Schedule(this);
        mobMovementJH.Complete();

        var entities = m_mobQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < entities.Length; ++i)
        {
            if (EntityManager.GetComponentData<MobStateData>(entities[i]).Value == MobState.Throw)
            {
                var spearEntity = PostUpdateCommands.Instantiate(ECSManager.Instance.SpearPrefab);
                Vector3 startPoint = EntityManager.GetComponentData<Translation>(entities[i]).Value + (float3)SettingsManager.ThrowingPoint;
                Quaternion initialRotation = Quaternion.Euler(SettingsManager.ThrowingRotation);
                Vector3 initialVelocity = initialRotation * Vector3.forward * 75.0f;

                PostUpdateCommands.SetComponent(spearEntity, new Translation { Value = startPoint });
                PostUpdateCommands.SetComponent(spearEntity, new Rotation { Value = initialRotation });
                PostUpdateCommands.SetComponent(spearEntity, new PhysicsVelocity { Linear = initialVelocity });

                PostUpdateCommands.SetComponent(entities[i], new MobStateData { Value = MobState.FromTarget });
            }
        }
        entities.Dispose();
    }
}
