using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
    public bool isU; // true = u, false = t
    public Transform head;

    public float health;
    public float maxHealth = 100;

    public float armor;
    public float maxArmor = 100;
    
    [Header("Body Parts")]
    public GameObject torso;
    public GameObject leftLeg;
    public GameObject rightLeg;
    public GameObject[] bodyTexts;

    [Header("Scripts (Player Only)")]
    public MouseLook bodyMouseLook;
    public MouseLook headMouseLook;
    
    // Start is called before the first frame update
    void Start()
    {
        health = maxHealth;
        armor = 0;
    }

    public void IncreaseHealth(float change)
    {
        health += change;
    }
    
    public void DecreaseHealth(float change)
    {
        health += change;

        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }

    public void MakeLowerBodyInvisible()
    {
        // torso.layer = LayerMask.NameToLayer("Invisible Body");
        // leftLeg.layer = LayerMask.NameToLayer("Invisible Body");
        // rightLeg.layer = LayerMask.NameToLayer("Invisible Body");
        // Can't do this because then they don't cast any shadows and when you look down you just see the shadow of a floating head

        torso.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        leftLeg.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        rightLeg.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        
        foreach (GameObject bodyText in bodyTexts)
        {
            bodyText.layer = LayerMask.NameToLayer("Invisible Body");
        }
    }
    
    public void MakeLowerBodyVisible()
    {
        string layer = isU ? "U" : "T";
        // torso.layer = LayerMask.NameToLayer(layer);
        // leftLeg.layer = LayerMask.NameToLayer(layer);
        // rightLeg.layer = LayerMask.NameToLayer(layer);
        
        torso.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
        leftLeg.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
        rightLeg.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;

        foreach (GameObject bodyText in bodyTexts)
        {
            bodyText.layer = LayerMask.NameToLayer(layer);
        }
    }
}
