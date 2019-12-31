using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public Text timeText;

    public float roundTime = 15;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        float remainingTime = roundTime - Time.time;
        
        timeText.text = ((int) remainingTime / 60).ToString("00") + ":" + ((int) remainingTime % 60).ToString("00");
    }
}
