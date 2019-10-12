using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class JobManager : Singleton<JobManager>
{
    private TransformAccessArray taa_mobs;
    private NativeList<Vector3> nl_startPos;
    private NativeList<Vector3> nl_targetPos;
    private NativeArray<MobState> na_mobStates;
    private NativeArray<RaycastCommand> na_rayCommands;
    private NativeArray<RaycastHit> na_rayHits;

    JobHandle movementJH;

    struct SetupRaycastJob : IJobParallelForTransform
    {
        [ReadOnly]
        public int layerMask;

        [ReadOnly]
        public Vector3 wallPos;

        [ReadOnly]
        public NativeList<Vector3> targetPos;

        [WriteOnly]
        public NativeArray<RaycastCommand> rayCommands;

        public void Execute(int index, TransformAccess transform)
        {
            Vector3 shootTarget = new Vector3(targetPos[index].x, wallPos.y, targetPos[index].z);
            Vector3 dir = shootTarget - transform.position;
            rayCommands[index] = new RaycastCommand(transform.position, dir, SettingsManager.ShootingRange, layerMask);
        }
    }

    struct MobMovementJob : IJobParallelForTransform
    {
        [ReadOnly]
        public float deltaTime;

        [ReadOnly]
        public NativeList<Vector3> startPos;

        [ReadOnly]
        public NativeList<Vector3> targetPos;

        [ReadOnly]
        public NativeArray<RaycastHit> hits;

        public NativeArray<MobState> mobStates;

        public void Execute(int index, TransformAccess transform)
        {
            Vector3 curPos = transform.position;

            if (mobStates[index] == MobState.ToTarget && hits[index].normal != Vector3.zero)
                mobStates[index] = MobState.Throw;

            switch (mobStates[index])
            {
                case MobState.ToTarget:
                    {
                        curPos = Vector3.MoveTowards(curPos, targetPos[index], deltaTime);

                        if (Vector3.Distance(curPos, targetPos[index]) < 2.0f)
                            mobStates[index] = MobState.FromTarget;
                    }
                    break;

                case MobState.FromTarget:
                    {
                        curPos = Vector3.MoveTowards(curPos, startPos[index], deltaTime);

                        if (Vector3.Distance(curPos, startPos[index]) < 2.0f)
                            mobStates[index] = MobState.ToTarget;
                    }
                    break;
            };

            transform.position = curPos;
        }
    }

    void Start() {}

    private void OnDestroy()
    {
        taa_mobs.Dispose();
        nl_startPos.Dispose();
        nl_targetPos.Dispose();
        na_mobStates.Dispose();
        na_rayCommands.Dispose();
        na_rayHits.Dispose();
    }

    private void Update()
    {
        if (!taa_mobs.isCreated || taa_mobs.length == 0 ||
                nl_startPos.Length != taa_mobs.length ||
                nl_targetPos.Length != taa_mobs.length ||
                na_mobStates.Length != taa_mobs.length)
            return;

        if (!na_rayCommands.IsCreated || na_rayCommands.Length != taa_mobs.length)
        {
            movementJH.Complete();

            if (na_rayCommands.IsCreated)
                na_rayCommands.Dispose();

            if (na_rayHits.IsCreated)
                na_rayHits.Dispose();

            na_rayCommands = new NativeArray<RaycastCommand>(taa_mobs.length, Allocator.Persistent);
            na_rayHits = new NativeArray<RaycastHit>(taa_mobs.length, Allocator.Persistent);
        }

        var setupRaycastJob = new SetupRaycastJob
        {
            layerMask = 1 << LayerMask.NameToLayer("Wall"),
            rayCommands = na_rayCommands,
            targetPos = nl_targetPos,
            wallPos = GameManager.instance.Target.transform.position
        };

        var setupJH = setupRaycastJob.Schedule(taa_mobs);
        var raycastJH = RaycastCommand.ScheduleBatch(na_rayCommands, na_rayHits, 100, setupJH);

        var mobMovementJob = new MobMovementJob
        {
            deltaTime = Time.deltaTime,
            hits = na_rayHits,
            mobStates = na_mobStates,
            startPos = nl_startPos,
            targetPos = nl_targetPos
        };

        movementJH = mobMovementJob.Schedule(taa_mobs, raycastJH);

        movementJH.Complete();

        for (int i = 0; i < na_mobStates.Length; ++i)
        {
            if (na_mobStates[i] == MobState.Throw)
            {
                SpearBehavior spear = PoolManager.instance.SpearPool.SpawnObject(taa_mobs[i].position + SettingsManager.ThrowingPoint,
                                                                                 Quaternion.Euler(SettingsManager.ThrowingRotation)) as SpearBehavior;

                spear.Throw();
                na_mobStates[i] = MobState.FromTarget;
            }
        }
    }

    internal void AddMobsToSystem(int mobCnt)
    {
        movementJH.Complete();

        if (!taa_mobs.isCreated)
            taa_mobs = new TransformAccessArray(0);

        if (na_mobStates == null || !na_mobStates.IsCreated)
            na_mobStates = new NativeArray<MobState>(0, Allocator.Persistent);

        if (!nl_startPos.IsCreated)
            nl_startPos = new NativeList<Vector3>(Allocator.Persistent);

        if (!nl_targetPos.IsCreated)
            nl_targetPos = new NativeList<Vector3>(Allocator.Persistent);

        int oldMobCnt = PoolManager.instance.MobPool.m_cnt - mobCnt;

       for (int i = 0; i < mobCnt; ++i)
        {
            var startPos = GameManager.instance.GetSpawnPosFromStart(GameManager.instance.m_defaultSpawnPos, oldMobCnt + i, 2.0f);
            var SpawnedMob = PoolManager.instance.MobPool.SpawnObject(startPos, Quaternion.identity) as MobFightComponent;
            taa_mobs.Add(SpawnedMob.transform);

            nl_startPos.Add(startPos);
            nl_targetPos.Add(new Vector3(startPos.x, startPos.y, GameManager.instance.Target.transform.position.z));
        }

        if (na_mobStates.Length > 0)
        {
            var tempMobState = new NativeArray<MobState>(na_mobStates, Allocator.Temp);
            na_mobStates.Dispose();
            na_mobStates = new NativeArray<MobState>(tempMobState.Length + mobCnt, Allocator.Persistent);
            NativeArray<MobState>.Copy(tempMobState, 0, na_mobStates, 0, tempMobState.Length);
        }
        else
        {
            na_mobStates.Dispose();
            na_mobStates = new NativeArray<MobState>(mobCnt, Allocator.Persistent);
        }

        for (int i = oldMobCnt; i < na_mobStates.Length; ++i)
            na_mobStates[i] = MobState.ToTarget;

        if (PoolManager.instance.SpearPool.m_cnt < PoolManager.instance.MobPool.m_cnt)
        {
            int diff = PoolManager.instance.MobPool.m_cnt - PoolManager.instance.SpearPool.m_cnt;
            AddSpearsToSystem(diff);
        }
    }

    internal void RemoveMobsFromSystem(int mobCnt)
    {
        movementJH.Complete();

        for (int i = 0; i < mobCnt; ++i)
        {
            if (taa_mobs.length > 0)
                taa_mobs.RemoveAtSwapBack(taa_mobs.length - 1);

            if (nl_startPos.Length > 0)
                nl_startPos.RemoveAtSwapBack(nl_startPos.Length - 1);

            if (nl_targetPos.Length > 0)
                nl_targetPos.RemoveAtSwapBack(nl_targetPos.Length - 1);
        }

        int maxChange = mobCnt;
        if (maxChange > na_mobStates.Length)
        {
            na_mobStates.Dispose();
            na_mobStates = new NativeArray<MobState>(0, Allocator.Persistent);
        }
        else
        {
            int newSize = na_mobStates.Length - mobCnt;
            var tempStates = new NativeArray<MobState>(newSize, Allocator.Temp);
            NativeArray<MobState>.Copy(na_mobStates, 0, tempStates, 0, newSize);
            na_mobStates.Dispose();
            na_mobStates = new NativeArray<MobState>(newSize, Allocator.Persistent);
            tempStates.CopyTo(na_mobStates);
        }

        PoolManager.instance.MobPool.Expand(-mobCnt);

        RemoveSpearsFromSystem(mobCnt);
    }

    internal void AddSpearsToSystem(int spearCnt)
    {
        PoolManager.instance.SpearPool.Expand(spearCnt);
    }

    internal void RemoveSpearsFromSystem(int spearCnt)
    {
        PoolManager.instance.SpearPool.Expand(-spearCnt);
    }
}
