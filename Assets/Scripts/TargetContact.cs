using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class contains logic for locomotion agents with joints which might make contact with a target.
/// By attaching this as a component to those joints, their contact with the ground can be used as
/// an observation for that agent.
/// </summary>
[DisallowMultipleComponent]
public class TargetContact : MonoBehaviour
{
    [Header("Detect Targets")] public bool touchingTarget;
    const string targetTag = "target";

    /// <summary>
    /// Check for collision with a target.
    /// </summary>
    void OnCollisionEnter(Collision col)
    {
        if (col.transform.CompareTag(targetTag))
        {
            touchingTarget = true;
        }
    }

    /// <summary>
    /// Check for end of ground collision and reset flag appropriately.
    /// </summary>
    void OnCollisionExit(Collision other)
    {
        if (other.transform.CompareTag(targetTag))
        {
            touchingTarget = false;
        }
    }
}
