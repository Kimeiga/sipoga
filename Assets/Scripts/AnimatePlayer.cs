using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatePlayer : MonoBehaviour
{
    
    // if this is a player, we can use "Grounded" from FirstPersonMovement
    // if not, we can implement it ourself

    private FirstPersonMovement firstPersonMovement;
    
    
    [Header("isGrounded for AI")]
    private bool isAI;
    private RaycastHit _hit;
    public float groundedVectorScale = 1;
    private bool aiIsGrounded = true;
    
    // Start is called before the first frame update
    void Start()
    {
        try
        {
            firstPersonMovement = GetComponent<FirstPersonMovement>();
            isAI = false;
        }
        catch (Exception e)
        {
            isAI = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // if isAI, we have to get grounded by ourself
        if (isAI)
        {
            if (Physics.Raycast(transform.position, groundedVectorScale * -Vector3.up, out _hit))
            {
                print("AI is on top of " + _hit.transform.name);
                aiIsGrounded = true;   
            }
            else
            {
                print("AI is not grounded");
                aiIsGrounded = false;
            }
        }
        
        // draw the grounded vector so you can tune it
        if (aiIsGrounded)
        {
            Debug.DrawLine(transform.position, transform.position + (groundedVectorScale * -Vector3.up), Color.green);
        }
        else
        {
            Debug.DrawLine(transform.position, transform.position + (groundedVectorScale * -Vector3.up), Color.magenta);
        }
        
        // if grounded, find 2d velocity and use that to determine legs joint rotation and play walk animation
        
        // if not grounded, find velocity and make legs point in opposite direction (as if jumping)
        //    for the "up" part of the jump
        // and when you start to fall, find velocity and make legs point in that direction (as if landing with your feet)
        //    for the "down" part of the jump
        
        
    }
}
