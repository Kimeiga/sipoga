using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    public float roundTime = 15;

    public TextMeshProUGUI ammoText;

    private static HUDManager _instance;

    public static HUDManager Instance
    {
        get { return _instance; }
    }


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    // Update is called once per frame
    void Update()
    {
        float remainingTime = roundTime - Time.time;

        timeText.text = ((int) remainingTime / 60).ToString("00") + ":" + ((int) remainingTime % 60).ToString("00");
    }
}