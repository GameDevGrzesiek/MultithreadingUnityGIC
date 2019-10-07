using UnityEngine;

public class MobFightComponent : CustomBehaviour
{
    public enum MobState
    {
        ToTarget,
        Throw,
        FromTarget
    }

    internal Vector3 StartPos = Vector3.zero;
    internal Vector3 TargetPos = Vector3.zero;

    private MobState m_state;

    void Start()
    {
        m_state = MobState.ToTarget;
    }

    void Update()
    {
        Vector3 curPos = transform.position;

        switch (m_state)
        {
            case MobState.ToTarget:
            {
                curPos = Vector3.MoveTowards(curPos, TargetPos, Time.deltaTime);

                Vector3 shootTarget = new Vector3(TargetPos.x, GameManager.instance.Target.transform.position.y, TargetPos.z);
                Vector3 dir = shootTarget - curPos;

                if (Physics.Raycast(curPos, dir, SettingsManager.ShootingRange, 1 << LayerMask.NameToLayer("Wall")))
                    m_state = MobState.Throw;

                if (Vector3.Distance(curPos, TargetPos) < 2.0f)
                    m_state = MobState.FromTarget;
            } break;

            case MobState.Throw:
            {
                SpearBehavior spear = PoolManager.instance.SpearPool.SpawnObject(curPos + SettingsManager.ThrowingPoint, 
                                                                                 Quaternion.Euler(SettingsManager.ThrowingRotation)) as SpearBehavior;

                spear.Throw();
                m_state = MobState.FromTarget;
            } break;

            case MobState.FromTarget:
            {
                curPos = Vector3.MoveTowards(curPos, StartPos, Time.deltaTime);

                if (Vector3.Distance(curPos, StartPos) < 2.0f)
                    m_state = MobState.ToTarget;
            } break;
        };

        transform.position = curPos;
    }
}
