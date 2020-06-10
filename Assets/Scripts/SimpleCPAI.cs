using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SimpleCPAI : MonoBehaviour
{
    // given a control point, it find a random waypoint in it, adds noise, and goes to it if its in the CP

    public ControlPoint controlPoint;
    private int desiredStatus;
//    public Transform waypoint;
    private NavMeshAgent navMeshAgent;
    private Player player;
    
    public NavMeshQueryFilter controlPointFilter;
    
    private bool isU; // 1 = U, 0 = T
    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Player>();
        isU = player.isU;
        navMeshAgent = GetComponent<NavMeshAgent>();
//        if (isYena)
//        {
//            foreach (Transform waypoint in controlPoint.availableWaypointsYena)
//            {
//                float distance = Vector3.Distance(transform.position, )
//            }
//
//            controlPoint.availableWaypointsYena
//        }
//        
//        navMeshAgent.SetDestination(waypoint.position);
//        navMeshAgent.SamplePathPosition()
//        NavMesh.SamplePosition()

        controlPointFilter.areaMask = 1 << 3;
        desiredStatus = isU ? 0 : 100;
        
        
//        Transform randomWaypoint = controlPoint.waypoints[Random.Range(0, controlPoint.waypoints.Length)];
//
//        NavMeshHit hit;
//        if (NavMesh.SamplePosition(randomWaypoint.position, out hit, 2.0f, NavMesh.AllAreas))
//        {
//            // hopefully this returns a point on the navmesh in the cp
//            Debug.DrawLine(hit.position, hit.position + Vector3.up, Color.yellow);
//            navMeshAgent.SetDestination(hit.position);
//        }
        StartCoroutine(GoToControlPoint());
    }

    IEnumerator GoToControlPoint()
    {
        while (Mathf.Abs(controlPoint.status - desiredStatus) > 0.1f)
        {
            Transform randomWaypoint = controlPoint.waypoints[Random.Range(0, controlPoint.waypoints.Length)];
    
            NavMeshHit hit;
            Vector3 randomPoint = randomWaypoint.position + Random.insideUnitSphere * 5;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
            }

//            while (navMeshAgent.remainingDistance >= 0.3f)
//            {
//                // hopefully this returns a point on the navmesh in the cp
////                Debug.DrawLine(hit.position, hit.position + Vector3.up, Color.yellow);
//                
//            }
//            
            yield return new WaitUntil(() => navMeshAgent.remainingDistance < 0.3f);
            
        }
    }
    
//    void OnDrawGizmos()
//    {
//        Gizmos.color = Color.red;
//        Gizmos.DrawSphere(this.transform.position, 1);
//    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
