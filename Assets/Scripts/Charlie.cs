using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class Charlie : Agent
{
    [Header("Body Parts")]
    public Transform pelvis;
    public Transform chest;
    public Transform head;
    public Transform armL;
    public Transform forearmL;
    public Transform armR;
    public Transform forearmR;
    public Transform thighL;
    public Transform shinL;
    public Transform thighR;
    public Transform shinR;

    public Transform target;

    JointController jc;
    OrientationCubeController orientationCube;
    EnvironmentParameters resetParams;

    public float targetWalkingSpeed;
    private Vector3 walkingDirection = Vector3.right;
    private float originalDistance;

    public override void Initialize()
    {
        orientationCube = GetComponentInChildren<OrientationCubeController>();
        jc = GetComponent<JointController>();
        jc.SetUpBodyPart(pelvis);
        jc.SetUpBodyPart(chest);
        jc.SetUpBodyPart(head);
        jc.SetUpBodyPart(armL);
        jc.SetUpBodyPart(forearmL);
        jc.SetUpBodyPart(armR);
        jc.SetUpBodyPart(forearmR);
        jc.SetUpBodyPart(thighL);
        jc.SetUpBodyPart(shinL);
        jc.SetUpBodyPart(thighR);
        jc.SetUpBodyPart(shinR);

        originalDistance = Vector3.Distance(this.transform.position, target.position);
        resetParams = Academy.Instance.EnvironmentParameters;
    }

    public override void OnEpisodeBegin()
    {
        foreach (var bodyPart in jc.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        // // Random start rotation to help generalize
        // pelvis.Rotate(new Vector3(0.0f, 0.0f, Random.Range(0.0f, 360.0f)));

        UpdateWalkingDirectionAndOrientationCube();
        originalDistance = Vector3.Distance(this.transform.position, target.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var cubeForward = orientationCube.transform.forward;
        var avgVel = GetAvgVelocity();
        var velGoal = cubeForward * targetWalkingSpeed;

        // Charlie's current velocity, normalized
        sensor.AddObservation(Vector3.Distance(velGoal, avgVel));
        // Average body vel relative to cube
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(avgVel));
        // Velocity goal relative to cube
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(velGoal));

        //rotation deltas
        sensor.AddObservation(Quaternion.FromToRotation(pelvis.forward, cubeForward));
        sensor.AddObservation(Quaternion.FromToRotation(head.forward, cubeForward));

        //Position of target position relative to cube
        sensor.AddObservation(orientationCube.transform.InverseTransformPoint(target.transform.position));

        foreach (var bodyPart in jc.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
        }
    }

    public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
    {
        // Ground check
        sensor.AddObservation(bp.groundContact.touchingGround);

        // Height check
        sensor.AddObservation(bp.heightCheck.isInRange);

        // Get velocities in the context of our orientation cube's space
        // Note: You can get these velocities in world space as well but it may not train as well.
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.velocity));
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));

        //Get position relative to hips in the context of our orientation cube's space
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.position - pelvis.position));
        if (bp.rb.transform != pelvis)
        {
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / jc.maxJointForceLimit);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var bpDict = jc.bodyPartsDict;
        var i = -1;

        var continuousActions = actions.ContinuousActions;
        bpDict[chest].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);

        bpDict[thighL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[thighR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], 0);
        bpDict[shinL].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[shinR].SetJointTargetRotation(continuousActions[++i], 0, 0);

        bpDict[armL].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[armR].SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
        bpDict[forearmL].SetJointTargetRotation(continuousActions[++i], 0, 0);
        bpDict[forearmR].SetJointTargetRotation(continuousActions[++i], 0, 0);

        // update joint strength settings
        bpDict[chest].SetJointStrength(continuousActions[++i]);
        bpDict[head].SetJointStrength(continuousActions[++i]);
        bpDict[thighL].SetJointStrength(continuousActions[++i]);
        bpDict[shinL].SetJointStrength(continuousActions[++i]);
        bpDict[thighR].SetJointStrength(continuousActions[++i]);
        bpDict[shinR].SetJointStrength(continuousActions[++i]);
        bpDict[armL].SetJointStrength(continuousActions[++i]);
        bpDict[forearmL].SetJointStrength(continuousActions[++i]);
        bpDict[armR].SetJointStrength(continuousActions[++i]);
        bpDict[forearmR].SetJointStrength(continuousActions[++i]);
    }

    void FixedUpdate()
    {
        UpdateWalkingDirectionAndOrientationCube();

        DistanceReward();

        // VelocityAndLookingRewards();
    }        

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(target.tag))
        {
            SetReward(10.0f);
        }
    }

    private void DistanceReward()
    {
        float distanceToTarget = Vector3.Distance(pelvis.position, target.position);
        float distanceDiff = originalDistance - distanceToTarget;

        float normalizedDistance = Mathf.Clamp(distanceDiff / originalDistance, -1.0f, 1.0f);

        AddReward(normalizedDistance);
    }

    private void VelocityAndLookingRewards()
    {
        var cubeForward = orientationCube.transform.forward;
        var matchSpeedReward = GetMatchingVelocityReward(cubeForward * targetWalkingSpeed, GetAvgVelocity());   

        if (float.IsNaN(matchSpeedReward))
        {
            throw new ArgumentException(
                "NaN in moveTowardsTargetReward.\n" +
                $" cubeForward: {cubeForward}\n" +
                $" hips.velocity: {jc.bodyPartsDict[pelvis].rb.velocity}\n" 
            );
        }

        var headForward = head.forward;
        headForward.y = 0;
        var lookAtTargetReward = (Vector3.Dot(cubeForward, headForward) + 1) * .5F;

        if (float.IsNaN(lookAtTargetReward))
        {
            throw new ArgumentException(
                "NaN in lookAtTargetReward.\n" +
                $" cubeForward: {cubeForward}\n" +
                $" head.forward: {head.forward}"
            );
        }

        AddReward(matchSpeedReward * lookAtTargetReward);
    }

    //Returns the average velocity of all of the body parts
    //Using the velocity of the hips only has shown to result in more erratic movement from the limbs, so...
    //...using the average helps prevent this erratic movement
    Vector3 GetAvgVelocity()
    {
        Vector3 velSum = Vector3.zero;

        //ALL RBS
        int numOfRb = 0;
        foreach (var item in jc.bodyPartsList)
        {
            numOfRb++;
            velSum += item.rb.velocity;
        }

        var avgVel = velSum / numOfRb;
        return avgVel;
    }

    public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
    {
        //distance between our actual velocity and goal velocity
        var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, targetWalkingSpeed);

        //return the value on a declining sigmoid shaped curve that decays from 1 to 0
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / targetWalkingSpeed, 2), 2);
    }

    void UpdateWalkingDirectionAndOrientationCube()
    {
        orientationCube.UpdateOrientation(pelvis, target);
        walkingDirection = target.position - pelvis.position;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
    }
}
