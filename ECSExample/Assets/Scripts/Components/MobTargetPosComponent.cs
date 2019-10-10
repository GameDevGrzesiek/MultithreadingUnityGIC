using Unity.Entities;
using Unity.Mathematics;

public struct MobTargetPos : IComponentData
{
    public float3 Value;
}

public class MobTargetPosComponent : ComponentDataProxy<MobTargetPos> { }