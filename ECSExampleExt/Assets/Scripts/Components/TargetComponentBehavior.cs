using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct TargetComponentECSData : IComponentData
{
    public float LeftBorder;
    public float RightBorder;
}

public class TargetComponentBehavior : MonoBehaviour, IConvertGameObjectToEntity
{
    public float LeftBorder;
    public float RightBorder;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var targetComp = new TargetComponentECSData
        {
            LeftBorder = LeftBorder,
            RightBorder = RightBorder
        };

        GameManager.Instance.Target = transform.position;

        dstManager.AddComponentData(entity, targetComp);
    }
}

public class TargetComponentSystem : JobComponentSystem
{
    [BurstCompile]
    struct TargetComponentJob : IJobForEach<Translation, TargetComponentECSData>
    {
        [ReadOnly]
        public float time;

        public void Execute(ref Translation translation, ref TargetComponentECSData targetComp)
        {
            var pos = translation.Value;
            pos.x = Mathf.PingPong(time * SettingsManager.TargetSpeed, math.abs(targetComp.RightBorder - targetComp.LeftBorder)) + targetComp.LeftBorder;
            translation.Value = pos;
        }
    };

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var targetMoveJob = new TargetComponentJob { time = Time.time };
        return targetMoveJob.Schedule(this, inputDeps);
    }
}