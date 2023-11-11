using UnityEngine;
using Unity.MLAgents;

/// <summary>
/// This class contains logic for locomotion agents with joints which might make contact with the ground.
/// By attaching this as a component to those joints, their contact with the ground can be used as either
/// an observation for that agent, and/or a means of punishing the agent for making undesirable contact.
/// </summary>
[DisallowMultipleComponent]
public class GroundContact : MonoBehaviour
{
    [HideInInspector] public Agent agent;

    [Header("Ground Check")]
    public bool agentDoneOnGroundContact;
    public bool penalizeGroundContact;
    public float groundContactPenalty;
    public bool touchingGround;
    const string ground_tag = "ground";

    /// <summary>
    /// Check for collision with ground, and optionally penalize agent.
    /// </summary>
    void OnCollisionEnter(Collision col)
    {
        if (col.transform.CompareTag(ground_tag))
        {
            touchingGround = true;
            if (penalizeGroundContact)
            {
                agent.SetReward(groundContactPenalty);
            }

            if (agentDoneOnGroundContact)
            {
                agent.EndEpisode();
            }
        }
    }

    /// <summary>
    /// Check for end of ground collision and reset flag appropriately.
    /// </summary>
    void OnCollisionExit(Collision other)
    {
        if (other.transform.CompareTag(ground_tag))
        {
            touchingGround = false;
        }
    }
}
