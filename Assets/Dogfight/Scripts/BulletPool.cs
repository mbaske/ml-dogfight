using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    [SerializeField]
    private Bullet prefab;
    private Stack<Bullet> inactive;
    private Dictionary<Bullet, Transform> active;

    private void Awake()
    {
        inactive = new Stack<Bullet>();
        active = new Dictionary<Bullet, Transform>();
    }

    public void Shoot(AdvancedAgent shooter, float offset = 3f)
    {
        Transform t = shooter.transform;
        Vector3 fwd = t.forward;
        Bullet bullet = Spawn();
        bullet.transform.position = t.position + fwd * offset;
        bullet.Callback = shooter.BulletCallback;
        bullet.Shoot(fwd, shooter.IsTeamA);
        
        if (active.ContainsKey(bullet))
        {
             // TODO should never contain bullet here.
            active.Remove(bullet);
        }
        active.Add(bullet, t);
    }

    public void Discard(Bullet bullet)
    {
        active.Remove(bullet);
        bullet.Callback = null;
        bullet.gameObject.SetActive(false);
        inactive.Push(bullet);
    }

    private void Update()
    {
        foreach (KeyValuePair<Bullet, Transform> kvp in active)
        {
            kvp.Key.UpdateLine(kvp.Value.position);
        }
    }

    private Bullet Spawn()
    {
        Bullet bullet;
        if (inactive.Count > 0)
        {
            bullet = inactive.Pop();
            bullet.gameObject.SetActive(true);
            return bullet;
        }
        bullet = Instantiate(prefab, transform).GetComponent<Bullet>();
        bullet.Pool = this;
        return bullet;
    }
}
