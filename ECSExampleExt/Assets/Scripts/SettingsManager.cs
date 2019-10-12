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
    Start,
    Active
}

public static class SettingsManager
{
    public static readonly float TargetSpeed = 100.0f;
    public static readonly float ShootingRange = 50.0f;
    public static readonly Vector3 ThrowingPoint = new Vector3(0.8f, 0.8f, 0);
    public static readonly Vector3 ThrowingRotation = new Vector3(-45f, 0, 0);
}