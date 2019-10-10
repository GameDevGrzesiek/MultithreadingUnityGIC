using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class MobSpawningSystem : ComponentSystem
{
    EntityQuery m_mobQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_mobQuery = World.Active.EntityManager.CreateEntityQuery(typeof(MobStartPos), typeof(MobTargetPos), typeof(MobStateData));
    }

    protected override void OnUpdate()
    {
        int curEntityCnt = m_mobQuery.CalculateEntityCount();
        int curWantedMobCnt = ECSManager.Instance.MobCnt;

        if (curEntityCnt < curWantedMobCnt)
        {
            int diff = curWantedMobCnt - curEntityCnt;

            for (int i = 0; i < diff; ++i)
            {
                var entity = PostUpdateCommands.Instantiate(ECSManager.Instance.MobPrefab);
                var startPos = GameManager.instance.GetSpawnPosFromStart(GameManager.instance.m_defaultSpawnPos, curEntityCnt + i, 2.0f);
                var targetPos = new Vector3(startPos.x, startPos.y, GameManager.instance.Target.transform.position.z);

                PostUpdateCommands.SetComponent(entity, new Translation { Value = startPos });
                PostUpdateCommands.SetComponent(entity, new MobStartPos { Value = startPos });
                PostUpdateCommands.SetComponent(entity, new MobTargetPos { Value = targetPos });
                PostUpdateCommands.SetComponent(entity, new MobStateData { Value = MobState.ToTarget });
            }
        }
        else if (curEntityCnt > curWantedMobCnt)
        {
            int diff = curEntityCnt - curWantedMobCnt;
            var entityArray = m_mobQuery.ToEntityArray(Allocator.Temp);

            if (diff > entityArray.Length)
                diff = entityArray.Length;

            for (int i = curEntityCnt - 1; i >= curWantedMobCnt; --i)
                PostUpdateCommands.DestroyEntity(entityArray[i]);

            entityArray.Dispose();
        }
    }
}
