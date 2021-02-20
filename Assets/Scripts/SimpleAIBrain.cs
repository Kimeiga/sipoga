using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SimpleAIBrain : MonoBehaviour
{
    // we want to pick up a gun first, so we need to check if our currentItem in our inventory is empty
    // and then if so, waypoint to the nearest gun

    private SimpleInventory inventory;
    private NavMeshAgent navMeshAgent;
    
    
    
    // Start is called before the first frame update
    void Start()
    {
        inventory = GetComponent<SimpleInventory>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        
        if (!inventory.CurrentItem)
        {
            // find closest item to you in the scene. Might be good to do this with a K-d tree, but actually you 
            // want the distance by navmesh, so it will have to be O(n).
            
            // could also be done with boids if we have enough AIs

            GameObject itemToGet = GameManager.instance.FindClosestItem(transform.position);

            navMeshAgent.destination = itemToGet.transform.position;
        }

        inventory.onCurrentItemChanged += CurrentItemChanged;
    }

    // Update is called once per frame
    void Update()
    {
        // this will be a kind of priority tree brain
        // where we are just constantly evaluating our situation, but we can convert to a finite state machine 
        // if necessary i suppose. Even though this will kinda be like a finite state machine as is.

    }

    void CurrentItemChanged(GameObject item)
    {
        // the current item in the inventory has changed. We need to recheck the 
        
        if (!inventory.CurrentItem)
        {
            // find closest item to you in the scene. Might be good to do this with a K-d tree, but actually you 
            // want the distance by navmesh, so it will have to be O(n).
            
            // could also be done with boids if we have enough AIs

            GameObject itemToGet = GameManager.instance.FindClosestItem(transform.position);

            navMeshAgent.destination = itemToGet.transform.position;
        }
    }
}
