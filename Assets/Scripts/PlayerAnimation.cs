using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;

public class PlayerAnimation : MonoBehaviour
{
    /// <summary>
    /// This script is for the "simple" style of animation rather than the procedural stuff I was trying to do earlier.
    /// I will rotate the Legs Joint to face the direction of movement (or away from it if moving backwards).
    /// I will play the relevant animation in a single direction (run, walk, idle, jump-rise (rotate away from direction
    /// of movement), jump-fall (rotate towards direction of movement).
    /// This means that...
    /// ...the "(away from it if moving backwards)" only applies on the ground, in the air we have to rotate towards
    /// the direction of movement the entire time to ensure jumping backwards looks right.
    /// </summary>

    public bool isAI = false;
    private FPSWalker fpsWalkerScript;
    private NavMeshAgent navMeshAgent;
    
    public Animation legsAnimation;

    public Transform legsJoint;
    public Quaternion legsJointPreviousRotation;
    
    // for jump rising
    public Transform leftLegJoint;
    public Transform rightLegJoint;
    private Quaternion legOriginalRotation; // I am just making one because they should be identical

    
    public float runSpeed = 5;

    public bool grounded = true;
    private bool previousGrounded = true;
    public Vector3 jumpPoint;
    
    public float fallLegAngleMod = 400;
    // so it is Player/Bot agnostic
    public Vector3 velocity;
    private Vector3 lastPosition;

    // Start is called before the first frame update
    void Start()
    {
        // for jump-falling
        legOriginalRotation = leftLegJoint.localRotation;

        if (isAI)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }
        else
        {
            fpsWalkerScript = GetComponent<FPSWalker>();
        }

        legsJointPreviousRotation = transform.rotation;
        legsJoint.rotation = legsJointPreviousRotation;

        DOTween.Init();
    }

    private void FixedUpdate()
    {
        // Assumes script is on root of player
        Vector3 pos = transform.position;
        
        // Ground movement
        // we should get grounded from FPSWalker or NavMeshAgent
        // not sure if isOnNavMesh is best, but I think it's correct
        grounded = isAI ? navMeshAgent.isOnNavMesh : fpsWalkerScript.grounded;

        if (!grounded && previousGrounded)
        {
            // means you just jumped off the ground, so we need to record that as the jump point
            // TODO Problem: jumpPoint is not being set fast enough, so we might have to import it from another script,
            // like FPSWalker and whatever script causes bot to jump... 
            // jumpPoint = transform.position;
        }
        
        velocity = pos - lastPosition;
        lastPosition = pos;
    }

    // Update is called once per frame
    void Update()
    {
        // For rising, maybe we point both feet at the point of leaving the ground
        // This would make sure jumping straight up would look visually interesting:
        // <idle> on ground -> <pointing at point on ground> rising jump -> <idle> falling -> <idle> land.

        // Should we implement a mini crouch for landing recoil? That would add even more visual interest to the aforementioned situation.
        
        // I should do the legs joint rotation code here to prevent artifacts
        
        

        Vector3 xZVelocity = new Vector3(velocity.x, 0, velocity.z);


        // should we just take the same walk animation and scale the speed by how fast the player is moving?
        // This would allow us to have the speed boost character and not have to make special animations for it.
        // but the animations would all have the same amplitude of leg swing, so we should still make a different
        // animation for running/walking (/crouch-walking (?))
        
        

        
        if (grounded)
        {
            int animationDirection = 1;
            
            // Ground specific because we want to turn away from the velocity when going back, back-left, back-right
            // xZVelocity because if we include Y, it could cause problems with angle and magnitude
            if (xZVelocity.magnitude > 0.01f)
            {
                float angle = Vector3.SignedAngle(transform.forward, xZVelocity, Vector3.up);
                
                if (angle <= 91 && angle > -89)
                {
                    legsJoint.rotation = Quaternion.LookRotation(xZVelocity);
                    animationDirection = 1;
                }
                else
                {
                    legsJoint.rotation = Quaternion.LookRotation(-xZVelocity);
                    animationDirection = -1;
                }
                
            }
            else
            {
                // make it so that if you walk somewhere, and stop moving, your legs rotation stays as you look around
                legsJoint.rotation = legsJointPreviousRotation;
            }
            
            // print(xZVelocity);
            
            if (xZVelocity.magnitude > runSpeed)
            {
                // Play run animation
                legsAnimation.Play("Run");
                legsAnimation["Run"].speed = xZVelocity.magnitude * 10 * animationDirection;
            }
            else if (xZVelocity.magnitude > 0.001f)
            {
                legsAnimation.Play("Walk");
                legsAnimation["Walk"].speed = xZVelocity.magnitude * 10 * animationDirection;
            }
            else
            {
                // legsAnimation.Stop();
                legsAnimation.CrossFade("Idle", 0.15f);
            }
            
        }
        // air movement
        else
        {
            legsAnimation.Stop();
            if (xZVelocity.magnitude > 0.01f)
            {
                // point legs towards xzvelocity always
                legsJoint.rotation = Quaternion.LookRotation(xZVelocity);
            }
            else
            {
                // make it so that if you walk somewhere, and stop moving, your legs rotation stays as you look around
                legsJoint.rotation = legsJointPreviousRotation;
            }
            
            // if rising
            if (velocity.y >= 0)
            {
                // point legs towards point on ground with which we jumped from:
                // this script might have to keep track of jump point ourselves by polling ground and noting transform.position when it changes...
                leftLegJoint.DOKill();
                rightLegJoint.DOKill();
                leftLegJoint.LookAt(jumpPoint);
                rightLegJoint.LookAt(jumpPoint);
            }
            // else falling
            else
            {
                // point legs towards wherever you are jumping
                // I could use legs Animation.Play but I wonder if I can scale the tilt of the legs by how fast you are moving.
                
                // can't point them any farther than 90 
                float angle = Mathf.Clamp(xZVelocity.magnitude * fallLegAngleMod, -90, 90);
                
                // Quaternion temp = legOriginalRotation * Quaternion.AngleAxis(angle, leftLegJoint.transform.forward);
                Quaternion temp = Quaternion.Euler(new Vector3(90 - angle, 0, -90));
                // leftLegJoint.localRotation = temp;
                // rightLegJoint.localRotation = temp;
                leftLegJoint.DOLocalRotateQuaternion(temp, 0.15f);
                rightLegJoint.DOLocalRotateQuaternion(temp, 0.15f);
            }
        }

        previousGrounded = grounded;
        
        legsJointPreviousRotation = legsJoint.rotation;
        
    }
}