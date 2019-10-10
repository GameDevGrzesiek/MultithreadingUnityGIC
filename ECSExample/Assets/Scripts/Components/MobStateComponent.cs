using Unity.Entities;
using Unity.Mathematics;

public struct MobStateData : IComponentData
{
    public MobState Value;
}

public class MobStateDataComponent : ComponentDataProxy<MobStateData> { }