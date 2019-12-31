using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControls : MonoBehaviour
{
    public Camera playerCamera;
    private Player playerScript;
    
    private float originalFOV;
    public float zoomFOV = 40;
    public float zoomSpeed = 20f;
    private float originalSensitivity;
    public float zoomSensitivityMod = 0.6f;
    private float zoomSensitivity;
    
    private bool lockCursor = true;

    public bool LockCursor
    {
        get => lockCursor;
        set
        {
            if (value)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
            
            lockCursor = value;  
        } 
    }

    // Start is called before the first frame update
    void Start()
    {
        playerCamera = GetComponent<Camera>();
        originalFOV = playerCamera.fieldOfView;

        if (LockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }

        playerScript = transform.root.GetComponent<Player>();
        
        // assumes people have the same sensitivity for X as Y
        // we could keep track of both x and y if this is not true, but this is an unlikely use case.
        originalSensitivity = playerScript.bodyMouseLook.sensitivityX;

        zoomSensitivity = originalSensitivity * zoomSensitivityMod;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButton("Fire2"))
        {
            if(Mathf.Abs(playerCamera.fieldOfView - zoomFOV) > 0.001f)
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, zoomSpeed * Time.deltaTime);

            playerScript.bodyMouseLook.sensitivityX = zoomSensitivity;
            playerScript.headMouseLook.sensitivityY = zoomSensitivity;
        }
        else
        {
            if(Mathf.Abs(playerCamera.fieldOfView - originalFOV) > 0.001f)
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, originalFOV, zoomSpeed * Time.deltaTime);
            
            
            playerScript.bodyMouseLook.sensitivityX = originalSensitivity;
            playerScript.headMouseLook.sensitivityY = originalSensitivity;
        }
    }
}
