using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeLowerBodyInvisible : MonoBehaviour
{
    public Player player;

    [SerializeField]
    private bool activated = false;
    public bool Activated
    {
        get { return activated; }
        set
        {
            if (value)
            {
                player.MakeLowerBodyInvisible();
            }
            else
            {
                player.MakeLowerBodyVisible();
            }
            activated = value;
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        player = transform.root.GetComponent<Player>();

        if (Activated)
        {
            player.MakeLowerBodyInvisible();
        }
        else
        {
            player.MakeLowerBodyVisible();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
