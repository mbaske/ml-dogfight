using MLAgents;
using UnityEngine;

public class FighterAcademy : Academy
{
    [Space, SerializeField]
    private int resetInterval = 5000;

    private Agent[] agents;
    private AsteroidField[] asteroidFields;

    public override void InitializeAcademy()
    {
        agents = FindObjectsOfType<Agent>();
        asteroidFields = FindObjectsOfType<AsteroidField>();

        for (int i = 0; i < asteroidFields.Length; i++)
        {
            asteroidFields[i].Initialize();
        }
    }

    public override void AcademyStep()
    {
        int s = GetStepCount();
        if (s % resetInterval == 0 && s > 0)
        {
            for (int i = 0; i < asteroidFields.Length; i++)
            {
                asteroidFields[i].ResetBounds();
            }
            for (int i = 0; i < agents.Length; i++)
            {
                agents[i].Done();
            }
        }
    }
}
