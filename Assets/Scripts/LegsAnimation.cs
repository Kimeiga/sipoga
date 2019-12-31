using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LegsAnimation : MonoBehaviour
{
    // private Rigidbody rigidbody;

    public Transform leftLegJoint;
    public Transform rightLegJoint;

    // used to see if the distance between the foot and the foot target is beyond a certain threshold.
    // if so, then the foot target is moved to a new position in the direction of movement. 
    public Transform leftFoot;
    public Transform rightFoot;


    public Vector3 leftFootTarget;
    public Vector3 rightFootTarget;

    // the amount of distance between the feet and their respective targets required to move the target to a new location.
    public float stepThreshold = 0.5f;
    public float runSpeed = 5;

    // the original positions are needed because we will add the velocity to these positions to find the new
    // positions for the foot targets to move to
    public Transform leftFootTargetStartPosition;
    public Transform rightFootTargetStartPosition;

    public Vector3 leftLegJointGroundPosition;
    public Vector3 rightLegJointGroundPosition;
    
    public Vector3 leftFootTargetOriginalPosition;
    public Vector3 rightFootTargetOriginalPosition;

    public float stepSize; // amount of distance to travel before taking step (?)
    public float firstStepSize; // amount of distance to travel before taking first step

    // should the character move their right foot first after just starting or landing on the ground 
    public bool rightFootFirst = true;

    // has the character taken their first step after just starting or landing on the ground?
    private bool firstStepTaken = false;
    private Vector3 firstPosition;
    public float firstStepThreshold = 0.5f;

    private Vector3 velocity;
    private Vector3 lastPosition;

    public float stepMod = 1;


    // Start is called before the first frame update
    void Start()
    {
        // leftFootTargetStartPosition.position = leftFoot.position;
        // rightFootTargetStartPosition.position = rightFoot.position;

        leftFootTargetOriginalPosition = transform.position - leftFoot.position;
        rightFootTargetOriginalPosition = transform.position - rightFoot.position;
        
        leftFootTarget = leftFootTargetStartPosition.position;
        rightFootTarget = rightFootTargetStartPosition.position;

        // assumes that this script is located on the root prefab
        firstPosition = transform.position;

        lastPosition = transform.position;

        leftLegJointGroundPosition =
            transform.InverseTransformPoint(new Vector3(leftLegJoint.position.x, 0, leftLegJoint.position.z));
        
        rightLegJointGroundPosition =
            transform.InverseTransformPoint(new Vector3(rightLegJoint.position.x, 0, rightLegJoint.position.z));
    }

    // Update is called once per frame
    void Update()
    {
        leftLegJoint.LookAt(leftFootTarget);
        rightLegJoint.LookAt(rightFootTarget);
    }

    private void FixedUpdate()
    {
        // try not having a special case for first step and just space out the feet at the start!
        
        // if (firstStepTaken == false && Vector3.Distance(firstPosition, transform.position) > firstStepThreshold)
        // {
        //     // you either just started or have just landed from a jump and you are ready to take your first step (how exciting :) )
        //
        //     if (rightFootFirst)
        //     {
        //         // step first with your right foot
        //         StepWithRightFoot();
        //     }
        //     else
        //     {
        //         StepWithLeftFoot();
        //     }
        //
        //     firstStepTaken = true;
        // }

        // instead of measuring individual foot distances, measure distance from transform 
        if (Vector3.Distance(transform.TransformPoint(rightLegJointGroundPosition), rightFootTarget) > stepThreshold)
        {
            // time to move right foot
            StepWithRightFoot();
        }
        
        
        if (Vector3.Distance(transform.TransformPoint(leftLegJointGroundPosition), leftFootTarget) > stepThreshold)
        {
            // time to move left foot
            StepWithLeftFoot();
        }

        velocity = transform.position - lastPosition;
        lastPosition = transform.position;
    }

    private void StepWithRightFoot()
    {
        print("Stepwithrightfoot");
        Vector3 v = new Vector3();

        // I hope doing it this way will save us from having to keep track of the velocity ourselves.
        // But I guess I'm not sure if they are going to return comparable velocities...
        v = new Vector3(velocity.x, 0, velocity.z);

        print(v);
        // We could move the targets farther if the velocity is larger (within some range)
        // Or we could just use the Run vs Walk vs Crouch modes and do casework on them.
        // I think the velocity way is easier but we have to have a parameter for Run Speed and it has to be up to date
        // with the regular run speed (at least a little bit)

        // Run Speed (5) => max distance (1 (?))
        float distanceToMove = stepMod * v.magnitude / runSpeed;

        Vector3 newPosition =
            // transform.TransformPoint(rightFootTargetOriginalPosition) +
            transform.TransformPoint(rightLegJointGroundPosition) + 
            (v.normalized *
             stepMod); // original position + offset (distanceToMove m in the v (velocity) direction)

        rightFootTarget = newPosition;
    }

    private void StepWithLeftFoot()
    {
        print("Stepwithleftfoot");
        Vector3 v = new Vector3();

        // I hope doing it this way will save us from having to keep track of the velocity ourselves.
        // But I guess I'm not sure if they are going to return comparable velocities...
        v = new Vector3(velocity.x, 0, velocity.z);

        print(v);
        // We could move the targets farther if the velocity is larger (within some range)
        // Or we could just use the Run vs Walk vs Crouch modes and do casework on them.
        // I think the velocity way is easier but we have to have a parameter for Run Speed and it has to be up to date
        // with the regular run speed (at least a little bit)

        // Run Speed (5) => max distance (1 (?))
        float distanceToMove = stepMod * v.magnitude / runSpeed;

        Vector3 newPosition =
            // transform.TransformPoint(leftFootTargetOriginalPosition) +
            transform.TransformPoint(leftLegJointGroundPosition) +
            (v.normalized *
             stepMod); // original position + offset (distanceToMove m in the v (velocity) direction)

        leftFootTarget = newPosition;
    }

    private void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(rightFootTarget, 0.2f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(leftFootTarget, 0.2f);
    }
}