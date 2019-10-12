using UnityEngine;

public enum MobState
{
    ToTarget,
    Throw,
    FromTarget
}

public enum SpearState
{
    Inactive,
    Starting,
    Active
}

public enum SimulationMode
{
    Standard,
    Extended
}

public static class SettingsManager
{
    public static float TargetSpeed = 60.0f;
    public static float ShootingRange = 50.0f;
    public static Vector3 ThrowingPoint = new Vector3(0.8f, 0.8f, 0);
    public static Vector3 ThrowingRotation = new Vector3(-45f, 0, 0);
}