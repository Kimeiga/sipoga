using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FirstPersonMovement : MonoBehaviour {
    

	[Header("Base Variables")]
	public CharacterController CharacterController;
	public bool CanMove = true;

	[Space(10)]

	[Header("Speed Variables")]
	public float RunSpeed = 5;
	public float WalkSpeed = 4;
	public float CrouchSpeed = 3;
	public float Speed;
	private bool _jumpedFromStand = false;

	[Space(10)]

	[Header("Sliding Variables")]
	// If the player ends up on a slope which is at least the Slope Limit as set on the character controller, then he will slide down
    public bool SlideWhenOverSlopeLimit = true;
	// If checked and the player is on an object tagged "Slide", he will slide down it regardless of the slope limit
	public bool SlideOnTaggedObjects = false;
	public float SlideSpeed = 6f;
	private float _slideLimit;
	public bool Sliding = false;
	public int SlideCounter = 0;

	private float _slideDirectionMod = 1;


    [Space(10)]


    [Header("Jumping Variables")]

	public float JumpSpeed = 6;
	private bool _falling = false;
	
	
	private bool _hitCeiling = false;
	public float Gravity = 20;
	private float _antiBumpFactor = .75f;
	private bool _fellOffLedge = false;

	public bool Grounded = false;

	private Vector3 _wishDir;

	private Vector3 _moveDir;


	[Space(10)]

	[Header("Crouching Variables")]
	//These are the crouching values that can be changed around (just make sure that crouchHeight - springOffset is not below 1)
	public float CrouchHeight = 1.4f;
	public float SpringOffset = 0.2f;
	public float CrouchingSpeed = 0.2f;
	public float SpringMod = 2;
	private float _standHeight; //You can edit this one by just changing the height of the controller in the inspector
	
	//working variables for crouching mechanism
	private bool _recoilingFromLand = false;
	private float _crouchingSpeedMod = 1;
	private float _targetHeight;

    public float HeightActual;
    
    private float _previousHeight;

	[Space(10)]


	[Header("Changing Body During Crouch Variables")]
	public Transform MeshTransform;
	private Vector3 _meshTransformOriginalScale;

	public Transform[] HeadObjects;
	public Transform[] FootObjects;



	private RaycastHit _hit;
	private Vector3 _contactPoint;
	private float _rayDistance;
    public LayerMask NotBodyPartAlive;
	
	
	private bool _jumpButtonUp;

	private bool _wallHang = false;
	private float _stepOffsetInitial;
	private bool _onSlope;



    private bool _dontBounce = false;
	private bool _dontBounceAssist = false;


	[Space(10)]

	[Header("Measurements")]
	public Vector3 MeasuredDisplacement;
	public float MeasuredSpeed;
	private Vector3 _lastPosition;




	// Use this for initialization
	void Start () {


        //initialize last position
        _lastPosition = Vector3.zero;

        CharacterController = GetComponent<CharacterController>();

		_stepOffsetInitial = CharacterController.stepOffset;

		_slideLimit = CharacterController.slopeLimit - .1f;
		
		_standHeight = CharacterController.height;
		
		

		_wishDir = Vector3.zero;
		

		_meshTransformOriginalScale = MeshTransform.localScale;
        

	}

    void Update()
    {
        
        if(Grounded){
            _jumpedFromStand = false;
        }


        if ((CanMove && Input.GetButton("Crouch") && !_jumpedFromStand) 
            || (CharacterController.collisionFlags & CollisionFlags.Above) != 0 && CharacterController.height < _standHeight)
		{
			Speed = CrouchSpeed;
		}
		else if (CanMove && Input.GetButton("Walk"))
		{
			Speed = WalkSpeed;
		}
		else
		{
			Speed = RunSpeed;
		}


		_rayDistance = CharacterController.height * 0.5f + CharacterController.radius;

		if (CanMove && Input.GetButtonUp("Jump") && !Sliding)
		{
			_jumpButtonUp = true;
		}
        
	}

	void FixedUpdate() {
        


        //sliding mechanic
		if (Grounded)
		{
			Sliding = false;
			// See if surface immediately below should be slid down. We use this normally rather than a ControllerColliderHit point,
			// because that interferes with step climbing amongst other annoyances
			if (Physics.Raycast(transform.position, -Vector3.up, out _hit, _rayDistance))
			{
				if (Vector3.Angle(_hit.normal, Vector3.up) > _slideLimit)
				{
					Sliding = true;
					Grounded = true;
				}

				if (Vector3.Angle(_hit.normal, Vector3.up) > 0.1f)
				{
					_onSlope = true;
				}
				else
				{
					_onSlope = false;
				}

			}
			// However, just raycasting straight down from the center can fail when on steep slopes
			// So if the above raycast didn't catch anything, raycast down from the stored ControllerColliderHit point instead
			
			Physics.Raycast(_contactPoint + Vector3.up, -Vector3.up, out _hit);
			if (Vector3.Angle(_hit.normal, Vector3.up) > _slideLimit)
			{
				Sliding = true;
				Grounded = true;
                
			}
                
		}

		if (Sliding)
		{
			SlideCounter++;

		}
		else
		{
			SlideCounter = 0;
		}


        if (CanMove)
        {

            if (Grounded) {
                if (_falling && !(SlideCounter > 2) && !_dontBounceAssist && !Sliding)
                {
                    _recoilingFromLand = true;
                    _falling = false;

                    if (_dontBounce)
                    {
                        _dontBounceAssist = true;
                    }
                }
            }


            //record the previous height to scale the mesh, move the head, and change the y value of the transform
            _previousHeight = CharacterController.height;

            //set the target height for the crouch command
            if (Input.GetButton("Crouch"))
            {
                //If you are near the crouching spring height, than stop recoiling from a landing
                if (CharacterController.height - (CrouchHeight - SpringOffset) < 0.05f)
                {
                    _recoilingFromLand = false;
                }

                //if you are crouching, then go to the crouch height
                _targetHeight = CrouchHeight;

                //if you are jumping or recoiling from a landing, go to the crouch spring height
                if ((Input.GetButton("Jump") || _recoilingFromLand) && !Sliding)
                {
                    _targetHeight = CrouchHeight - SpringOffset;
                }
            }
            else
            {
                //If you are near the spring height, than stop recoiling from a landing
                if (CharacterController.height - (_standHeight - SpringOffset) < 0.05f)
                {
                    _recoilingFromLand = false;
                }

                //if you are just standing, then go to the stand height
                _targetHeight = _standHeight;

                //if you are jumping or recoiling from a landing, go to the spring height
                if ((Input.GetButton("Jump") || _recoilingFromLand) && !Sliding)
                {
                    _targetHeight = _standHeight - SpringOffset;
                }
            }


            //If you are recoiling from a landing, than crouch faster
            if (_recoilingFromLand)
            {
                _crouchingSpeedMod = SpringMod;
            }
            else
            {
                _crouchingSpeedMod = 1;
            }


            //prevent increasing in height if there is something over my head
            if ((CharacterController.collisionFlags & CollisionFlags.Above) != 0 && _targetHeight == _standHeight)
            {
                _targetHeight = CharacterController.height;
            }
            //prevent increasing in height if there is something over my head
            if (Physics.Raycast(transform.position, Vector3.up, out _hit, CharacterController.height * 0.5f + 0.1f, NotBodyPartAlive) && _targetHeight == _standHeight)
            {
                _targetHeight = CharacterController.height;
            }


            //Lerp the controller height to the target height

            if (Mathf.Abs(CharacterController.height - _targetHeight) > 0.0001f)
            {
                HeightActual = Mathf.Lerp(CharacterController.height, _targetHeight, CrouchingSpeed * _crouchingSpeedMod);

                CharacterController.height = HeightActual;
            }
            

            //Change Y Value of Transform with change in controller height
            float changeInHeightHalfed = (CharacterController.height - _previousHeight) * 0.5f; //these two variables will be used for the next three sections
            Vector3 temp = transform.position;
            if (Grounded)
            {
                temp.y += changeInHeightHalfed;
            }
            else
            {
                temp.y -= changeInHeightHalfed;
            }
            if (!float.IsNaN(temp.y))
            {
                transform.position = temp;
            }



            //Change position of each "head object" with change in controller height such that it stays by the "head"
            foreach (Transform t in HeadObjects)
            {
                temp = t.position;
                temp.y += changeInHeightHalfed;
                t.position = temp;
            }

            //Change position of each "foot object" with change in controller height such that it stays by the "foot"
            foreach (Transform t in FootObjects)
            {
                temp = t.position;
                temp.y -= changeInHeightHalfed;
                t.position = temp;
            }


            //Change scale of character mesh with change in controller height
            temp = new Vector3(_meshTransformOriginalScale.x, _meshTransformOriginalScale.y * CharacterController.height / _standHeight, _meshTransformOriginalScale.z);
            if (!float.IsNaN(_meshTransformOriginalScale.y * CharacterController.height / _standHeight))
            {
                MeshTransform.localScale = temp;
            }

            




            if (Grounded)
            {
                _fellOffLedge = false;
            }

            _slideDirectionMod = Mathf.Lerp(_slideDirectionMod, 1, 0.1f);


            //get wish direction of movement from inputs
            _wishDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            if (_wishDir.magnitude > 1)
            {
                _wishDir = _wishDir.normalized;
            }

            _moveDir.x = _wishDir.x * Speed;
            _moveDir.z = _wishDir.z * Speed;

            if (!_wallHang)
            {
                if (!Grounded)
                {

                    _moveDir.y -= Gravity * Time.fixedDeltaTime;
                }
                else
                {

                    _moveDir.y -= Gravity * Time.fixedDeltaTime * 2;
                }
                
                CharacterController.stepOffset = _stepOffsetInitial;
            }
            else
            {
                _moveDir.y = 0;
                CharacterController.stepOffset = 0.01f;
            }

            if ((CharacterController.collisionFlags & CollisionFlags.Below) != 0)
            {
                Grounded = true;
                
            }
            else
            {
                Grounded = false;
            }
                

            if (!_jumpButtonUp)
            {
	            RaycastHit hit2;
                //if we are still not grounded, raycast down to double check if we aren't
                if (Physics.Raycast(transform.position, -transform.up, out hit2,  (CharacterController.height * 0.5f) + CharacterController.skinWidth + 0.05f))
                {
                    Grounded = true;
	                print(hit2.collider.gameObject.name);
                }

                Debug.DrawRay(transform.position,-transform.up * ((CharacterController.height * 0.5f) + CharacterController.skinWidth + 0.05f), Color.blue, 0.1f);
            }


            if (Grounded)
			{
				if (_jumpButtonUp)
				{
					_moveDir.y = JumpSpeed;
					Grounded = false;
					_dontBounceAssist = false;
                    _jumpedFromStand = !Input.GetButton("Crouch");
				}
			}


            if (((CharacterController.collisionFlags & CollisionFlags.Above) != 0) && _hitCeiling == false)
			{
				_moveDir.y = -0.4f;
				_hitCeiling = true;
			}


			_moveDir = transform.TransformDirection(_moveDir);


			// If sliding (and it's allowed), or if we're on an object tagged "Slide", get a vector pointing down the slope we're on
			if (Grounded && (((SlideCounter > 2) && SlideWhenOverSlopeLimit) || (SlideOnTaggedObjects && _hit.collider.tag == "Slide")))
			{
				Vector3 slideMoveDirection = _moveDir;
				Vector3 hitNormal = _hit.normal;
				slideMoveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
				Vector3.OrthoNormalize(ref hitNormal, ref slideMoveDirection);
				slideMoveDirection *= SlideSpeed;


				_moveDir *= 0.2f;
				_moveDir += slideMoveDirection * 0.8f * _slideDirectionMod;

				if (_jumpButtonUp)
				{
					_moveDir.y = JumpSpeed * 0.7f;
				}

			}


			CharacterController.Move(_moveDir * Time.fixedDeltaTime);

            


            if (Grounded)
			{
				_hitCeiling = false;
				_moveDir.y = -_antiBumpFactor;
				if (_falling && !Sliding)
				{
					_slideDirectionMod = 0.5f;
				}
			}
			else
			{
				_falling = true;
				if (_fellOffLedge && !_falling && !(SlideCounter > 2))
				{
					_moveDir.y = -1;
					_fellOffLedge = false;
				}
			}

		}
		
		_wallHang = false;
		_jumpButtonUp = false;


		MeasuredDisplacement = transform.position - _lastPosition;
		MeasuredSpeed = MeasuredDisplacement.magnitude;

        //set lastposition for displacement/speed calculation
        _lastPosition = transform.position;
		
	}


	
	void OnControllerColliderHit(ControllerColliderHit hit)
	{

        _contactPoint = hit.point;
		if (_onSlope && _wishDir.magnitude > 0.05f)
		{
			_dontBounce = true;
		}
		else
		{
			_dontBounce = false;

		}


        
        
    }

	void OnTriggerStay(Collider other)
	{

        if (other.gameObject.tag == "Level")
		{

            if (!Grounded && CanMove && Input.GetButton("Wall Hang")){
                _wallHang = true;

			}


        }
	}

    
    
    
    void Crouch(float height,float heightHalf)
    {

    }

}
