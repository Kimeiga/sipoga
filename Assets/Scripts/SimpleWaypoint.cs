using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SimpleWaypoint : MonoBehaviour
{
    public Transform waypoint;
    public NavMeshAgent navMeshAgent;
    
    // Start is called before the first frame update
    void Start()
    {
        navMeshAgent.SetDestination(waypoint.position);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
