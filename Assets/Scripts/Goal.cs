using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using UnityEngine.Events;

/// <summary>
/// Utility class to allow target placement and collision detection with an agent
/// Add this script to the target you want the agent to touch.
/// Callbacks will be triggered any time the target is touched with a collider tagged as 'agent.tag'
/// </summary>
public class Goal : MonoBehaviour
{

    [Header("Agent to detect")]
    public Agent agent;

    [Header("Target Placement")]
    public float spawnRadius; //The radius in which a target can be randomly spawned.
    public bool respawnIfTouched; //Should the target respawn to a different position when touched

    [Header("Target Fell Protection")]
    public bool respawnIfFallsOffPlatform = true; //If the target falls off the platform, reset the position.
    public float fallDistance = 5; //distance below the starting height that will trigger a respawn

    private Vector3 m_startingPos; //the starting position of the target

    [System.Serializable]
    public class TriggerEvent : UnityEvent<Collider>
    {
    }

    [Header("Trigger Callbacks")]
    public TriggerEvent onTriggerEnterEvent = new TriggerEvent();
    public TriggerEvent onTriggerStayEvent = new TriggerEvent();
    public TriggerEvent onTriggerExitEvent = new TriggerEvent();

    [System.Serializable]
    public class CollisionEvent : UnityEvent<Collision>
    {
    }

    [Header("Collision Callbacks")]
    public CollisionEvent onCollisionEnterEvent = new CollisionEvent();
    public CollisionEvent onCollisionStayEvent = new CollisionEvent();
    public CollisionEvent onCollisionExitEvent = new CollisionEvent();

    // Start is called before the first frame update
    void OnEnable()
    {
        m_startingPos = transform.position;
        if (respawnIfTouched)
        {
            MoveTargetToRandomPosition();
        }
    }

    void Update()
    {
        if (respawnIfFallsOffPlatform)
        {
            if (transform.position.y < m_startingPos.y - fallDistance)
            {
                Debug.Log($"{transform.name} Fell Off Platform");
                MoveTargetToRandomPosition();
            }
        }
    }

    /// <summary>
    /// Moves target to a random position within specified radius.
    /// </summary>
    public void MoveTargetToRandomPosition()
    {
        var newTargetPos = m_startingPos + (Random.insideUnitSphere * spawnRadius);
        newTargetPos.y = m_startingPos.y;
        transform.position = newTargetPos;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.transform.CompareTag(agent.tag))
        {
            onCollisionStayEvent.Invoke(col);
        }
    }

    private void OnCollisionStay(Collision col)
    {
        if (col.transform.CompareTag(agent.tag))
        {
            onCollisionStayEvent.Invoke(col);
        }
    }

    private void OnCollisionExit(Collision col)
    {
        if (col.transform.CompareTag(agent.tag))
        {
            onCollisionExitEvent.Invoke(col);
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.transform.CompareTag(agent.tag))
        {
            onTriggerEnterEvent.Invoke(col);
            if (respawnIfTouched)
            {
                MoveTargetToRandomPosition();
            }
            else
            {
                agent.EndEpisode();    
            }
        }
    }

    private void OnTriggerStay(Collider col)
    {
        if (col.CompareTag(agent.tag))
        {
            onTriggerStayEvent.Invoke(col);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.CompareTag(agent.tag))
        {
            onTriggerExitEvent.Invoke(col);
        }
    }
}
