using System;
using DG.Tweening;
using UnityEngine;
 
[RequireComponent(typeof(CharacterController))]

public class FPSWalker : MonoBehaviour
{
    /// <summary>
    /// From FPSWalkerEnhanced
    /// https://wiki.unity3d.com/index.php/FPSWalkerEnhanced
    /// </summary>
    
    [Tooltip("How fast the player moves when walking (default move speed).")]
    [SerializeField]
    private float m_WalkSpeed = 5.0f;
 
    [Tooltip("How fast the player moves when running.")]
    [SerializeField]
    private float m_RunSpeed = 4.0f;
 
    [Tooltip("If true, diagonal speed (when strafing + moving forward or back) can't exceed normal move speed; otherwise it's about 1.4 times faster.")]
    [SerializeField]
    public bool m_LimitDiagonalSpeed = true;
 
    [Tooltip("If checked, the run key toggles between running and walking. Otherwise player runs if the key is held down.")]
    [SerializeField]
    private bool m_ToggleRun = false;
 
    [Tooltip("How high the player jumps when hitting the jump button.")]
    [SerializeField]
    private float m_JumpSpeed = 8.0f;
 
    [Tooltip("How fast the player falls when not standing on anything.")]
    [SerializeField]
    private float m_Gravity = 20.0f;
 
    [Tooltip("Units that player can fall before a falling function is run. To disable, type \"infinity\" in the inspector.")]
    [SerializeField]
    private float m_FallingThreshold = 10.0f;
 
    [Tooltip("If the player ends up on a slope which is at least the Slope Limit as set on the character controller, then he will slide down.")]
    [SerializeField]
    private bool m_SlideWhenOverSlopeLimit = false;
 
    [Tooltip("If checked and the player is on an object tagged \"Slide\", he will slide down it regardless of the slope limit.")]
    [SerializeField]
    private bool m_SlideOnTaggedObjects = false;
 
    [Tooltip("How fast the player slides when on slopes as defined above.")]
    [SerializeField]
    private float m_SlideSpeed = 12.0f;
 
    [Tooltip("If checked, then the player can change direction while in the air.")]
    [SerializeField]
    private bool m_AirControl = true;
 
    [Tooltip("Small amounts of this results in bumping when walking down slopes, but large amounts results in falling too fast.")]
    [SerializeField]
    private float m_AntiBumpFactor = .75f;
 
    [Tooltip("Player must be grounded for at least this many physics frames before being able to jump again; set to 0 to allow bunny hopping.")]
    [SerializeField]
    private int m_AntiBunnyHopFactor = 0;
 
    private Vector3 m_MoveDirection = Vector3.zero;
    public bool grounded = false;
    private CharacterController m_Controller;
    private Transform m_Transform;
    private float m_Speed;
    private RaycastHit m_Hit;
    private float m_FallStartLevel;
    private bool m_Falling;
    private float m_SlideLimit;
    private float m_RayDistance;
    private Vector3 m_ContactPoint;
    private bool m_PlayerControl = false;
    private int m_JumpTimer;

    [Header("Crouch Variables")]
    public float standHeight;

    public float landRecoilHeightChange = 0.2f;
    public float landRecoilDuration = 0.1f;
    public float crouchHeight = 1;
    public float crouchDuration = 0.2f;
    private Tweener crouchTween;
    private Tweener uncrouchTween;
    public Transform[] headObjects;
    public Transform[] footObjects;
    public Transform bodyTransform;
    private Vector3 bodyTransformOriginalScale;

    private PlayerAnimation playerAnimation;

    [Header("Measurements")]
    public Vector3 velocity;
    public Vector3 xZVelocity;
    private Vector3 lastPosition;
    
    private void Start()
    {
        // Saving component references to improve performance.
        m_Transform = GetComponent<Transform>();
        m_Controller = GetComponent<CharacterController>();
 
        // Setting initial values.
        m_Speed = m_WalkSpeed;
        m_RayDistance = m_Controller.height * .5f + m_Controller.radius;
        m_SlideLimit = m_Controller.slopeLimit - .1f;
        m_JumpTimer = m_AntiBunnyHopFactor;
        
        DOTween.Init();

        standHeight = m_Controller.height;

        bodyTransformOriginalScale = bodyTransform.localScale;

        playerAnimation = GetComponent<PlayerAnimation>();

        lastPosition = transform.position;
    }
 
 
    private void Update()
    {
        // If the run button is set to toggle, then switch between walk/run speed. (We use Update for this...
        // FixedUpdate is a poor place to use GetButtonDown, since it doesn't necessarily run every frame and can miss the event)
        if (m_ToggleRun && grounded && Input.GetButtonDown("Run"))
        {
            m_Speed = (m_Speed == m_WalkSpeed ? m_RunSpeed : m_WalkSpeed);
        }
        
        // Crouch code is here because it is on input down and up
        if (Input.GetButtonDown("Crouch"))
        {
            // Crouch
            uncrouchTween?.Kill();
            
            Crouch(crouchHeight, crouchDuration);
        }

        if (Input.GetButtonUp("Crouch"))
        {
            crouchTween?.Kill();
            
            Uncrouch(standHeight, crouchDuration);
        }
    }
 
 
    private void FixedUpdate()
    {
        // TODO: add crouch code, and maybe use a tweening library (or find your own tweening solution in native code), because you will have to 
        // do a lot of tweening code anyways
        
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical");
 
        // If both horizontal and vertical are used simultaneously, limit speed (if allowed), so the total doesn't exceed normal move speed
        float inputModifyFactor = (inputX != 0.0f && inputY != 0.0f && m_LimitDiagonalSpeed) ? .7071f : 1.0f;
 
        if (grounded)
        {
            bool sliding = false;
            // See if surface immediately below should be slid down. We use this normally rather than a ControllerColliderHit point,
            // because that interferes with step climbing amongst other annoyances
            if (Physics.Raycast(m_Transform.position, -Vector3.up, out m_Hit, m_RayDistance))
            {
                if (Vector3.Angle(m_Hit.normal, Vector3.up) > m_SlideLimit)
                {
                    sliding = true;
                }
            }
            // However, just raycasting straight down from the center can fail when on steep slopes
            // So if the above raycast didn't catch anything, raycast down from the stored ControllerColliderHit point instead
            else
            {
                Physics.Raycast(m_ContactPoint + Vector3.up, -Vector3.up, out m_Hit);
                if (Vector3.Angle(m_Hit.normal, Vector3.up) > m_SlideLimit)
                {
                    sliding = true;
                }
            }
 
            // If we were falling, and we fell a vertical distance greater than the threshold, run a falling damage routine
            if (m_Falling)
            {
                m_Falling = false;
                
                // We just landed right? Do a short crouch-uncrouch to give a bounce to landing

                if (Input.GetButton("Crouch"))
                {
                    LandRecoil(crouchHeight - landRecoilHeightChange, landRecoilDuration);
                }
                else
                {
                    LandRecoil(standHeight - landRecoilHeightChange, landRecoilDuration);
                }
                
                if (m_Transform.position.y < m_FallStartLevel - m_FallingThreshold)
                {
                    OnFell(m_FallStartLevel - m_Transform.position.y);
                }
            }
 
            // If running isn't on a toggle, then use the appropriate speed depending on whether the run button is down
            if (!m_ToggleRun)
            {
                //TODO: change to walk speed and the other to run speed and use the input name.
                m_Speed = Input.GetKey(KeyCode.LeftShift) ? m_RunSpeed : m_WalkSpeed;
            }
 
            // If sliding (and it's allowed), or if we're on an object tagged "Slide", get a vector pointing down the slope we're on
            if ((sliding && m_SlideWhenOverSlopeLimit) || (m_SlideOnTaggedObjects && m_Hit.collider.tag == "Slide"))
            {
                Vector3 hitNormal = m_Hit.normal;
                m_MoveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
                Vector3.OrthoNormalize(ref hitNormal, ref m_MoveDirection);
                m_MoveDirection *= m_SlideSpeed;
                m_PlayerControl = false;
            }
            // Otherwise recalculate moveDirection directly from axes, adding a bit of -y to avoid bumping down inclines
            else
            {
                m_MoveDirection = new Vector3(inputX * inputModifyFactor, -m_AntiBumpFactor, inputY * inputModifyFactor);
                m_MoveDirection = m_Transform.TransformDirection(m_MoveDirection) * m_Speed;
                m_PlayerControl = true;
            }
 
            // Jump! But only if the jump button has been released and player has been grounded for a given number of frames
            if (!Input.GetButton("Jump"))
            {
                m_JumpTimer++;
            }
            else if (m_JumpTimer >= m_AntiBunnyHopFactor)
            {
                if(playerAnimation != null)
                    playerAnimation.jumpPoint = transform.position;
                
                m_MoveDirection.y = m_JumpSpeed;
                m_JumpTimer = 0;
            }
        }
        else
        {
            // If we stepped over a cliff or something, set the height at which we started falling
            if (!m_Falling)
            {
                m_Falling = true;
                m_FallStartLevel = m_Transform.position.y;
            }
 
            // If air control is allowed, check movement but don't touch the y component
            if (m_AirControl && m_PlayerControl)
            {
                m_MoveDirection.x = inputX * m_Speed * inputModifyFactor;
                m_MoveDirection.z = inputY * m_Speed * inputModifyFactor;
                m_MoveDirection = m_Transform.TransformDirection(m_MoveDirection);
            }
        }
 
        // Apply gravity
        m_MoveDirection.y -= m_Gravity * Time.deltaTime;
 
        // Move the controller, and set grounded true or false depending on whether we're standing on something
        grounded = (m_Controller.Move(m_MoveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;

        velocity = m_Transform.position - lastPosition;
        xZVelocity = new Vector3(velocity.x, 0, velocity.z);
        
        lastPosition = m_Transform.position;
    }

    // TODO: Potential problem: if you uncrouch in the middle of crouching,
    // you'll uncrouch really slowly because you have the same duration being used
    // potential solution would be to scale the duration down when the distance
    // between the current height and the uncrouch height is low
    // and you would have to do this for both Crouch() and Uncrouch()...

    private void LandRecoil(float targetHeight, float duration)
    {
        float originalHeight = m_Controller.height;

        crouchTween = DOTween.To(() => m_Controller.height, x =>
                {
                    float changeInHeightHalved = (m_Controller.height - x) * 0.5f;

                    Vector3 temp = m_Transform.position;
                    Vector3 bodyTemp = bodyTransform.localPosition;
                    if (grounded)
                    {
                        temp.y -= changeInHeightHalved;

                    }

                    bodyTemp.y += changeInHeightHalved;

                    if (!float.IsNaN(temp.y))
                    {
                        m_Transform.position = temp;
                        bodyTransform.localPosition = bodyTemp;
                    }


                    //Change position of each "head object" with change in controller height such that it stays by the "head"
                    foreach (Transform t in headObjects)
                    {
                        temp = t.position;
                        temp.y -= changeInHeightHalved;
                        t.position = temp;
                    }

                    //Change position of each "foot object" with change in controller height such that it stays by the "foot"
                    foreach (Transform t in footObjects)
                    {
                        temp = t.position;
                        temp.y += changeInHeightHalved;
                        t.position = temp;
                    }


                    // Also we have to scale the body and move the head down...
                    temp = new Vector3(bodyTransformOriginalScale.x,
                        bodyTransformOriginalScale.y * m_Controller.height / standHeight, bodyTransformOriginalScale.z);
                    if (!float.IsNaN(bodyTransformOriginalScale.y * m_Controller.height / standHeight))
                    {
                        bodyTransform.localScale = temp;
                    }

                    // // we also have to decrease the offset by half the distance crouched
                    // m_Controller.center =
                    //     new Vector3(m_Controller.center.x, m_Controller.center.y - (m_Controller.height - x) * 0.5f, m_Controller.center.z);

                    m_Controller.height = x;
                }, targetHeight,
                duration)
            .SetEase(Ease.OutQuart)
            .OnComplete(()=>Uncrouch(originalHeight, duration));
    }
    
    private void Crouch(float targetHeight, float duration)
    {
        
        // we also have to tween the y position because otherwise it will just fall down
        // and not feel like a crouch
        
        // //Change Y Value of Transform with change in controller height
        // float changeInHeightHalfed = (CharacterController.height - _previousHeight) * 0.5f; //these two variables will be used for the next three sections
        // Vector3 temp = transform.position;
        // if (Grounded)
        // {
        //     temp.y += changeInHeightHalfed;
        // }
        // else
        // {
        //     temp.y -= changeInHeightHalfed;
        // }
        // if (!float.IsNaN(temp.y))
        // {
        //     transform.position = temp;
        // }
        
        // transform.DOMoveY()
        
        crouchTween = DOTween.To(() => m_Controller.height, x =>
                {
                    float changeInHeightHalved = (m_Controller.height - x) * 0.5f;
                    
                    Vector3 temp = m_Transform.position;
                    Vector3 bodyTemp = bodyTransform.localPosition;
                    if (grounded)
                    {
                        temp.y -= changeInHeightHalved;

                    }

                    bodyTemp.y += changeInHeightHalved;
                    
                    if (!float.IsNaN(temp.y))
                    {
                        m_Transform.position = temp;
                        bodyTransform.localPosition = bodyTemp;
                    }

                                    
                    //Change position of each "head object" with change in controller height such that it stays by the "head"
                    foreach (Transform t in headObjects)
                    {
                        temp = t.position;
                        temp.y -= changeInHeightHalved;
                        t.position = temp;
                    }

                    //Change position of each "foot object" with change in controller height such that it stays by the "foot"
                    foreach (Transform t in footObjects)
                    {
                        temp = t.position;
                        temp.y += changeInHeightHalved;
                        t.position = temp;
                    }

                    
                    // Also we have to scale the body and move the head down...
                    temp = new Vector3(bodyTransformOriginalScale.x, bodyTransformOriginalScale.y * m_Controller.height / standHeight, bodyTransformOriginalScale.z);
                    if (!float.IsNaN(bodyTransformOriginalScale.y * m_Controller.height / standHeight))
                    {
                        bodyTransform.localScale = temp;
                    }
                    
                    // // we also have to decrease the offset by half the distance crouched
                    // m_Controller.center =
                    //     new Vector3(m_Controller.center.x, m_Controller.center.y - (m_Controller.height - x) * 0.5f, m_Controller.center.z);
                    
                    m_Controller.height = x;
                }, targetHeight,
            duration)
            .SetEase(Ease.OutQuart);
    }

    private void Uncrouch(float targetHeight, float duration)
    {
        uncrouchTween = DOTween.To(() => m_Controller.height, x =>
            {
                float changeInHeightHalved = (m_Controller.height - x) * 0.5f;
                Vector3 temp = m_Transform.position;
                
                Vector3 bodyTemp = bodyTransform.localPosition;
                if (grounded)
                {
                    temp.y -= changeInHeightHalved;
                }

                bodyTemp.y += changeInHeightHalved;
                
                if (!float.IsNaN(temp.y))
                {
                    m_Transform.position = temp;
                    bodyTransform.localPosition = bodyTemp;
                }
                
                
                // Also we have to scale the body and move the head down...
                temp = new Vector3(bodyTransformOriginalScale.x, bodyTransformOriginalScale.y * m_Controller.height / standHeight, bodyTransformOriginalScale.z);
                if (!float.IsNaN(bodyTransformOriginalScale.y * m_Controller.height / standHeight))
                {
                    bodyTransform.localScale = temp;
                }
                
                //Change position of each "head object" with change in controller height such that it stays by the "head"
                foreach (Transform t in headObjects)
                {
                    temp = t.position;
                    temp.y -= changeInHeightHalved;
                    t.position = temp;
                }

                //Change position of each "foot object" with change in controller height such that it stays by the "foot"
                foreach (Transform t in footObjects)
                {
                    temp = t.position;
                    temp.y += changeInHeightHalved;
                    t.position = temp;
                }
                
                // // we also have to decrease the offset by half the distance crouched
                // m_Controller.center =
                //     new Vector3(m_Controller.center.x, m_Controller.center.y - (m_Controller.height - x) * 0.5f, m_Controller.center.z);
                
                m_Controller.height = x;
            }, targetHeight,
            duration).SetEase(Ease.OutQuart);
    }
 
    // Store point that we're in contact with for use in FixedUpdate if needed
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        m_ContactPoint = hit.point;
    }
 
 
    // This is the place to apply things like fall damage. You can give the player hitpoints and remove some
    // of them based on the distance fallen, play sound effects, etc.
    private void OnFell(float fallDistance)
    {
        //TODO: we don't need to track fallDistance so just remove all that code.
        print("Ouch! Fell " + fallDistance + " units!");
    }
}