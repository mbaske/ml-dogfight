using UnityEngine;
using System;

public class Opponent : IComparable<Opponent>
{
    public AdvancedAgent Agent { get; private set; }
    public float SqrDistance { get; private set; }
    public Vector3 Direction { get; private set; }
    public float DirDot { get; private set; }

    public Opponent(AdvancedAgent agent)
    {
        Agent = agent;
    }

    public void Update(Vector3 fwd, Vector3 delta)
    {
        SqrDistance = delta.sqrMagnitude;
        Direction = delta.normalized; // world space
        DirDot = Vector3.Dot(fwd, Direction);
    }

    public int CompareTo(Opponent other)
    {
        return SqrDistance.CompareTo(other.SqrDistance);
    }
}

public class AdvancedAgent : BasicAgent
{
    public bool IsTeamA;

    private Opponent[] opponents;
    private Opponent oppFront, oppRear, oppCurrent;
    private BulletPool bulletPool;
    private float boostAcceleration;

    public override void InitializeAgent()
    {
        base.InitializeAgent();
        bulletPool = FindObjectOfType<BulletPool>();

        AdvancedAgent[] agents = FindObjectsOfType<AdvancedAgent>();
        // Assuming equally matched teams.
        opponents = new Opponent[agents.Length / 2];
        for (int i = 0, j = 0; i < agents.Length; i++)
        {
            if (agents[i].IsTeamA != IsTeamA)
            {
                opponents[j++] = new Opponent(agents[i]);
            }
        }
        oppCurrent = opponents[0];
    }

    public override void CollectObservations()
    {
        base.CollectObservations();

        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        boostAcceleration = 0;

        // Ideally, it should be up to the agent to decide which opponent it follows.
        // That would require observing a variable number (up to 19 in this case) of 
        // opponents though. To simplify things, we focus on only two - one in front, 
        // and one behind the agent.

        for (int i = 0; i < opponents.Length; i++)
        {
            opponents[i].Update(fwd, opponents[i].Agent.transform.position - pos);
        }
        Array.Sort(opponents); // by distance

        bool hasFrontOpponent = false;
        bool hasRearOpponent = false;
        // Iterate from closest to farthest.
        for (int i = 0; i < opponents.Length; i++)
        {
            if (opponents[i].DirDot > 0)
            {
                if (!hasFrontOpponent)
                {
                    oppFront = opponents[i];
                    hasFrontOpponent = true;
                }
            }
            else if (opponents[i].DirDot < 0 && !hasRearOpponent)
            {
                oppRear = opponents[i];
                hasRearOpponent = true;
            }

            if (hasFrontOpponent && hasRearOpponent)
            {
                break;
            }
        }

        AdvancedAgent oppAgent;

        if (hasFrontOpponent)
        {
            const float followMaxSqrDistance = 10000;
            if (oppFront.DirDot < oppCurrent.DirDot && oppCurrent.SqrDistance < followMaxSqrDistance)
            {
                // Keep following oppCurrent if that requires less steering,
                // even if another opponent is now closer.
                oppFront = oppCurrent;
            }
            else
            {
                // oppFront.DirDot < oppCurrent.DirDot is false if closest opponent didn't change.
                oppCurrent = oppFront;
            }

            oppAgent = oppFront.Agent;

            if (hasAgentDetection)
            {
                // Another agent was detected straight ahead (raycast).
                // +1 -> Clear shot.
                // -1 -> Blocked by team member.
                bool hasClearShot = detectedCollider == oppAgent.AgentPhysics.Collider;
                AddVectorObs(hasClearShot ? 1 : -1);
            }
            else
            {
                AddVectorObs(0);
            }

            float distance = Mathf.Sqrt(oppFront.SqrDistance);
            AddVectorObs(NormalizeDistance(distance));

            Vector3 direction = Localize(oppFront.Direction);
            AddVectorObs(Util.ToPolar(direction) / 90f); // front hemisphere -90/+90 deg

            Vector3 orientation = Localize(oppAgent.transform.forward);
            AddVectorObs(Util.ToPolar(orientation) / 180f);

            Vector3 velocity = Localize(oppAgent.AgentPhysics.Rigidbody.velocity) * velocityScale;
            AddVectorObs(Util.Sigmoid(velocity));

            // Reward forward velocity like with BasicAgent, but focus reward on opponent
            // direction. Training should start with a low exponent, can be increased later.
            const int followRewardExp = 4;
            float reward = scaledLocalVelocity.z * velocityRewardFactor;
            reward *= Util.PowInt(direction.z, followRewardExp);
            AddReward(reward);

            // Boost acceleration if ship is pointed towards opponent.
            const int boostExp = 16;
            const float boostFactor = 2f;
            boostAcceleration = Util.PowInt(direction.z, boostExp) * boostFactor;
        }
        else
        {
            // Neutral values.
            AddVectorObs(0);
            AddVectorObs(1);
            AddVectorObs(Vector2.zero);
            AddVectorObs(Vector2.zero);
            AddVectorObs(Vector3.zero);
        }

        AddVectorObs(hasFrontOpponent ? 1 : -1);

        if (hasRearOpponent)
        {
            oppAgent = oppRear.Agent;

            float distance = Mathf.Sqrt(oppRear.SqrDistance);
            AddVectorObs(NormalizeDistance(distance));

            Vector3 direction = Localize(oppRear.Direction);
            if (!hasFrontOpponent)
            {
                // Turn around to face rear opponent, direction.z is negative here.
                AddReward(direction.z);
            }
            direction.z *= -1f; // rear -> flip 
            AddVectorObs(Util.ToPolar(direction) / 90f); // rear hemisphere -90/+90 deg

            Vector3 orientation = Localize(oppAgent.transform.forward);
            AddVectorObs(Util.ToPolar(orientation) / 180f);

            Vector3 velocity = Localize(oppAgent.AgentPhysics.Rigidbody.velocity) * velocityScale;
            AddVectorObs(Util.Sigmoid(velocity));
        }
        else
        {
            // Neutral values.
            AddVectorObs(1);
            AddVectorObs(Vector2.zero);
            AddVectorObs(Vector2.zero);
            AddVectorObs(Vector3.zero);
        }
    }

    public override void AgentAction(float[] vectorAction)
    {
        vectorAction[0] *= (1f + boostAcceleration);
        base.AgentAction(vectorAction);

        if (vectorAction[3] > 0)
        {
            bulletPool.Shoot(this);
        }

        if (asteroidField.IsOutOfBounds(transform.position))
        {
            AgentReset();
        }
    }

    public void BulletCallback(bool timedOut, Collision other)
    {
        // Should start low and increase later in training.
        const float wasteAmmoPenalty = 0.1f;

        if (timedOut)
        {
            AddReward(-wasteAmmoPenalty);
        }
        else if (other.gameObject.CompareTag("Agent"))
        {
            AdvancedAgent agent = other.gameObject.GetComponent<AdvancedAgent>();
            if (agent.IsTeamA == IsTeamA)
            {
                // TODO Too many friendly fire hits.
                AddReward(-0.5f);
            }
            else
            {
                AddReward(1f);
                agent.AddReward(-1f);
            }
        }
        else
        {
            // Asteroid.
            AddReward(-wasteAmmoPenalty);
        }
    }
}
