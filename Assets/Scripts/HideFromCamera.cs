using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Turns on shadows only for all objects
/// </summary>
public class HideFromCamera : MonoBehaviour
{
    
    public GameObject[] objectsToHide;
    
    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject obj in objectsToHide)
        {
            obj.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
        
    }

}
