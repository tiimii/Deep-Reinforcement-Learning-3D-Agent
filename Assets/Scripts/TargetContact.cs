using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

[DisallowMultipleComponent]
public class TargetContact : MonoBehaviour
{
    [Header("Detect Targets")]
    const string targetTag = "target";
    public Agent agent;

    /// <summary>
    /// Check for collision with a target.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            agent.AddReward(1.0f);
            
        }
    }
}
