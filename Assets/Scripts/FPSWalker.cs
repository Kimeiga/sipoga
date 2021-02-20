using System;
using DG.Tweening;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(CharacterController))]
public class FPSWalker : MonoBehaviour
{
	// TODO: I need to clean up this script, specifically by getting rid of values that I won't use...


	/// <summary>
	/// From FPSWalkerEnhanced
	/// https://wiki.unity3d.com/index.php/FPSWalkerEnhanced
	/// </summary>
	[Tooltip("How fast the player moves when walking (default move speed).")] 
	[SerializeField]
	private float moveSpeed = 5.0f;

	[Tooltip("How fast the player moves when walking.")] 
	[SerializeField]
	private float walkSpeed = 3.5f;

	[Tooltip("How fast the player moves when crouching.")] 
	[SerializeField]
	private float crouchSpeed = 2.5f;

	[Tooltip(
		"If checked, the run key toggles between running and walking. Otherwise player runs if the key is held down.")]
	[SerializeField]
	private bool toggleRun = false;

	[Tooltip("How high the player jumps when hitting the jump button.")] 
	[SerializeField]
	private float jumpSpeed = 8.0f;

	[Tooltip("How fast the player falls when not standing on anything.")] 
	[SerializeField]
	private float gravity = 20.0f;

	[Tooltip(
		"If the player ends up on a slope which is at least the Slope Limit as set on the character controller, then he will slide down.")]
	[SerializeField]
	private bool slideWhenOverSlopeLimit = false;

	[Tooltip(
		"If checked and the player is on an object tagged \"Slide\", he will slide down it regardless of the slope limit.")]
	[SerializeField]
	private bool slideOnTaggedObjects = false;

	[Tooltip("How fast the player slides when on slopes as defined above.")] 
	[SerializeField]
	private float slideSpeed = 12.0f;

	[Tooltip("If checked, then the player can change direction while in the air.")]
	[SerializeField]
	private bool airControl = true;

	[Tooltip(
		"Small amounts of this results in bumping when walking down slopes, but large amounts results in falling too fast.")]
	private float antiBumpFactor = .75f;

	[Tooltip(
		"Player must be grounded for at least this many physics frames before being able to jump again; set to 0 to allow bunny hopping.")]
	private int antiBunnyHopFactor = 0;

	private Vector3 moveDirection = Vector3.zero;
	internal bool grounded = false;
	private CharacterController controller;
	private Transform m_Transform;
	private float currentSpeed;
	private RaycastHit slideRaycastHit;
	private bool falling;
	private float slideLimit;
	private float rayDistance;
	private Vector3 contactPoint;
	private bool playerControl = false;
	private int jumpTimer;

	[Header("Crouch Variables")]
	public float standHeight;

	public float landRecoilHeightChange = 0.2f;
	public float landRecoilDuration = 0.1f;
	public float crouchHeight = 1;
	public float crouchDuration = 0.2f;
	public float crouchBodyHeightFactor = 0.9f;
	public Transform[] headObjects;
	public Transform[] footObjects;
	public Transform bodyTransform;
	private Vector3 bodyTransformOriginalScale;

	private PlayerAnimation playerAnimation;

	private bool canWallJump = false;

	[Header("Thrust")]
	private bool thrusting = false;

	private float thrustInputX;
	private float thrustInputY;
	public float thrustSpeed = 8;
	private float currentThrustSpeed;
	private Vector3 thrustDirection = Vector3.zero;
	public float thrustDuration = 0.4f;
	private Tweener thrustTween;

	private Tweener thrustCameraTween;
	private float thrustCameraTilt;
	public float thrustMaxCameraTilt = 4;
	private Player playerScript;
	private MouseLook headMouseLook;

	// Crouching code
	private float targetHeight;
	public float heightChangeSpeed = 2;
	private float heightVelocity;
	
	private float height;
	public float Height
	{
		get { return height; }
		set
		{
			height = value;
			float changeInHeightHalved = (controller.height - height) * 0.5f;

			Vector3 temp = m_Transform.position;
			Vector3 bodyTemp = bodyTransform.localPosition;
			if (!grounded)
			{
				temp.y += changeInHeightHalved * 2;
			}

			m_Transform.position = temp;


			//Change position of each "head object" with change in controller height such that it stays by the "head"
			foreach (Transform t in headObjects)
			{
				temp = t.position;
				temp.y -= changeInHeightHalved * 2;
				t.position = temp;
			}

			// //Change position of each "foot object" with change in controller height such that it stays by the "foot"
			// foreach (Transform t in footObjects)
			// {
			//     temp = t.position;
			//     temp.y += changeInHeightHalved;
			//     t.position = temp;
			// }

			// Also we have to scale the body and move the head down...
			float mod = Mathf.Lerp(1, 0.9f, (standHeight - controller.height) / (standHeight - crouchHeight));
			temp = new Vector3(bodyTransformOriginalScale.x,
				bodyTransformOriginalScale.y * controller.height / standHeight * mod, bodyTransformOriginalScale.z);
			if (!float.IsNaN(bodyTransformOriginalScale.y * controller.height / standHeight))
			{
				bodyTransform.localScale = temp;
			}

			// we also have to decrease the offset by half the distance crouched
			controller.center = new Vector3(0, controller.center.y - (controller.height - height) * 0.5f, 0);

			controller.height = height;
		}
	}


	private void Start()
	{
		// Saving component references to improve performance.
		m_Transform = GetComponent<Transform>();
		controller = GetComponent<CharacterController>();

		// Setting initial values.
		currentSpeed = moveSpeed;
		rayDistance = controller.height * .5f + controller.radius;
		slideLimit = controller.slopeLimit - .1f;
		jumpTimer = antiBunnyHopFactor;

		DOTween.Init();

		standHeight = controller.height;

		bodyTransformOriginalScale = bodyTransform.localScale;

		playerAnimation = GetComponent<PlayerAnimation>();

		playerScript = GetComponent<Player>();
		headMouseLook = playerScript.body.headMouseLook;

		Height = controller.height;
		targetHeight = controller.height;
	}


	private void Update()
	{
		// If the run button is set to toggle, then switch between walk/run speed. (We use Update for this...
		// FixedUpdate is a poor place to use GetButtonDown, since it doesn't necessarily run every frame and can miss the event)
		if (toggleRun && grounded && Input.GetButtonDown("Run"))
		{
			if (Input.GetButton("Crouch") && grounded)
			{
				currentSpeed = crouchSpeed;
			}
			else
			{
				currentSpeed = (currentSpeed == moveSpeed ? walkSpeed : moveSpeed);
			}
		}

		// Crouch code is here because it is on input down and up
		if (Input.GetButtonDown("Crouch"))
		{
			targetHeight = crouchHeight;
		}

		if (Input.GetButtonUp("Crouch"))
		{
			targetHeight = standHeight;
		}

		if (Input.GetButtonDown("Thrust"))
		{
			thrustTween?.Kill();
			thrustCameraTween?.Kill();

			Thrust();
		}
	}


	private void FixedUpdate()
	{
		// control crouching
		Height = Mathf.SmoothDamp(Height, targetHeight, ref heightVelocity, heightChangeSpeed);

		// 2d input vector
		Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

		// moving diagonally isn't faster
		// not a good movement tech to make it faster since it's not intuitive and would complicate the movement for AI
		// since you would have to calculate speed based on direction of body relative to head, and their pathfinding
		// may not accomodate it (i.e. they may swing past the target by accident if they move diagonally at one point
		// while moving.)
		input = Vector2.ClampMagnitude(input, 1);


		if (grounded)
		{
			bool sliding = false;
			// See if surface immediately below should be slid down. We use this normally rather than a ControllerColliderHit point,
			// because that interferes with step climbing amongst other annoyances
			if (Physics.Raycast(m_Transform.position, -Vector3.up, out slideRaycastHit, rayDistance))
			{
				if (Vector3.Angle(slideRaycastHit.normal, Vector3.up) > slideLimit)
				{
					sliding = true;
				}
			}
			// However, just raycasting straight down from the center can fail when on steep slopes
			// So if the above raycast didn't catch anything, raycast down from the stored ControllerColliderHit point instead
			else
			{
				Physics.Raycast(contactPoint + Vector3.up, -Vector3.up, out slideRaycastHit);
				if (Vector3.Angle(slideRaycastHit.normal, Vector3.up) > slideLimit)
				{
					sliding = true;
				}
			}

			// If we were falling, and we fell a vertical distance greater than the threshold, run a falling damage routine

			if (falling)
			{
				falling = false;

				// We just landed right? Do a short crouch-uncrouch to give a bounce to landing
				if (Input.GetButton("Crouch"))
				{
					height = crouchHeight - landRecoilHeightChange;
				}
				else
				{
					height = standHeight - landRecoilHeightChange;
				}
			}

			// If running isn't on a toggle, then use the appropriate speed depending on whether the run button is down
			if (!toggleRun)
			{
				if (Input.GetButton("Crouch") && grounded)
				{
					currentSpeed = crouchSpeed;
				}
				else
				{
					currentSpeed = Input.GetKey(KeyCode.LeftShift) ? walkSpeed : moveSpeed;
				}
			}

			// If sliding (and it's allowed), or if we're on an object tagged "Slide", get a vector pointing down the slope we're on
			if ((sliding && slideWhenOverSlopeLimit) ||
			    (slideOnTaggedObjects && slideRaycastHit.collider.CompareTag("Slide")))
			{
				Vector3 hitNormal = slideRaycastHit.normal;
				moveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
				Vector3.OrthoNormalize(ref hitNormal, ref moveDirection);
				moveDirection *= slideSpeed;
				playerControl = false;
			}
			// Otherwise recalculate moveDirection directly from axes, adding a bit of -y to avoid bumping down inclines
			else
			{
				moveDirection =
					new Vector3(input.x, -antiBumpFactor, input.y);
				moveDirection = m_Transform.TransformDirection(moveDirection) * currentSpeed;
				playerControl = true;
			}

			// Jump! But only if the jump button has been released and player has been grounded for a given number of frames
			if (!Input.GetButton("Jump"))
			{
				jumpTimer++;
			}
			else if (jumpTimer >= antiBunnyHopFactor)
			{
				moveDirection.y = jumpSpeed;
				jumpTimer = 0;
			}
		}
		else
		{
			// TODO: Decide if we can get walljump working. Leaning no right now...
			// if (canWallJump)
			// {
			//     // Jump! But only if the jump button has been released and player has been grounded for a given number of frames
			//     if (!Input.GetButton("Jump"))
			//     {
			//         m_JumpTimer++;
			//     }
			//     else if (m_JumpTimer >= m_AntiBunnyHopFactor)
			//     {
			//         if(playerAnimation != null)
			//             playerAnimation.jumpPoint = transform.position;
			//     
			//         m_MoveDirection.y = m_JumpSpeed;
			//         m_JumpTimer = 0;
			//     }
			//
			//     canWallJump = false;
			// }

			// If we stepped over a cliff or something, set the height at which we started falling
			if (!falling)
			{
				falling = true;
			}

			// If air control is allowed, check movement but don't touch the y component
			if (airControl && playerControl)
			{
				moveDirection.x = input.x * currentSpeed;
				moveDirection.z = input.y * currentSpeed;

				moveDirection = m_Transform.TransformDirection(moveDirection);
			}
		}

		if (thrusting)
		{
			thrustDirection.x = thrustInputX * currentThrustSpeed;
			thrustDirection.y = 0;
			thrustDirection.z = thrustInputY * currentThrustSpeed;
		}
		else
		{
			thrustDirection = Vector3.zero;
		}

		thrustDirection = m_Transform.TransformDirection(thrustDirection);

		// Apply gravity
		moveDirection.y -= gravity * Time.deltaTime;


		// Move the controller, and set grounded true or false depending on whether we're standing on something
		grounded = (controller.Move((moveDirection + thrustDirection) * Time.deltaTime) & CollisionFlags.Below) !=
		           0;

	}

	// Store point that we're in contact with for use in FixedUpdate if needed
	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		contactPoint = hit.point;
	}

	// private void OnTriggerEnter(Collider other)
	// {
	//     if (other.gameObject.layer == LayerMask.NameToLayer("T") ||
	//         other.gameObject.layer == LayerMask.NameToLayer("U"))
	//     {
	//         return;
	//     }
	//     
	//     print("Enter: " + other);
	//     canWallJump = true;
	// }
	//
	// private void OnTriggerExit(Collider other)
	// {
	//     if (other.gameObject.layer == LayerMask.NameToLayer("T") ||
	//         other.gameObject.layer == LayerMask.NameToLayer("U"))
	//     {
	//         return;
	//     }
	//     
	//     print("Exit: " + other);
	//     canWallJump = false;
	// }

	// private void OnTriggerStay(Collider other)
	// {
	//     if (other.gameObject.layer == LayerMask.NameToLayer("T") ||
	//         other.gameObject.layer == LayerMask.NameToLayer("U"))
	//     {
	//         return;
	//     }
	//
	//     print("Stay: " + other);
	//     canWallJump = true;
	// }

	private void Thrust()
	{
		// initial conditions of thrust
		currentThrustSpeed = thrustSpeed;
		thrusting = true;

		thrustInputX = Input.GetAxisRaw("Horizontal");
		thrustInputY = Input.GetAxisRaw("Vertical");


		thrustTween = DOTween.To(() => currentThrustSpeed, x => { currentThrustSpeed = x; }
				, 0, thrustDuration).SetEase(Ease.InCubic)
			.OnComplete(() =>
			{
				thrusting = false;
				thrustDirection = Vector3.zero;
			});

		// you should also yaw the camera if you are thrusting left or right
		// since no action needs to be taken for thrusting forward or back, you can just use the thrustInputX to set a float
		// and then bring it back to 0
		thrustCameraTilt = thrustMaxCameraTilt * thrustInputX;

		thrustCameraTween = DOTween.To(() => thrustCameraTilt, x =>
			{
				thrustCameraTilt = x;
				headMouseLook.tiltAmount = -thrustCameraTilt;
			}
			, 0, thrustDuration).SetEase(Ease.InCubic);
	}
}