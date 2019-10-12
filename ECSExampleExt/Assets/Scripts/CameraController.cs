using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    readonly Vector3 m_initialPos = new Vector3(0, 10, -40);
    readonly Vector3 m_initialRot = new Vector3(12, 0, 0);

    Vector3 m_curPos = Vector3.zero;
    Vector3 m_curRot = Vector3.zero;

    void Start()
    {
        m_curPos = m_initialPos;
        m_curRot = m_initialRot;
    }

    void Update()
    {
        this.transform.position = m_curPos;
        this.transform.eulerAngles = m_curRot;
    }

    public void MoveCam(float forward, float right)
    {
        m_curPos += transform.forward * forward + transform.right * right;
    }

    public void RotateCam(float yaw, float pitch)
    {
        m_curRot.x -= pitch;
        m_curRot.y += yaw;
    }
}
