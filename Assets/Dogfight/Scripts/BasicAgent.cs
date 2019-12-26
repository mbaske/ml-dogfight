using UnityEngine;
using MLAgents;

// For training obstacle avoidance only.

public class BasicAgent : Agent
{
    public AgentPhysics AgentPhysics { get; private set; }

    // Not needed for BasicAgent, see CastRays() / TODO refactor.
    protected bool hasAgentDetection;
    protected Collider detectedCollider;

    [SerializeField]
    protected AsteroidField asteroidField;
    
    [SerializeField]
    private GameObject shipPrefab;

    protected const float velocityScale = 0.1f;
    protected const float velocityRewardFactor = 0.2f;
    protected Vector3 scaledLocalVelocity;

    private bool isBasic;
    private Vector3 defPos;
    private Quaternion defRot;
    private Vector3[] rayOrigins;

    public override void InitializeAgent()
    {
        isBasic = !(this is AdvancedAgent);
        defPos = transform.localPosition;
        defRot = transform.localRotation;
        Instantiate(shipPrefab, transform);
        AgentPhysics = GetComponent<AgentPhysics>();
        AgentPhysics.Initialize();
        InitRays();
    }

    public override void AgentReset()
    {
        AgentPhysics.AgentReset();
        transform.position = asteroidField.GetAgentSpawnPosition(defPos, 25, 5);
        transform.localRotation = defRot;
    }

    public override void CollectObservations()
    {
        Rigidbody rb = AgentPhysics.Rigidbody;
        // Measured max z is 48 with drag = 2, force multiplier = 2
        // Can be higher with AdvancedAgent's additional boost.
        scaledLocalVelocity = Localize(rb.velocity) * velocityScale;
        AddVectorObs(Util.Sigmoid(scaledLocalVelocity));
        // Measured max is 7 with angular drag = 5, torque multiplier = 1
        AddVectorObs(Util.Sigmoid(Localize(rb.angularVelocity) * 0.5f));

        Vector3 pos = transform.position;
        CastRays(pos);

        if (isBasic)
        {
            // Neutral values for initial training without opponents.
            AddVectorObs(0); // detected agent (raycast)
            AddVectorObs(1); // front opponent distance
            AddVectorObs(Vector2.zero); // front opponent direction
            AddVectorObs(Vector2.zero); // front opponent orientation
            AddVectorObs(Vector3.zero); // front opponent velocity
            AddVectorObs(1); // has front opponent
            AddVectorObs(1); // rear opponent distance
            AddVectorObs(Vector2.zero); // rear opponent direction 
            AddVectorObs(Vector2.zero); // rear opponent orientation 
            AddVectorObs(Vector3.zero); // rear opponent velocity 

            // Reward forward speed.
            AddReward(scaledLocalVelocity.z * velocityRewardFactor);
            
            // One agent per asteroid field.
            asteroidField.UpdateBounds(pos);
        }
    }

    public override void AgentAction(float[] vectorAction)
    {
        AgentPhysics.Accelerate(vectorAction[0]);
        AgentPhysics.Pitch(vectorAction[1]);
        AgentPhysics.Roll(vectorAction[2]);
    }

    protected Vector3 Localize(Vector3 v)
    {
        return transform.InverseTransformDirection(v);
    }

    protected float NormalizeDistance(float distance)
    {
        return Util.Sigmoid((distance - 15f) / 5f);
    }

    protected void OnCollisionEnter(Collision other)
    {
        AddReward(-1f);
    }

    private void CastRays(Vector3 pos)
    {
        const int lmAsteroid = 1 << 8;
        const int lmAgent = 1 << 8;
        const float range = 40f;
        const float radius = 2f;
        const float proxThresh = 5f;
        float proximity = 0; // <= 0
        Vector3 fwd = transform.forward;
        Quaternion rot = transform.rotation;

        // Check if there's another agent straight ahead of this one.
        // Not needed for BasicAgent, but here because it can override 
        // the first asteroid raycast result below.
        bool hasAgentHit = Physics.SphereCast(pos, radius, fwd, out RaycastHit agentHit, range, lmAgent);

        hasAgentDetection = false;
        for (int i = 0; i < 7; i++)
        {
            // Detect asteroids.
            Vector3 p = pos + rot * rayOrigins[i];
            bool hasHit = Physics.SphereCast(p, radius, fwd, out RaycastHit hit, range, lmAsteroid);

            if (hasAgentHit)
            {
                // Runs once when i = 0.
                hasAgentDetection = !hasHit || agentHit.distance < hit.distance;
                detectedCollider = agentHit.collider;
                hit = hasAgentDetection ? agentHit : hit;
                hasAgentHit = false;
                hasHit = true;
            }

            if (hasHit)
            {
                Vector3 delta = hit.point - pos;
                Vector2 polar = Util.ToPolar(Localize(delta));
                // Clamp angles to front hemisphere.
                AddVectorObs(Mathf.Clamp(polar.x / 90f, -1f, 1f));
                AddVectorObs(Mathf.Clamp(polar.y / 90f, -1f, 1f));
                float distance = delta.magnitude;
                AddVectorObs(NormalizeDistance(distance));
                // Debug.DrawRay(pos, delta, Color.yellow);

                if (distance < proxThresh)
                {
                    proximity = Mathf.Min(proximity, distance - proxThresh);
                    // Debug.DrawRay(pos, delta, Color.red);
                }
            }
            else
            {
                AddVectorObs(0);
                AddVectorObs(0);
                AddVectorObs(1);
                // Debug.DrawRay(p, fwd * range, Color.green);
            }

        }
        // Penalize closest.
        AddReward(proximity);
    }

    private void InitRays()
    {
        float s = Mathf.Sin(60 * Mathf.Deg2Rad);
        const float r = 4f;
        const float z = -3f;
        rayOrigins = new Vector3[7]
        {
            new Vector3(0, 0, z),
            new Vector3(0, -r, z),
            new Vector3(0, r, z),
            new Vector3(-s * r, r / 2f, z),
            new Vector3(-s * r, -r / 2f, z),
            new Vector3(s * r, r / 2f, z),
            new Vector3(s * r, -r / 2f, z)
        };
    }
}
