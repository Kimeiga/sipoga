using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FOVTest : MonoBehaviour
{

    public Transform target;

    public float maxAngle = 120;

    private Renderer renderer;
    // Start is called before the first frame update
    void Start()
    {
        renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 relativeNormalizedPos = (target.position - transform.position).normalized;
                
        float dot = Vector3.Dot(relativeNormalizedPos, transform.forward);
 
        // angle difference between looking direction and direction to item (radians)
        float angle = Mathf.Acos(dot);

        float maxAngleRadians = Mathf.Deg2Rad * maxAngle;

        print("angle: " + angle * Mathf.Rad2Deg);
        
        if(angle < maxAngleRadians) {
            // this enemy is within player's FOV
            renderer.material.color = Color.green;
        }
        else
        {
            renderer.material.color = Color.red;            
        }
    }
}
