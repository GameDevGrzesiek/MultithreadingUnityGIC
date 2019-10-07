using UnityEngine;

public class SpearBehavior : CustomBehaviour
{
    Rigidbody m_rBody;

    void Start()
    {
        UpdateRigidBody();
    }

    private void Awake()
    {
        UpdateRigidBody();
    }

    override public void Restart()
    {
        UpdateRigidBody();
        m_rBody.rotation = Quaternion.identity;
        m_rBody.velocity = Vector3.zero;
        m_rBody.angularVelocity = Vector3.zero;
        m_rBody.transform.rotation = Quaternion.identity;
    }

    private void UpdateRigidBody()
    {
        if (!m_rBody)
            m_rBody = this.GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (m_rBody)
            m_rBody.transform.forward = Vector3.Slerp(transform.forward, m_rBody.velocity.normalized, Time.deltaTime * 15);

        if (transform.position.y < 0)
            PoolManager.instance.SpearPool.ReturnToPool(this);
    }

    public void Throw()
    {
        m_rBody.AddForce(transform.forward * 5000);
    }
}
