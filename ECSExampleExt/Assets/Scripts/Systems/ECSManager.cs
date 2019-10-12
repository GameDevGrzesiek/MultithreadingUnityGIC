using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ECSManager : Singleton<ECSManager>
{
    public int MobCnt = 0;
    internal Entity MobPrefab;
    internal Entity SpearPrefab;
}
