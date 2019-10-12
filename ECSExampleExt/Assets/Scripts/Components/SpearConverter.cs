using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
public class SpearConverter : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SpearStateData { Value = SpearState.Inactive });
    }
}
