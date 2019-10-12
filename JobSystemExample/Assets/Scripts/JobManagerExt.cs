using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class JobManagerExt : Singleton<JobManagerExt>
{
    #region MOB_PROPERTIES
    private TransformAccessArray taa_mobs;
    private NativeList<Vector3> nl_startPos;
    private NativeList<Vector3> nl_targetPos;
    private NativeArray<MobState> na_mobStates;
    private NativeArray<RaycastCommand> na_rayCommands;
    private NativeArray<RaycastHit> na_rayHits;

    JobHandle movementJH;
    #endregion

    #region SPEAR_PROPERTIES
    private TransformAccessArray taa_spears;
    private NativeArray<Vector3> na_spearVelocities;
    private NativeArray<SpearState> na_spearState;
    private NativeArray<RaycastCommand> na_spearRayCommands;
    private NativeArray<RaycastHit> na_spearRayHits;

    JobHandle spearMovementJH;
    #endregion

    #region MOB_JOBS
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
    #endregion

    #region SPEAR_JOBS


    struct SpearGravityJob : IJobParallelForTransform
    {
        [ReadOnly]
        public float deltaTime;

        [ReadOnly]
        public Vector3 Gravity;

        public NativeArray<SpearState> State;

        public NativeArray<Vector3> Velocities;

        public void Execute(int i, TransformAccess transform)
        {
            switch (State[i])
            {
                case SpearState.Active:
                {
                    Velocities[i] += Gravity * deltaTime;
                }
                break;

                case SpearState.Inactive:
                {
                    Velocities[i] = Vector3.zero;
                    transform.position = Vector3.zero;
                }
                break;

                case SpearState.Starting:
                {
                    Vector3 forward = transform.rotation * Vector3.forward;
                    Velocities[i] = forward * 11.0f;
                    State[i] = SpearState.Active;
                }
                break;
            };
        }
    }

    struct SpearRaycastSetupJob : IJobParallelForTransform
    {
        public float deltaTime;
        public int layer;
        
        [ReadOnly]
        public NativeArray<Vector3> Velocities;

        public NativeArray<RaycastCommand> Raycasts;

        public void Execute(int i, TransformAccess transform)
        {
            float distance = (Velocities[i] * deltaTime).magnitude;
            Raycasts[i] = new RaycastCommand(transform.position, Velocities[i], distance, layer);
        }
    }

    struct SpearMovementJob : IJobParallelForTransform
    {
        [ReadOnly]
        public float DeltaTime;

        public NativeArray<Vector3> Velocities;

        [ReadOnly]
        public NativeArray<RaycastHit> Hits;

        public NativeArray<SpearState> State;

        public void Execute(int i, TransformAccess transform)
        {
            if (State[i] != SpearState.Active)
                return;

            if (Hits[i].normal == Vector3.zero)
            {
                transform.position += Velocities[i] * (Velocities[i] * DeltaTime).magnitude;

                if (transform.position.z > 200.0f)
                {
                    transform.position = new Vector3(transform.position.x, transform.position.y, 198.0f);
                    Velocities[i] = Vector3.Reflect(Velocities[i], transform.rotation * Vector3.forward);
                    var angleBetweenNormalAndUp = Vector3.Angle(Hits[i].normal, Vector3.up);
                    var lerp = angleBetweenNormalAndUp / 180f;
                    Velocities[i] = Velocities[i] * Mathf.Lerp(0.5f, 1f, lerp);
                }
            }
            else
            {
                transform.position = Hits[i].point + new Vector3(0, 0.1f, 0);
            }

            if (transform.position.y < 0)
                State[i] = SpearState.Inactive;
        }
    }

    struct SpearCalculateResponse : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<RaycastHit> Hits;

        [ReadOnly]
        public NativeArray<SpearState> States;

        public NativeArray<Vector3> Velocities;

        public void Execute(int i)
        {
            if (States[i] != SpearState.Active)
                return;

            if (Hits[i].normal != Vector3.zero)
            {
                Velocities[i] = Vector3.Reflect(Velocities[i], Hits[i].normal);
                var angleBetweenNormalAndUp = Vector3.Angle(Hits[i].normal, Vector3.up);
                var lerp = angleBetweenNormalAndUp / 180f;
                Velocities[i] = Velocities[i] * Mathf.Lerp(0.5f, 1f, lerp);
            }
        }
    }

    #endregion

    void Start() {}

    private void OnDestroy()
    {
        #region MOB_DISPOSE
        taa_mobs.Dispose();
        nl_startPos.Dispose();
        nl_targetPos.Dispose();
        na_mobStates.Dispose();
        na_rayCommands.Dispose();
        na_rayHits.Dispose();
        #endregion

        #region SPEAR_DISPOSE
        taa_spears.Dispose();
        na_spearVelocities.Dispose();
        na_spearState.Dispose();
        na_spearRayCommands.Dispose();
        na_spearRayHits.Dispose();
        #endregion
    }

    private void Update()
    {
        if (!taa_mobs.isCreated || taa_mobs.length == 0 ||
                nl_startPos.Length != taa_mobs.length ||
                nl_targetPos.Length != taa_mobs.length ||
                na_mobStates.Length != taa_mobs.length)
            return;

        #region ARRAY_SETUP
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

        if (!na_spearRayCommands.IsCreated || na_spearRayCommands.Length != taa_spears.length)
        {
            spearMovementJH.Complete();

            if (na_spearRayCommands.IsCreated)
                na_spearRayCommands.Dispose();

            if (na_spearRayHits.IsCreated)
                na_spearRayHits.Dispose();

            na_spearRayCommands = new NativeArray<RaycastCommand>(taa_spears.length, Allocator.Persistent);
            na_spearRayHits = new NativeArray<RaycastHit>(taa_spears.length, Allocator.Persistent);
        }

        #endregion

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

        var spearGravityJob = new SpearGravityJob
        {
            deltaTime = Time.deltaTime,
            Gravity = Physics.gravity,
            State = na_spearState,
            Velocities = na_spearVelocities
        };

        var spearGravityJH = spearGravityJob.Schedule(taa_spears, movementJH);

        var spearRaycastSetup = new SpearRaycastSetupJob
        {
            deltaTime = Time.deltaTime,
            layer = 1 << LayerMask.NameToLayer("Wall"),
            Raycasts = na_spearRayCommands,
            Velocities = na_spearVelocities
        };

        var spearRaySetupJH = spearRaycastSetup.Schedule(taa_spears, spearGravityJH);

        var spearRaycastBatchJH = RaycastCommand.ScheduleBatch(na_spearRayCommands, na_spearRayHits, 100, spearRaySetupJH);

        var spearCollisionResponse = new SpearCalculateResponse
        {
            Hits = na_spearRayHits,
            States = na_spearState,
            Velocities = na_spearVelocities
        };

        var responseJH = spearCollisionResponse.Schedule(taa_spears.length, 100, spearRaycastBatchJH);

        var spearMovementJob = new SpearMovementJob
        {
            DeltaTime = Time.deltaTime,
            Hits = na_spearRayHits,
            State = na_spearState,
            Velocities = na_spearVelocities
        };

        spearMovementJH = spearMovementJob.Schedule(taa_spears, responseJH);

        spearMovementJH.Complete();

        for (int i = 0; i < na_mobStates.Length; ++i)
        {
            if (na_mobStates[i] == MobState.Throw)
            {
                SpearBehaviorExt spear = PoolManager.instance.SpearPool.SpawnObject(taa_mobs[i].position + SettingsManager.ThrowingPoint,
                                                                                     Quaternion.Euler(SettingsManager.ThrowingRotation)) as SpearBehaviorExt;

                if (spear)
                {
                    int spearIndex = PoolManager.instance.SpearPool.IndexOf(spear);
                    na_spearState[spearIndex] = SpearState.Starting;
                    taa_spears[spearIndex].position = taa_mobs[i].position + SettingsManager.ThrowingPoint;
                    taa_spears[spearIndex].rotation = Quaternion.Euler(SettingsManager.ThrowingRotation);
                    na_mobStates[i] = MobState.FromTarget;
                }
            }
        }

        for (int i = 0; i < na_spearState.Length; ++i)
        {
            if (na_spearState[i] == SpearState.Inactive)
            {
                var spear = PoolManager.Instance.SpearPool.GetAt(i);
                if (spear && spear.isActiveAndEnabled)
                    PoolManager.Instance.SpearPool.ReturnToPool(spear);
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
        spearMovementJH.Complete();

        if (!taa_spears.isCreated)
            taa_spears = new TransformAccessArray(0);

        if (!na_spearVelocities.IsCreated)
            na_spearVelocities = new NativeArray<Vector3>(0, Allocator.Persistent);

        if (!na_spearState.IsCreated)
            na_spearState = new NativeArray<SpearState>(0, Allocator.Persistent);

        PoolManager.instance.SpearPool.Expand(spearCnt);

        int oldMobCnt = PoolManager.instance.SpearPool.m_cnt - spearCnt;

        for (int i = 0; i < spearCnt; ++i)
            taa_spears.Add(PoolManager.instance.SpearPool.GetAt(oldMobCnt + i).transform);

        if (na_spearVelocities.Length > 0)
        {
            var tempSpearVelocities = new NativeArray<Vector3>(na_spearVelocities, Allocator.Temp);
            na_spearVelocities.Dispose();
            na_spearVelocities = new NativeArray<Vector3>(tempSpearVelocities.Length + spearCnt, Allocator.Persistent);
            NativeArray<Vector3>.Copy(tempSpearVelocities, 0, na_spearVelocities, 0, tempSpearVelocities.Length);
        }
        else
        {
            na_spearVelocities.Dispose();
            na_spearVelocities = new NativeArray<Vector3>(spearCnt, Allocator.Persistent);
        }

        if (na_spearState.Length > 0)
        {
            var tempSpearActive = new NativeArray<SpearState>(na_spearState, Allocator.Temp);
            na_spearState.Dispose();
            na_spearState = new NativeArray<SpearState>(tempSpearActive.Length + spearCnt, Allocator.Persistent);
            NativeArray<SpearState>.Copy(tempSpearActive, 0, na_spearState, 0, tempSpearActive.Length);
        }
        else
        {
            na_spearState.Dispose();
            na_spearState = new NativeArray<SpearState>(spearCnt, Allocator.Persistent);
        }

        for (int i = oldMobCnt; i < na_spearState.Length; ++i)
            na_spearState[i] = SpearState.Inactive;
    }

    internal void RemoveSpearsFromSystem(int spearCnt)
    {
        spearMovementJH.Complete();

        for (int i = 0; i < spearCnt; ++i)
            taa_spears.RemoveAtSwapBack(taa_spears.length - 1);

        int maxChange = spearCnt;

        if (maxChange > na_spearVelocities.Length)
        {
            na_spearVelocities.Dispose();
            na_spearVelocities = new NativeArray<Vector3>(0, Allocator.Persistent);
        }
        else
        {
            int newSize = na_spearVelocities.Length - spearCnt;
            var tempVelocities = new NativeArray<Vector3>(newSize, Allocator.Temp);
            NativeArray<Vector3>.Copy(na_spearVelocities, 0, tempVelocities, 0, newSize);
            na_spearVelocities.Dispose();
            na_spearVelocities = new NativeArray<Vector3>(newSize, Allocator.Persistent);
            tempVelocities.CopyTo(na_spearVelocities);
        }

        if (maxChange > na_spearState.Length)
        {
            na_spearState.Dispose();
            na_spearState = new NativeArray<SpearState>(0, Allocator.Persistent);
        }
        else
        {
            int newSize = na_spearState.Length - spearCnt;
            var tempActives = new NativeArray<SpearState>(newSize, Allocator.Temp);
            NativeArray<SpearState>.Copy(na_spearState, 0, tempActives, 0, newSize);
            na_spearState.Dispose();
            na_spearState = new NativeArray<SpearState>(newSize, Allocator.Persistent);
            tempActives.CopyTo(na_spearState);
        }

        PoolManager.instance.SpearPool.Expand(-spearCnt);
    }
}
