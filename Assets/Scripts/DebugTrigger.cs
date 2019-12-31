using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

//    private void OnTriggerStay(Collider other)
//    {
//        print(other.gameObject.name);
//    }

    private void OnTriggerEnter(Collider other)
    {
        print("Enter: " + other.gameObject.name);
    }
    
    
    private void OnTriggerExit(Collider other)
    {
        print("Exit: " + other.gameObject.name);
    }
    
   
}
