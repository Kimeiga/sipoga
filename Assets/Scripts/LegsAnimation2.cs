using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegsAnimation2 : MonoBehaviour
{
    /// <summary>
    /// Different implementation of LegsAnimation that I think is more likely to work.
    ///
    /// Each leg has its own grid and will snap to the grid point that is closest to transform.position.
    /// The grids are interlocking and their origin is the Foot Start Positions (from the editor).
    /// Their size is a parameter.
    /// </summary>
    
    
    public Transform leftLegJoint;
    public Transform rightLegJoint;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Stage 1, one grid, both legs point to it, grid size 1
        // Stage 2, grid needs to rotate with player so that when you walk diagonally it looks right...
        //     not sure how to do this, maybe use local space instead of world space?
        
        // if moved (we have to keep track of movement), 
        // then check which grid point you are closest to.
        
        Vector3 leftFootTarget = new Vector3(roundToMultiple(transform.position.x, 2), transform.position.y, roundToMultiple(transform.position.z, 2));
        Vector3 rightFootTarget = new Vector3(roundToMultiple(transform.position.x, 2), transform.position.y, roundToMultiple(transform.position.z, 2));
        
        leftLegJoint.LookAt(leftFootTarget);
        rightLegJoint.LookAt(rightFootTarget);
        
        
    }

    int roundToMultiple(float number, int multiple)
    {
        int n = Mathf.RoundToInt(number);
        
        // smaller multiple
        int a = (n / multiple) * multiple;
        
        // larger multiple
        int b = a + multiple;
        
        // return closest of the two 
        return (n - a > b - n) ? b : a;
    }
}
