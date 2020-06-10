using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleShootEnemy : MonoBehaviour
{
    private Player player;
    private Transform head;

    private Player closestEnemy;
    private List<Player> closeEnemyList = new List<Player>(); 
    private Transform closestEnemyHead;

    public Player targetEnemy;

    public LayerMask seeEnemyMask;
    public float maxAngle = 120;

    public bool debug = false;
    
    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Player>();
        head = player.head;
    }
    
    private bool IsValidEnemy(Player obj)
    {
        // is in FOV
        Transform enemyHead = obj.head;     
        Vector3 relativeNormalizedPos = (enemyHead.position - head.transform.position).normalized;
                
        float dot = Vector3.Dot(relativeNormalizedPos, head.forward);
 
        // angle difference between looking direction and direction to item (radians)
        float angle = Mathf.Acos(dot);

        float maxAngleRadians = Mathf.Deg2Rad * maxAngle;
            
        if(angle > maxAngleRadians) {
            // this enemy is within player's FOV
//                print(angle);
            return false;
//                break;
        }
        
        
        // is able to be seen
        RaycastHit hit;
        if (Physics.Linecast(head.transform.position, enemyHead.transform.position, out hit, seeEnemyMask,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    Player FindNewEnemy()
    {
//        print(player.isYena ?
//            GameManager.instance.canaTeam.FindClosestWithCondition(transform.position, IsInFOV) :
//            GameManager.instance.yenaTeam.FindClosestWithCondition(transform.position, IsInFOV));
        
        Player chosenEnemy = player.isU ?
            GameManager.instance.tTeam.FindClosestWithCondition(transform.position, IsValidEnemy) :
            GameManager.instance.uTeam.FindClosestWithCondition(transform.position, IsValidEnemy);
        
//        print(chosenEnemy.gameObject.name);

        return chosenEnemy;
//        closeEnemyList.Clear();
//        
//        if (player.isYena)
//        {
//            closeEnemyList.AddRange(GameManager.instance.canaTeam.FindClose(transform.position));    
//        }
//        else // is cana
//        {
//            closeEnemyList.AddRange(GameManager.instance.yenaTeam.FindClose(transform.position));                
//        }
//
//        foreach (Player enemy in closeEnemyList)
//        {
//            if (debug)
//            {
//                print(enemy.gameObject.name);
//            }
//        }
//
//        
//        foreach (Player enemy in closeEnemyList)
//        {
//            Transform enemyHead = enemy.head;     
//            Vector3 relativeNormalizedPos = (enemyHead.position - head.transform.position).normalized;
//                
//            float dot = Vector3.Dot(relativeNormalizedPos, head.forward);
// 
//            // angle difference between looking direction and direction to item (radians)
//            float angle = Mathf.Acos(dot);
//
//            float maxAngleRadians = Mathf.Deg2Rad * maxAngle;
//            
//            if(angle < maxAngleRadians) {
//                // this enemy is within player's FOV
////                print(angle);
//                return enemy;
////                break;
//            }
//
//        }

        return null;
    }



    // Update is called once per frame
    void Update()
    {
        // find target enemy if there is not one already
        if (!targetEnemy)
        {
            Player enemy = FindNewEnemy();
            
            if (enemy)
            {
                print(enemy);
                
                targetEnemy = enemy;
            }
        }
        
        if (targetEnemy)
        {
            // this means that we will be doing the same linecast twice every frame if both enemies are attacking each other, 
            // we could speed this up by doing them for only one of the two enemies
            // this could be handled in a global class that memoizes the results
            // buuuuuut it might be better to leave it like this to reduce complexity when one ai targets the other
            // but not the other way around

            RaycastHit hit;
            if (Physics.Linecast(head.transform.position, targetEnemy.head.transform.position, out hit, seeEnemyMask, QueryTriggerInteraction.Ignore))
            {
                // if the target enemy gets behind cover (their head is no longer visible), then we dont have a target enemy anymore
                targetEnemy = null;
                
            }
            
        }
        
        
//        // just find closest enemy first
//        
//        if (player.isYena)
//        {
////          
////            closestEnemy = GameManager.instance.canaTeam.FindClose(transform.position);
////            closestEnemyHead = closestEnemy.GetComponent<SimpleShootEnemy>().head;
//        }
//        else // player is cana
//        {
//            closestEnemy = GameManager.instance.yenaTeam.FindClosest(transform.position);
//            closestEnemyHead = closestEnemy.GetComponent<SimpleShootEnemy>().head;
//
//        }
//        

        if (targetEnemy)
        {
            
        // look directly at him
            // need to rotate our root transform left right
            // and our head transform up down 
            // this mimics how the regular player looks
            
        // to make body only rotate left right, put target on same ground plane as body
        Vector3 bodyTarget = targetEnemy.transform.position;
        bodyTarget.y = transform.position.y;
        transform.LookAt(bodyTarget);
        
        // to make head only rotate up down, put target on same head plane (somehow...)
        // project cana position onto vertical looking plane
        
        // create plane going through head with normal to head's right
        
        // actually this wont work


//        Debug.DrawLine(head.transform.position, closestEnemyHead.transform.position, Color.green);
//        Transform test = transform;
//        test.LookAt(closestEnemy.transform.position);
//        Vector3 test2 = test.localRotation.eulerAngles;
//        test2.x = 0;
//        test2.z = 0;
//        transform.localRotation = Quaternion.Euler(test2);
            
                
        head.transform.LookAt(targetEnemy.head.transform);
        Vector3 clampedHeadRotation = head.transform.localRotation.eulerAngles;
        clampedHeadRotation.y = 0;
        clampedHeadRotation.z = 0;
        head.transform.localRotation = Quaternion.Euler(clampedHeadRotation);
        
        Debug.DrawRay(head.transform.position, head.transform.forward * 3, Color.cyan);
//            Vector3 directionToEnemy = closestEnemy.transform.position - transform.position;
//            Debug.DrawLine(head.transform.position, directionToEnemy, Color.cyan);
//            
//            directionToEnemy = head.transform.InverseTransformDirection(directionToEnemy);
//            
//            
//            Vector3 headingToEnemy = new Vector3(directionToEnemy.x, 0, directionToEnemy.z);
//            Debug.DrawLine(head.transform.position, head.transform.position + headingToEnemy, Color.blue);
//
//
//            float angle = Vector3.SignedAngle(headingToEnemy, directionToEnemy,
//                Vector3.Cross(headingToEnemy, directionToEnemy));
//            
//            // rotate head to this angle
//            // eventually lerp
//            Vector3 newHeadRotation = head.transform.rotation.eulerAngles;
//            newHeadRotation.x = angle;
//            head.transform.rotation = Quaternion.Euler(newHeadRotation);            
        }

        
    }
}
