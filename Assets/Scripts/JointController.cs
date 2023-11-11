using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.MLAgents;
using Unity.Mathematics;

/// <summary>
/// Used to store relevant information for acting and learning for each body part in agent.
/// </summary>
public class BodyPart
{
    [Header("Body Part Info")]
    public ConfigurableJoint joint;
    public Rigidbody rb;
    public Vector3 startingPos;
    public quaternion startingRot;

    [Header("Ground & Target Contact, Height Check")]
    public GroundContact groundContact;
    public TargetContact targetContact;
    public HeightCheck heightCheck;

    public JointController thisJointController;

    [Header("Current Joint Settings")]
    public Vector3 currentEularJointRotation;
    public float currentStrength;
    public float currentXNormalizedRot;
    public float currentYNormalizedRot;
    public float currentZNormalizedRot;

    [Header("Other Debug Info")]
    public Vector3 currentJointForce;
    public float currentJointForceSqrMag;
    public Vector3 currentJointTorque;
    public float currentJointTorqueSqrMag;
    public AnimationCurve jointForceCurve = new AnimationCurve();
    public AnimationCurve jointTorqueCurve = new AnimationCurve();

    /// <summary>
    /// Reset body part to initial configuration.
    /// <summary>
    public void Reset(BodyPart bp)
    {
        bp.rb.transform.position = bp.startingPos;
        bp.rb.transform.rotation = bp.startingRot;
        bp.rb.velocity = Vector3.zero;
        bp.rb.angularVelocity = Vector3.zero;

        if (bp.groundContact)
        {
            bp.groundContact.touchingGround = false;
        }
        if (bp.targetContact)
        {
            bp.targetContact.touchingTarget = false;
        }
        if (bp.heightCheck)
        {
            bp.heightCheck.isInRange = true;
        }
    }

    /// <summary>
    /// Apply torque according to defined goal `x, y, z` angle and force `strength`.
    /// <summary>
    public void SetJointTargetRotation(float a, float b, float c)
    {
        float x = (a + 1f) * 0.5f;
        float y = (b + 1f) * 0.5f;
        float z = (c + 1f) * 0.5f;

        var xRot = Mathf.Lerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, x);
        var yRot = Mathf.Lerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, y);
        var zRot = Mathf.Lerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, z);

        currentXNormalizedRot = Mathf.InverseLerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, xRot);
        currentYNormalizedRot = Mathf.InverseLerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, yRot);
        currentZNormalizedRot = Mathf.InverseLerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, zRot);

            joint.targetRotation = Quaternion.Euler(xRot, yRot, zRot);
            currentEularJointRotation = new Vector3(xRot, yRot, zRot);
    }

    public void SetJointStrength(float strength)
    {
        var rawVal = (strength + 1f) * 0.5f * thisJointController.maxJointForceLimit;
        var jd = new JointDrive
        {
            positionSpring = thisJointController.maxJointSpring,
            positionDamper = thisJointController.jointDampen,
            maximumForce = rawVal
        };
        joint.slerpDrive = jd;
        currentStrength = jd.maximumForce;
    }
}

public class JointController : MonoBehaviour
{
    [Header("Joint Drive Settings")]
    public float maxJointSpring;
    public float jointDampen;
    public float maxJointForceLimit;

    [HideInInspector] public Dictionary<Transform, BodyPart> bodyPartsDict = new Dictionary<Transform, BodyPart>();
    [HideInInspector] public List<BodyPart> bodyPartsList = new List<BodyPart>();

    const float maxAngularVelocity = 75.0f;

    /// <summary>
    /// Create BodyPart object and add it to dictionary.
    /// </summary>
    public void SetUpBodyPart(Transform t)
    {
        var bp = new BodyPart
        {
            rb = t.GetComponent<Rigidbody>(),
            joint = t.GetComponent<ConfigurableJoint>(),
            startingPos = t.position,
            startingRot = t.rotation
        };
        bp.rb.maxAngularVelocity = maxAngularVelocity;

        // Add & Set up the ground contact script
        bp.groundContact = t.GetComponent<GroundContact>();
        if (!bp.groundContact)
        {
            bp.groundContact = t.gameObject.AddComponent<GroundContact>();
            bp.groundContact.agent = gameObject.GetComponent<Agent>();
        }
        else
        {
            bp.groundContact.agent = gameObject.GetComponent<Agent>();
        }

         // Add & Set up the height check script
        bp.heightCheck = t.GetComponent<HeightCheck>();
        if (!bp.heightCheck)
        {
            bp.heightCheck = t.gameObject.AddComponent<HeightCheck>();
            bp.heightCheck.agent = gameObject.GetComponent<Agent>();
        }
        else
        {
            bp.heightCheck.agent = gameObject.GetComponent<Agent>();
        }

        if (bp.joint)
        {
            var jd = new JointDrive
            {
                positionSpring = maxJointSpring,
                positionDamper = jointDampen,
                maximumForce = maxJointForceLimit
            };
            bp.joint.slerpDrive = jd;
        }

        bp.thisJointController = this;
        bodyPartsDict.Add(t, bp);
        bodyPartsList.Add(bp);
    }

    public void GetCurrentJointForces()
    {
        foreach (var bodyPart in bodyPartsDict.Values)
        {
            if (bodyPart.joint)
            {
                bodyPart.currentJointForce = bodyPart.joint.currentForce;
                bodyPart.currentJointForceSqrMag = bodyPart.joint.currentForce.magnitude;
                bodyPart.currentJointTorque = bodyPart.joint.currentTorque;
                bodyPart.currentJointTorqueSqrMag = bodyPart.joint.currentTorque.magnitude;
                if (Application.isEditor)
                {
                    if (bodyPart.jointForceCurve.length > 1000)
                    {
                        bodyPart.jointForceCurve = new AnimationCurve();
                    }

                    if (bodyPart.jointTorqueCurve.length > 1000)
                    {
                        bodyPart.jointTorqueCurve = new AnimationCurve();
                    }

                    bodyPart.jointForceCurve.AddKey(Time.time, bodyPart.currentJointForceSqrMag);
                    bodyPart.jointTorqueCurve.AddKey(Time.time, bodyPart.currentJointTorqueSqrMag);
                }
            }
        }
    }
}
