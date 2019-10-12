using Unity.Entities;
using Unity.Mathematics;

public struct MobStartPos : IComponentData
{
    public float3 Value;
}

public class MobStartPosComponent : ComponentDataProxy<MobStartPos> { }