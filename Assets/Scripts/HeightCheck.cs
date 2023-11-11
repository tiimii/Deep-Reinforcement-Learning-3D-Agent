using Unity.MLAgents;
using UnityEngine;

[DisallowMultipleComponent]
public class HeightCheck : MonoBehaviour
{
    [HideInInspector] public Agent agent;
    [HideInInspector] public Transform bodyPartTransform;

    [Header("Height Check")]
    public bool checkHeightOn;
    public float minHeight;
    public float maxHeight;
    public float rewardInRange;
    public float rewardOutOfRange;
    [HideInInspector] public bool isInRange;

    public void FixedUpdate()
    {
        if (checkHeightOn) 
        {
            if (bodyPartTransform.position.y < minHeight || bodyPartTransform.position.y > maxHeight)
            {                
                isInRange = false;
                agent.SetReward(rewardOutOfRange);
            }
            else
            {
                isInRange = true;
                agent.AddReward(rewardInRange);
            }
        }
    }
}
