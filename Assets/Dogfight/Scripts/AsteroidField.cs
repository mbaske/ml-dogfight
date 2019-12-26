using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Burst;

// Asteroids are animated obstacles without physics.

public class AsteroidField : MonoBehaviour
{
    [SerializeField]
    private int boundsSize = 100;
    [SerializeField]
    private int nAsteroids = 200;
    
    [SerializeField]
    private float maxRadius = 10f;
    [SerializeField]
    private float maxVelocity = 2f;
    [SerializeField]
    private float maxRotation = 2f;
    [SerializeField]
    private Asteroid prefab;

    private Bounds bounds;
    private NativeArray<Vector3> velocities;
    private NativeArray<Vector3> rotations;
    private TransformAccessArray transforms;
    private UpdateJob job;
    private JobHandle jobHandle;
    private const int lmObstacles = (1 << 8) | (1 << 9);

    public void Initialize()
    {
        InitBounds();
        velocities = new NativeArray<Vector3>(nAsteroids, Allocator.Persistent);
        rotations = new NativeArray<Vector3>(nAsteroids, Allocator.Persistent);
        Transform[] t = new Transform[nAsteroids];
        for (int i = 0; i < nAsteroids; i++)
        {
            t[i] = SpawnAsteroid();
            velocities[i] = Random.insideUnitSphere * maxVelocity;
            rotations[i] = Random.insideUnitSphere * maxRotation;
        }
        transforms = new TransformAccessArray(t);
    }

    public void ResetBounds()
    {
        InitBounds();
        Schedule();
        jobHandle.Complete();
    }

    public void UpdateBounds(Vector3 center)
    {
        bounds.center = center;
    }

    public bool IsOutOfBounds(Vector3 pos)
    {
        return !bounds.Contains(pos);
    }

    public Vector3 GetAgentSpawnPosition(Vector3 pos, float radius, float clearRadius, int retry = 0)
    {
        Vector3 p = bounds.center + pos + radius * Random.insideUnitSphere;
        if (Physics.OverlapSphere(p, clearRadius, lmObstacles).Length > 0)
        {
            if (retry < 100)
            {
                return GetAgentSpawnPosition(pos, radius, clearRadius, retry++);
            }
            Debug.LogError("Could not find spawn position");
            return pos;
        }
        return p;
    }

    private void InitBounds()
    {
        bounds = new Bounds(transform.position, Vector3.one * boundsSize);
    }

    private Transform SpawnAsteroid()
    {
        Vector3 p = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
        Asteroid a = Instantiate(prefab, p, Quaternion.identity, transform).GetComponent<Asteroid>();
        float radius = Random.value * 0.7f + 0.3f;
        a.Initialize(radius * radius * maxRadius);
        return a.transform;
    }

    private void Update()
    {
        Schedule();
    }

    private void LateUpdate()
    {
        jobHandle.Complete();
    }

    private void Schedule()
    {
        job = new UpdateJob()
        {
            deltaTime = Time.deltaTime,
            velocities = velocities,
            rotations = rotations,
            bounds = bounds
        };
        jobHandle = job.Schedule(transforms);
    }

    [BurstCompile]
    private struct UpdateJob : IJobParallelForTransform
    {
        [ReadOnly]
        public NativeArray<Vector3> velocities;
        [ReadOnly]
        public NativeArray<Vector3> rotations;
        [ReadOnly]
        public Bounds bounds;
        [ReadOnly]
        public float deltaTime;

        public void Execute(int i, TransformAccess transform)
        {
            Vector3 p = transform.position;
            p += velocities[i] * deltaTime;
            if (p.x < bounds.min.x)
            {
                p.x += bounds.size.x;
            }
            else if (p.x > bounds.max.x)
            {
                p.x -= bounds.size.x;
            }
            if (p.y < bounds.min.y)
            {
                p.y += bounds.size.y;
            }
            else if (p.y > bounds.max.y)
            {
                p.y -= bounds.size.y;
            }
            if (p.z < bounds.min.z)
            {
                p.z += bounds.size.z;
            }
            else if (p.z > bounds.max.z)
            {
                p.z -= bounds.size.z;
            }
            transform.position = p;
            Vector3 r = transform.rotation.eulerAngles + rotations[i] * deltaTime;
            transform.rotation = Quaternion.Euler(r.x, r.y, r.z);
        }
    }

    private void OnDestroy()
    {
        transforms.Dispose();
        velocities.Dispose();
        rotations.Dispose();
    }
}
