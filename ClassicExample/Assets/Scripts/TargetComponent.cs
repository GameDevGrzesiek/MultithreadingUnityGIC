using UnityEngine;

public class TargetComponent : MonoBehaviour
{
    public float LeftBorder = 0f;
    public float RightBorder = 0f;

    void Update()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.PingPong(Time.time * SettingsManager.TargetSpeed, Mathf.Abs(RightBorder - LeftBorder)) + LeftBorder;
        transform.position = pos;
    }
}
