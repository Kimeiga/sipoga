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

	private Player player;
	private FPSWalker fpsWalkerScript;
	private NavMeshAgent navMeshAgent;

	private bool isAI = false;
	
	public Animation legsAnimation;
	private Transform legsJoint;
	private Quaternion legsJointPreviousRotation;

	// for jump rising
	private Transform rightLegJoint;
	private Transform leftLegJoint;

	public float runSpeed = 5;

	private bool grounded = true;
	private bool previousGrounded = true;

	public float fallLegAngleMod = 400;

	// controls the right/left leg switch while jumping
	private bool rightLegFirst = true;

	// Start is called before the first frame update
	void Start()
	{
		player = GetComponent<Player>();
		legsJoint = player.body.legsJoint;
		leftLegJoint = player.body.leftLegJoint;
		rightLegJoint = player.body.rightLegJoint;
		isAI = player.isAI;
		
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
		// Ground movement
		// we should get grounded from FPSWalker or NavMeshAgent
		// not sure if isOnNavMesh is best, but I think it's correct
		grounded = isAI ? navMeshAgent.isOnNavMesh : fpsWalkerScript.grounded;

		if (grounded && !previousGrounded)
		{
			rightLegFirst = !rightLegFirst;
		}
	}

	// Update is called once per frame
	void Update()
	{
		// For rising, maybe we point both feet at the point of leaving the ground
		// This would make sure jumping straight up would look visually interesting:
		// <idle> on ground -> <pointing at point on ground> rising jump -> <idle> falling -> <idle> land.

		// Should we implement a mini crouch for landing recoil? That would add even more visual interest to the aforementioned situation.

		// I should do the legs joint rotation code here to prevent artifacts


		Vector3 xZVelocity = new Vector3(player.velocity.x, 0, player.velocity.z);


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

			// can't point them any farther than 180 
			float angle =
				Mathf.Clamp(xZVelocity.magnitude * fallLegAngleMod + (xZVelocity.magnitude / runSpeed * player.velocity.y * 100), -180,
					180);

			Quaternion temp = Quaternion.Euler(new Vector3(90 - angle, 0, -90));
			Quaternion temp2 = Quaternion.Euler(new Vector3(90 + angle, 0, -90));

			if (rightLegFirst)
			{
				leftLegJoint.DOLocalRotateQuaternion(temp2, 0.15f);
				rightLegJoint.DOLocalRotateQuaternion(temp, 0.15f);
			}
			else
			{
				leftLegJoint.DOLocalRotateQuaternion(temp, 0.15f);
				rightLegJoint.DOLocalRotateQuaternion(temp2, 0.15f);
			}
		}

		previousGrounded = grounded;

		legsJointPreviousRotation = legsJoint.rotation;
	}
}