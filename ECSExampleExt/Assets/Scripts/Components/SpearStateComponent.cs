using Unity.Entities;
using Unity.Mathematics;

public struct SpearStateData : IComponentData
{
    public SpearState Value;
}

public class SpearStateComponent : ComponentDataProxy<SpearStateData> { }