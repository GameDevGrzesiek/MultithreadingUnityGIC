using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
public class MobConverter : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new MobStartPos { Value = Vector3.zero });
        dstManager.AddComponentData(entity, new MobTargetPos { Value = Vector3.zero });
        dstManager.AddComponentData(entity, new MobStateData { Value = MobState.ToTarget });
    }
}
