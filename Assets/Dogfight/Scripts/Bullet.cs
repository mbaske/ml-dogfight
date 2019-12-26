using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector]
    public Action<bool, Collision> Callback;
    [HideInInspector]
    public BulletPool Pool;

    [SerializeField]
    private float force = 1000;
    [SerializeField]
    private float lifetime = 0.25f;
    [SerializeField]
    private Color colorA = Color.blue;
    [SerializeField]
    private Color colorB = Color.red;

    private Rigidbody rb;
    private LineRenderer line;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        line = GetComponent<LineRenderer>();
    }

    public void Shoot(Vector3 dir, bool toggleColor)
    {
        rb.AddForce(dir * force, ForceMode.VelocityChange);
        line.material.SetColor("_Color", toggleColor ? colorA : colorB);
        line.enabled = true;
        Invoke("TimedOut", lifetime);
    }

    public void UpdateLine(Vector3 origin)
    {
        line.SetPosition(0, origin);
        line.SetPosition(1, transform.position);
    }

    private void OnCollisionEnter(Collision other)
    {
        CancelInvoke();
        Callback?.Invoke(false, other);
        Discard();
    }

    private void TimedOut()
    {
        Callback?.Invoke(true, null);
        Discard();
    }

    private void Discard()
    {
        line.enabled = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Pool.Discard(this);
    }
}
