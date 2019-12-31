using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//some item types i came up with
public enum ItemType { Melee, Gun, Gadget, Sundry }


public class Item : MonoBehaviour
{

    [Header("Classification")]
    //this isn't really that important anymore, but maybe it will help with making the UI specilized or something;
    //can't hurt
    public ItemType type;
    public string itemName;

    //is the item in player's hands?
    public bool active = false;

    [Space(10)]
    [Header("Holding")]

    //hand holds
    public Transform rightHandHold;
    public Transform leftHandHold;

    //base local pos/rot for holding
    public Vector3 localHoldPosition;
    public Vector3 localHoldRotation;


    public Collider col;




	//color changing when being aimed at stuff

    private bool highlighted = false;

    private List<Material> itemMaterials = new List<Material>();

    private Color[] initialEmissionColors;
    public float colorMod = 5;
    private Color[] finalEmissionColors;

	// Use this for initialization
	void Start () {

        //Inspector derp failsafes

        //if I didn't set the collider
        if(col == null){

            //use the one on the root
            col = gameObject.GetComponent<Collider>();

            //if there isn't one
            if(col == null){

                //iterate through the kids
				foreach (Transform child in transform)
				{

                    //if the current kid has a collider
                    if(child.gameObject.GetComponent<Collider>() != null){

                        //just use that one
                        col = child.gameObject.GetComponent<Collider>();

                        //break: therefore the first kid with the collider wins
                        break;
                    }
				}
            }

        }

        //if I didn't set right hand hold
        if(rightHandHold == null){
            rightHandHold = transform.Find("Right Hand Hold");
        }

		//if I didn't set left hand hold
		if (leftHandHold == null)
		{
            leftHandHold = transform.Find("Left Hand Hold");
		}

		//end derp failsafes...

        //initialize the lists of the item's renderers and materials

        //add whatever renderer or material might be on the root object
        if(GetComponent<Renderer>() != null){
            Renderer ren1 = GetComponent<Renderer>();

            itemMaterials.Add(ren1.material);
        }


        //now iterate through children for further renderers and materials
		Component[] childRenderers = GetComponentsInChildren(typeof(Renderer));

		foreach (Renderer ren in childRenderers)
		{
			itemMaterials.Add(ren.material);
		}


		//initialize emission colors for shine mechanic
		initialEmissionColors = new Color[itemMaterials.Count];
		finalEmissionColors = new Color[initialEmissionColors.Length];

        //populate initial and final emission colors
        for (int i = 0; i < itemMaterials.Count; i++)
        {
            //enable emission color if not enabled already
            itemMaterials[i].EnableKeyword("_EMISSION");

			//set the initial and final colors for the lerps
			initialEmissionColors[i] = itemMaterials[i].GetColor("_EmissionColor");

			//if it's black, just use grey as highlight emission color
			if (itemMaterials[i].GetColor("_EmissionColor") == Color.black)
			{
                //if grey is too bright/dark, we can always change this around
				finalEmissionColors[i] = Color.grey;
			}
			else
			{
                //if not, then multiply the base color to get a nice highlight emission color
                //colorMod will take some playing around with...
				finalEmissionColors[i] = itemMaterials[i].GetColor("_EmissionColor") * colorMod;
			}


        }


    }
	
	// Update is called once per frame
	void FixedUpdate () {


	}

    public void Highlight(){

        highlighted = true;

		for (int i = 0; i < itemMaterials.Count; i++)
		{
			itemMaterials[i].SetColor("_EmissionColor", finalEmissionColors[i]);
		}
    }

    public void Unhighlight(){

        highlighted = false;

		for (int i = 0; i < itemMaterials.Count; i++)
		{
			itemMaterials[i].SetColor("_EmissionColor", initialEmissionColors[i]);
		}
		
    }

}
