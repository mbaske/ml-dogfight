using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(SphereCollider))]
public class AgentPhysics : MonoBehaviour
{
    public Rigidbody Rigidbody { get; private set; }
    public SphereCollider Collider { get; private set; }

    private const float acceleration = 2f;
    private const float pitch = 1f;
    private const float roll = 1f;

    public void Initialize()
    {
        Collider = GetComponent<SphereCollider>();
        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody.mass = 1000;
        Rigidbody.drag = 2;
        Rigidbody.angularDrag = 5;
        Rigidbody.useGravity = false;
    }

    public void AgentReset()
    {
        Rigidbody.velocity = Vector3.zero;
        Rigidbody.angularVelocity = Vector3.zero;
    }

    public void Accelerate(float v)
    {
        Rigidbody.AddForce(transform.forward * v * acceleration, ForceMode.VelocityChange);
    }

    public void Pitch(float v)
    {
        Rigidbody.AddTorque(transform.right * v * pitch, ForceMode.VelocityChange);
    }

    public void Roll(float v)
    {
        Rigidbody.AddTorque(transform.forward * v * -roll, ForceMode.VelocityChange);
    }
}
