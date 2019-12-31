using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ControlPoint : MonoBehaviour
{
    public Transform[] waypoints;
//    public List<Transform> availableWaypointsYena;
//    public HashSet<Transform> availableWaypointsYena;
//
//    public HashSet<Transform> availableWaypointsCana;
    
    public float status = 50; // 0 = captured by yena, 50 = uncaptured, 100 = captured by cana 
    public float flagCaptureRate = 1;
    
    public float startStatus = 50;
    private int yenaCount = 0;
    private int canaCount = 0;

    [Header("Indicators")]
    public GameObject flag; // the colored cube

    private float flagMaxHeight;
    public float flagMinHeight = 0;
    private float flagTargetHeight;
    
    public Material yenaFlagMaterial;
    public Material canaFlagMaterial;
    public Material neutralFlagMaterial;

    private enum FlagStatus
    {
        YenaMajority,
        CanaMajority,
        Neutral
    }

    private FlagStatus flagStatus;
    private FlagStatus prevFlagStatus;
    

    public LineRenderer laser; // the line going to the sky
    
    
    // Start is called before the first frame update
    void Start()
    {
        status = startStatus;
        flagMaxHeight = flag.transform.position.y;

        StartCoroutine(FlagTick());
        
        // initialize
        Update();



    }
    
    
    // Update is called once per frame
    void Update()
    {
        
        // 0 - <50 = yena majority = yena color
        // >50 - 100 = cana majority = cana color
        // 50 = neutral
        if (0 <= status && status < 50)
        {
            flagStatus = FlagStatus.YenaMajority;
            
        }
        else if (status == 50) // likely to only happen when initialized
        {
            flagStatus = FlagStatus.Neutral;
        }
        else if (50 < status && status <= 100) // can be replaced with just "else"
        {
            flagStatus = FlagStatus.CanaMajority;
        }


        // set flag material
        if (flagStatus != prevFlagStatus)
        {
            // this means that the status changed
            // we do this check because setting the material on a gameobject might be expensive
            // so we only change the material of the flag when we need to (when the status changed)
            switch (flagStatus)
            {
                case FlagStatus.Neutral:
                    flag.GetComponent<Renderer>().material = neutralFlagMaterial;
                    
                    // set flag light color
                    flag.GetComponent<Light>().color = neutralFlagMaterial.color;
                    break;
                case FlagStatus.CanaMajority:
                    flag.GetComponent<Renderer>().material = canaFlagMaterial;
                    
                    // set flag light color
                    flag.GetComponent<Light>().color = canaFlagMaterial.color;
                    
                    break;
                case FlagStatus.YenaMajority:
                    flag.GetComponent<Renderer>().material = yenaFlagMaterial;
                    
                    // set flag light color
                    flag.GetComponent<Light>().color = yenaFlagMaterial.color;
                    
                    break;
            }
        }
        
        // set flag height
        
        // 0 = full yena capture = full height
        // 100 = full cana capture = full height
        // 50 = uncontested = 0 height
        if (0 <= status && status <= 50)
        {

            flagTargetHeight = Mathf.Lerp(flagMaxHeight, flagMinHeight, (status / 100) * 2); // t = [0, 2] but only first half
            

            // set laser as interpolation between colors actually
            // that gives the player more information about flag status
            laser.startColor = Color.Lerp(yenaFlagMaterial.color, neutralFlagMaterial.color, (status / 100) * 2);
            laser.endColor = Color.Lerp(yenaFlagMaterial.color, neutralFlagMaterial.color, (status / 100) * 2);
        }
        else if (50 < status && status <= 100) // can be replaced with "else"
        {
            
            flagTargetHeight = Mathf.Lerp(flagMinHeight, flagMaxHeight, ((status / 100) * 2) - 1); // t = [-1, 1] but only second half
            
            
            laser.startColor = Color.Lerp(neutralFlagMaterial.color, canaFlagMaterial.color, ((status / 100) * 2) - 1);
            laser.endColor = Color.Lerp(neutralFlagMaterial.color, canaFlagMaterial.color, ((status / 100) * 2) - 1);
        } 
        
        
        // lerp flag height to flag target height;
        Vector3 newHeight = flag.transform.position;
        newHeight.y = Mathf.Lerp(newHeight.y, flagTargetHeight, Time.deltaTime * 4);
        flag.transform.position = newHeight;
        
        prevFlagStatus = flagStatus;

    }
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Yena"))
        {
            yenaCount++;
        }
        else if (other.gameObject.CompareTag("Cana"))
        {
            canaCount++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Yena"))
        {
            yenaCount--;
        }
        else if (other.gameObject.CompareTag("Cana"))
        {
            canaCount--;
        }
    }

    IEnumerator FlagTick()
    {
        //every second, look at current status of occupation and increment status 
        
        int difference = canaCount - yenaCount; // + = more cana, - = more yena
        // we want flag capture rate to scale with numbers advantage
        
        // 
        while (true)
        {
            status += difference * flagCaptureRate;
    
            status = Mathf.Clamp(status, 0, 100);
    
            yield return new WaitForSeconds(0.5f);
            
            difference = canaCount - yenaCount; // + = more cana, - = more yena
            
        }


    }

    
    
}
