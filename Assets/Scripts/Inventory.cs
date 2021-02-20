using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Inventory : MonoBehaviour
{
	// again, need to optimize for AI
	// this means it would be easier to pick up weapons on collision
	// then if they would pick them up 

	// if it is to be HUDless, then the inventory should be pretty simple,
	// and easy to operate without looking (game is not HUDless, but is simple HUD)

	// this could mean mouse scroll because you are guaranteed to find every weapon this way
	// If you do button presses, it might be easy to forget which buttons for which item 

	// but i dont like mouse scroll ...
	// its slow

	// ok then i do button presses but there will not be many buttons,
	// and it won't be slot based, each slot can only fit a thing of 1 type (and maybe there are some omnislots


	// this means that we should decide what things the player can carry
	// of course there are guns

	// Important:
	// AI will look more intelligent if they ask each other for things and give each other things
	// rather than just looking for things in the level
	// this shows "self sufficiency of squads"

	private Player player;
	
	//Poder Variables
	private bool stateChange;
	
	//Grabbing Behaviour Variables
	private GameObject nextItem;
	private Item nextItemScript;

	private GameObject prevItem;
	private Item prevItemScript;

	[Header("Grab Timing")] 
	public float grabTimeMod = 0.3f;
	private float grabTime = 1;
	public float grabTimeLower = 0.4f;
	public float grabTimeUpper = 0.7f;

	[Space(10)] 
	[Header("Grab Raycast")]
	//Initial Grab Raycast Variables
	public Transform fireTransform;

	private MouseLook fireTransformScript;
	private MouseLook bodyTransformScript;
	private MouseLook lookTransformScript;
	public float grabRange = 4;
	private LayerMask grabLayerMask;
	private RaycastHit hit;

	GameObject hitItem;
	Item hitItemScript;

	[Space(10)] 
	[Header("Hands")]

	//Hands Variables
	public Transform leftHandTransform;

	private Transform[] leftHandChildren;
	private Vector3 leftHandOriginPos;
	private Quaternion leftHandOriginRot;

	public Transform rightHandTransform;
	private Transform[] rightHandChildren;
	private Vector3 rightHandOriginPos;
	private Quaternion rightHandOriginRot;

	public Transform handsTransform;

	[Space(10)]
	[Header("Holsters")] public Transform[] holsters;

	[Space(10)]
	[Header("Player Properties")]
	public Collider playerCollider;

	private FPSWalker movementScript;


	[Space(10)] 
	[Header("Inventory")]

	//Inventory Variables
	public GameObject[] inventorySub;

	public GameObject[] inventory
	{
		get { return inventorySub; }
		set { inventorySub = value; }
	}

	public int maxInventorySize = 8;

	public int inventoryInd = 0;
	public int startingInventoryIndex = 0;

	public int inventoryIndex
	{
		get { return inventoryInd; }
		set { inventoryInd = value; }
	}


	[Space(10)] [Header("Drop and Throw")]

	//drop variables:
	public float dropTime = 0.3f;

	public float dropForce = 0.5f;
	public float throwForce = 3;
	public float dropMod = 10;

	/*
	[Space(10)]
	[Header("UI")]


	private GameObject ammoPanel;
	public Text ammoText;

	public Text[] slotTexts;

	public GameObject selectorPanel;
	private RectTransform selectorPanelTrans;
	private Image selectorPanelImage;
	private Color selectorPanelColor;
	*/

	// Use this for initialization
	void Start()
	{
		player = GetComponent<Player>();
		fireTransform = player.body.head;
		fireTransformScript = player.body.headMouseLook;
		bodyTransformScript = player.body.bodyMouseLook;
		lookTransformScript = player.body.lookMouseLook;

		//set ammopanel for ammo updating
		//ammoPanel = ammoText.transform.parent.gameObject;

		//set fpmscript for enhanced drop mechanic
		movementScript = GetComponent<FPSWalker>();


		inventory = new GameObject[maxInventorySize];
		inventoryIndex = startingInventoryIndex;

		//store right/left hand children to make the layer switching faster
		rightHandChildren = rightHandTransform.GetComponentsInChildren<Transform>();
		leftHandChildren = leftHandTransform.GetComponentsInChildren<Transform>();

		//set grab layer mask for the items:
		grabLayerMask = 1 << LayerMask.NameToLayer("Item");


		//set hand origins
		rightHandOriginPos = rightHandTransform.localPosition;
		rightHandOriginRot = rightHandTransform.localRotation;
		leftHandOriginPos = leftHandTransform.localPosition;
		leftHandOriginRot = leftHandTransform.localRotation;

		//set statechange to false
		stateChange = false;

		//get player collider
		playerCollider = GetComponent<Collider>();

		/*
		for (int i = 0; i < inventory.Length; i++)
		{
			if (inventory[i])
			{
				slotTexts[i].text = inventory[i].GetComponent<Item>().itemName + "  " + (i + 1).ToString();
			}
			else
			{
				slotTexts[i].text = (i + 1).ToString();
			}
			
		}

		slotTexts[inventoryIndex].color = Color.white;
		slotTexts[inventoryIndex].fontStyle = FontStyle.Bold;

		selectorPanelTrans = selectorPanel.GetComponent<RectTransform>();
		selectorPanelImage = selectorPanel.transform.GetChild(0).GetComponent<Image>();
		selectorPanelColor = selectorPanelImage.color;
		*/


		DOTween.Init();
	}

	// Update is called once per frame
	void Update()
	{
		if (stateChange == false)
		{
			if (Physics.Raycast(fireTransform.position, fireTransform.forward, out hit, grabRange, grabLayerMask))
			{
				//oneshot
				//if you go from looking at nothing to looking at an item
				if (hitItem == null)
				{
					hitItem = hit.transform.gameObject;
					hitItemScript = hitItem.GetComponent<Item>();

					//highlight item here
					hitItemScript.Highlight();
				}

				//oneshot
				//if you go from looking at one item to another
				if (hit.transform.gameObject != hitItem)
				{
					//unhighlight item
					hitItemScript.Unhighlight();

					//store new item
					hitItem = hit.transform.gameObject;
					hitItemScript = hitItem.GetComponent<Item>();
					//rehighlight
					hitItemScript.Highlight();
				}


				//ONESHOT: player wants to grab something
				if (Input.GetButtonDown("Grab"))
				{
					//set the fire transform if it's a gun
					if (hitItemScript.type == ItemType.Gun)
					{
						Gun hitItemGun = hitItem.GetComponent<Gun>();
						hitItemGun.fireTransform = fireTransform;
						hitItemGun.fireTransformRotate = fireTransformScript;
						hitItemGun.lookTransformRotate = lookTransformScript;
						hitItemGun.bodyTransformRotate = bodyTransformScript;
						hitItemGun.handsSway = handsTransform.GetComponent<Sway>();
					}


					//as soon as you grab, you can stop highlighting the item
					//dehighlight
					hitItemScript.Unhighlight();

					hitItem = null;
					hitItemScript = null;


					//this should be only called once, so I'm moving it here
					//technically things in "Item" Layer should have item scripts but i'm just making sure
					if (hit.transform.gameObject.GetComponent<Item>() != null)
					{
						//set working variables
						stateChange = true;
						nextItem = hitItem;
						nextItemScript = hitItemScript;

						//got yer hands out
						if (inventory[inventoryIndex] == null)
						{
							StartCoroutine(GrabItem(hit.transform.gameObject));
						}
						else
						{
							//declare workvar for free slot index
							int freeIndex = -1;


							for (int i = 0; i < inventory.Length; i++)
							{
								if (inventory[i] == null)
								{
									freeIndex = i;
									break;
								}
							}

							//if it's still null, then there are no free slots
							if (freeIndex == -1)
							{
								//eventually write a function that drops the item in the last slot and equips the new item
								DropAndSwitchToItem(hit.transform.gameObject);
							}
							else
							{
								HolsterAndSwitchToItem(hit.transform.gameObject, freeIndex);
							}
						}
					}
				}
			}
			//oneshot
			//go from looking at something to nothing
			else if (hitItem != null)
			{
				//dehighlight
				hitItemScript.Unhighlight();

				hitItem = null;
				hitItemScript = null;
			}


			if (inventory[inventoryIndex] != null)
			{
				if (Input.GetButtonDown("Drop"))
				{
					DropItem(dropForce, inventory[inventoryIndex]);
				}
				else if (Input.GetButtonDown("Throw"))
				{
					DropItem(throwForce, inventory[inventoryIndex]);
				}
			}
			
			// primary weapon on scroll up
			if (ScrollInput.mouseScrollUpButton && inventoryIndex != 0 && 0 < inventory.Length)
			{
				StartCoroutine(SwitchToItem(0));
				//print("switching to 0");
			}
			// secondary weapon on scroll down
			if (ScrollInput.mouseScrollDownButton && inventoryIndex != 1 && 1 < inventory.Length)
			{
				StartCoroutine(SwitchToItem(1));
				//print("switching to 1");
			}
			// tertiary weapon (knife) on scroll button
			if (Input.GetButtonDown("Slot 2") && inventoryIndex != 2 && 2 < inventory.Length)
			{
				StartCoroutine(SwitchToItem(2));
				//print("switching to 2");
			}
			// slot 3 (4th weapon) is on number 1, 
			// these next 5 slots are on 1 - 5 for simplicity!
			if (Input.GetButtonDown("Slot 3") && inventoryIndex != 3 && 3 < inventory.Length)
			{
				StartCoroutine(SwitchToItem(3));
				//print("switching to 3");
			}

			if (Input.GetButtonDown("Slot 4") && inventoryIndex != 4 && 4 < inventory.Length)
			{
				StartCoroutine(SwitchToItem(4));
				//print("switching to 4");
			}

			if (Input.GetButtonDown("Slot 5") && inventoryIndex != 5 && 5 < inventory.Length)
			{
				StartCoroutine(SwitchToItem(5));
				//print("switching to 5");
			}

			if (Input.GetButtonDown("Slot 6") && inventoryIndex != 6 && 6 < inventory.Length)
			{
				StartCoroutine(SwitchToItem(6));
				//print("switching to 6");  
			}

			if (Input.GetButtonDown("Slot 7") && inventoryIndex != 7 && 7 < inventory.Length)
			{
				StartCoroutine(SwitchToItem(7));
				//print("switching to 7");  
			}
		}
	}


	IEnumerator GrabItem(GameObject item, bool? useTheRightHand = null, bool fromHolsterAndSwitchTo = false)
	{
		//PHASE 1: 1 HAND GOES TO ITEM HAND HOLD

		//set working variables
		stateChange = true;
		nextItem = item;
		nextItemScript = nextItem.GetComponent<Item>();


		if (useTheRightHand == null)
		{
			//find distances for each hand
			float distanceForRightHand =
				Vector3.Distance(rightHandTransform.position, nextItemScript.rightHandHold.position);
			float distanceForLeftHand =
				Vector3.Distance(leftHandTransform.position, nextItemScript.leftHandHold.position);

			//find if right hand or if left hand should pick up the item
			useTheRightHand = distanceForRightHand < distanceForLeftHand;
		}

		//recast
		bool useRightHand = (bool) useTheRightHand;


		if (useRightHand)
		{
			//unparent right hand so it can move to gun smoothly
			rightHandTransform.parent = null;

			// //you should do this to the hands so that it looks ok:
			// rightHandTransform.gameObject.layer = LayerMask.NameToLayer("Body Part Alive");
			// foreach (Transform trans in rightHandChildren)
			// {
			// 	trans.gameObject.layer = LayerMask.NameToLayer("Body Part Alive");
			// }

			//set grab time to something proportional to the distance to the next item
			grabTime = Vector3.Distance(rightHandTransform.position, nextItemScript.rightHandHold.position) *
					   grabTimeMod;
			grabTime = Mathf.Clamp(grabTime, grabTimeLower, grabTimeUpper);

			//Rotate right hand to align with right hand hold
			rightHandTransform.DORotate(nextItemScript.rightHandHold.rotation.eulerAngles, grabTime)
				.SetEase(Ease.OutQuart);


			//Move right hand toward right hand transform
			rightHandTransform.DOMove(nextItemScript.rightHandHold.position, grabTime).SetEase(Ease.OutQuart);
		}
		else
		{
			//unparent left hand
			leftHandTransform.parent = null;

			// //making the left hand look ok in 3d space.
			// leftHandTransform.gameObject.layer = LayerMask.NameToLayer("Body Part Alive");
			// foreach (Transform trans in leftHandChildren)
			// {
			// 	trans.gameObject.layer = LayerMask.NameToLayer("Body Part Alive");
			// }

			//setting grab time proportionally
			grabTime = Vector3.Distance(leftHandTransform.position, nextItemScript.leftHandHold.position) * grabTimeMod;
			grabTime = Mathf.Clamp(grabTime, grabTimeLower, grabTimeUpper);

			//Rotate left hand to align with left hand hold
			leftHandTransform.DORotate(nextItemScript.leftHandHold.rotation.eulerAngles, grabTime)
				.SetEase(Ease.OutQuart);

			//Move left hand toward left hand transform
			leftHandTransform.DOMove(nextItemScript.leftHandHold.position, grabTime).SetEase(Ease.OutQuart);
		}


		yield return new WaitForSeconds(grabTime);

		//PHASE 2: BRING ITEM TO THE ITEM'S PREFERRED HOLD POSITION
		//BRING NON GRABBING HAND TO ITS HAND HOLD ON THE ITEM


		//put it in the inventory
		inventory[inventoryIndex] = nextItem;

		//slotTexts[inventoryIndex].text = nextItemScript.itemName + "  " + (inventoryIndex + 1).ToString();


		if (nextItemScript.type == ItemType.Gun)
		{
			//ammoPanel.SetActive(true);
			// SimpleGun gunScript = nextItem.GetComponent<SimpleGun>();
			Gun gunScript = nextItem.GetComponent<Gun>();
			//ammoText.text = gunScript.Ammo.ToString();
			gunScript.inventoryScript = gameObject.GetComponent<Inventory>();

			fireTransformScript.ResetOffsets();
			lookTransformScript.ResetOffsets();
			bodyTransformScript.ResetOffsets();

			// gunScript.currentRecoil = 0;
			// gunScript.xRecoilOffset = 0;
			// gunScript.yRecoilOffset = 0;
		}

		//deactivate rigidbody physics
		Rigidbody rigid = nextItem.GetComponent<Rigidbody>();
		rigid.isKinematic = true;
		rigid.useGravity = false;


		//turn off that fucking collider so it doesn't fucking move my dude and it doesn't cause excess collision events and trigger events and ahofidajwepoijvz
		nextItemScript.collider.enabled = false;

		// //put item on Player Item Layer so it doesn't appear to clip through walls
		// nextItem.layer = LayerMask.NameToLayer("Player Item");
		//
		// //do this for all its children
		// Transform[] childTransforms = nextItem.GetComponentsInChildren<Transform>();
		// foreach (Transform trans in childTransforms)
		// {
		// 	trans.gameObject.layer = LayerMask.NameToLayer("Player Item");
		// }
		//
		// leftHandTransform.gameObject.layer = LayerMask.NameToLayer("Player Item");
		// foreach (Transform trans in leftHandChildren)
		// {
		// 	trans.gameObject.layer = LayerMask.NameToLayer("Player Item");
		// }
		//
		//
		// rightHandTransform.gameObject.layer = LayerMask.NameToLayer("Player Item");
		// foreach (Transform trans in rightHandChildren)
		// {
		// 	trans.gameObject.layer = LayerMask.NameToLayer("Player Item");
		// }

		//child item to super hand to make preferred local position and rotation easier to calculate
		nextItem.transform.parent = handsTransform;


		//set grab time proportional to distance between hold position and the item's position
		grabTime = Vector3.Distance(nextItem.transform.position,
					   handsTransform.TransformPoint(nextItemScript.localHoldPosition)) * grabTimeMod;
		grabTime = Mathf.Clamp(grabTime, grabTimeLower, grabTimeUpper);

		//move item to preferred hold position
		nextItem.transform.DOLocalMove(nextItemScript.localHoldPosition, grabTime).SetEase(Ease.OutQuart);

		//move item to preferred hold rotation
		//LeanTween.rotateLocal(nextItem, nextItemScript.localHoldRotation, grabTime).setEase(LeanTweenType.easeOutQuart);
		nextItem.transform.DOLocalRotateQuaternion(Quaternion.Euler(nextItemScript.localHoldRotation), grabTime)
			.SetEase(Ease.OutQuart);


		if (useRightHand)
		{
			//child right hand to item to allow right hand to move along with item
			rightHandTransform.parent = nextItem.transform;

			leftHandTransform.parent = handsTransform;
			
			//rotate left hand to left hand hold on item
			leftHandTransform.DOLocalRotate(Quaternion.Euler(nextItemScript.localHoldRotation) *
											Quaternion.Euler(nextItemScript.leftHandHold.localEulerAngles)
												.eulerAngles, grabTime).SetEase(Ease.OutQuart);

			//move left hand to left hand hold on item
			leftHandTransform.DOLocalMove(nextItemScript.leftHandHold.localPosition + nextItemScript.localHoldPosition, grabTime).SetEase(Ease.OutQuart);
		}
		else
		{
			//child left hand to item to allow right hand to move along with item
			leftHandTransform.parent = nextItem.transform;

			rightHandTransform.parent = handsTransform;
			
			//rotate right hand to right hand hold on item
			rightHandTransform.DOLocalRotate(Quaternion.Euler(nextItemScript.localHoldRotation) *
											 Quaternion.Euler(nextItemScript.rightHandHold.localEulerAngles)
												 .eulerAngles, grabTime).SetEase(Ease.OutQuart);

			//move right hand to right hand hold on item
			rightHandTransform.DOLocalMove(nextItemScript.rightHandHold.localPosition + nextItemScript.localHoldPosition, grabTime).SetEase(Ease.OutQuart);
		}


		yield return new WaitForSeconds(grabTime);


		if (fromHolsterAndSwitchTo)
		{
			//nullify relavant vars 
			prevItem = null;
			prevItemScript = null;
		}


		//child non grab hand to the gun
		if (useRightHand)
		{
			leftHandTransform.parent = nextItem.transform;
		}
		else
		{
			rightHandTransform.parent = nextItem.transform;
		}


		//reactivate item
		nextItemScript.active = true;


		//nullify working variables
		nextItem = null;
		nextItemScript = null;
		stateChange = false;
	}


	void DropItem(float force, GameObject item, bool fromHolster = false, int ind = 0)
	{
		//set working variables
		stateChange = true;
		prevItem = item;
		prevItemScript = prevItem.GetComponent<Item>();


		fireTransformScript.ResetOffsets();
		lookTransformScript.ResetOffsets();
		bodyTransformScript.ResetOffsets();


		//unchild item from super hand 
		prevItem.transform.parent = null;

		if (!fromHolster)
		{
			//nullify its inventory slot
			inventory[inventoryIndex] = null;
		}
		else
		{
			inventory[ind] = null;
		}

		//deactivate rigidbody physics
		Rigidbody rigid = prevItem.GetComponent<Rigidbody>();
		rigid.isKinematic = false;
		rigid.useGravity = true;

		//unactivate item
		prevItemScript.active = false;


		//ammoPanel.SetActive(false);

		//slotTexts[inventoryIndex].text = (inventoryIndex + 1).ToString();


		//I want to compound the force with the player's own movement so it can fly farther when you run and jump and shit you know.
		//cuz that's how physics works anyways
		//so i'm going to make a new vector and add it in the addforce clause
		//but i'm going to need a class that does this for me because i'm going to reference player displacement in gun script too hahahahahaha

		Vector3 playerMovementMod = player.velocity * dropMod;


		//throw the fucker
		rigid.AddForce((fireTransform.forward * force) + playerMovementMod, ForceMode.Impulse);

		if (!fromHolster)
		{
			//keep your hands on it for a bit and then retract them to make it seem as if you are throwing it with your hands hahahahhahaha
			StartCoroutine(ThrowWithYourHands(0.2f));
		}


		//make sure that its trajectory is not going to be influenced by the current player so long as they are intersecting
		StartCoroutine(DontCollideWithDroppingItem(prevItemScript.collider));

		if (fromHolster)
		{
			//put item on back on Item Layer
			prevItem.layer = LayerMask.NameToLayer("Item");
			Transform[] childTransforms = prevItem.GetComponentsInChildren<Transform>();
			foreach (Transform trans in childTransforms)
			{
				if (trans.gameObject != rightHandTransform.gameObject &&
					trans.gameObject != leftHandTransform.gameObject)
				{
					if (System.Array.IndexOf(rightHandChildren, trans) == -1 &&
						System.Array.IndexOf(leftHandChildren, trans) == -1)
					{
						trans.gameObject.layer = LayerMask.NameToLayer("Item");
					}
				}
			}

			leftHandTransform.parent = transform.root;
			rightHandTransform.parent = transform.root;
		}
	}


	IEnumerator ThrowWithYourHands(float holdTime)
	{
		yield return new WaitForSeconds(holdTime);

		//put item on back on Item Layer
		prevItem.layer = LayerMask.NameToLayer("Item");
		Transform[] childTransforms = prevItem.GetComponentsInChildren<Transform>();
		foreach (Transform trans in childTransforms)
		{
			if (trans.gameObject != rightHandTransform.gameObject && trans.gameObject != leftHandTransform.gameObject)
			{
				if (System.Array.IndexOf(rightHandChildren, trans) == -1 &&
					System.Array.IndexOf(leftHandChildren, trans) == -1)
				{
					trans.gameObject.layer = LayerMask.NameToLayer("Item");
				}
			}
		}

		leftHandTransform.parent = transform.root;
		rightHandTransform.parent = transform.root;

		rightHandTransform.DOLocalMove(rightHandOriginPos, dropTime).SetEase(Ease.OutExpo);
		rightHandTransform.DOLocalRotate(rightHandOriginRot.eulerAngles, dropTime).SetEase(Ease.OutExpo);
		leftHandTransform.DOLocalMove(leftHandOriginPos, dropTime).SetEase(Ease.OutExpo);
		leftHandTransform.DOLocalRotate(leftHandOriginRot.eulerAngles, dropTime).SetEase(Ease.OutExpo).OnComplete(() =>
		{
			//nullify working variables
			prevItem = null;
			prevItemScript = null;
			stateChange = false;
		});
	}


	IEnumerator DontCollideWithDroppingItem(Collider itemCol)
	{
		Physics.IgnoreCollision(itemCol, playerCollider);

		//turn collider back on alright fine jeez
		itemCol.enabled = true;

		yield return new WaitUntil(() => itemCol.bounds.Intersects(playerCollider.bounds) == false);

		Physics.IgnoreCollision(itemCol, playerCollider, false);
	}


	void HolsterAndSwitchToItem(GameObject item, int slotToHolsterTo)
	{
		//set working variables
		stateChange = true;

		prevItem = inventory[inventoryIndex];
		prevItemScript = prevItem.GetComponent<Item>();

		nextItem = item;
		nextItemScript = nextItem.GetComponent<Item>();


		//find if right hand or if left hand should carry the gun over to the holster

		float distanceForRightHand = Vector3.Distance(rightHandTransform.position, holsters[slotToHolsterTo].position);
		float distanceForLeftHand = Vector3.Distance(leftHandTransform.position, holsters[slotToHolsterTo].position);


		bool useRightHand = distanceForRightHand < distanceForLeftHand;

		if (useRightHand)
		{
			rightHandTransform.parent = prevItem.transform;
			leftHandTransform.parent = handsTransform;
		}
		else
		{
			leftHandTransform.parent = prevItem.transform;
			rightHandTransform.parent = handsTransform;
		}

		//child item to target holster
		prevItem.transform.parent = holsters[slotToHolsterTo];

		//put it on the new inventory slot
		inventory[slotToHolsterTo] = prevItem;

		//slotTexts[inventoryIndex].text = (inventoryIndex + 1).ToString();
		//slotTexts[slotToHolsterTo].text = prevItemScript.itemName + "  " + (slotToHolsterTo + 1).ToString();


		//unactivate item
		prevItemScript.active = false;


		grabTime = Vector3.Distance(prevItem.transform.position, holsters[slotToHolsterTo].position) * grabTimeMod;
		grabTime = Mathf.Clamp(grabTime, grabTimeLower, grabTimeUpper);


		prevItem.transform.DOLocalMove(Vector3.zero, grabTime).SetEase(Ease.OutExpo);
		//LeanTween.rotateLocal(prevItem, Quaternion.identity.eulerAngles, grabTime).setEase(LeanTweenType.easeOutExpo);
		prevItem.transform.DOLocalRotateQuaternion(Quaternion.identity, grabTime).SetEase(Ease.OutExpo);


		//ammoPanel.SetActive(false);


		StartCoroutine(GrabItem(nextItem, !useRightHand, true));
	}


	IEnumerator SwitchToItem(int nextSlot)
	{
		//set working variables
		stateChange = true;

		bool fromHands = (inventory[inventoryIndex] == null);
		bool toHands = inventory[nextSlot] == null;


		fireTransformScript.ResetOffsets();
		lookTransformScript.ResetOffsets();
		bodyTransformScript.ResetOffsets();

		if (!fromHands)
		{
			prevItem = inventory[inventoryIndex];
			prevItemScript = prevItem.GetComponent<Item>();
		}

		if (!toHands)
		{
			nextItem = inventory[nextSlot];
			nextItemScript = nextItem.GetComponent<Item>();
		}


		//if you are just going from hands to hands, you literally just need to change the current inventory index and get the fuck out of here
		if (fromHands && toHands)
		{
			/*
			Vector2 temp = selectorPanelTrans.anchoredPosition;
			temp.y = 13 + 28 * nextSlot;
			selectorPanelTrans.anchoredPosition = temp;

			slotTexts[nextSlot].color = Color.white;
			slotTexts[inventoryIndex].color = Color.black;
			slotTexts[nextSlot].fontStyle = FontStyle.Bold;
			slotTexts[inventoryIndex].fontStyle = FontStyle.Normal;
			*/

			inventoryIndex = nextSlot;
			stateChange = false;
			yield break;
		}
		//of course, this means that from here on, either fromHands is true or toHands is true; they are mutually exclusive


		bool useRightHand;

		if (!fromHands)
		{
			//find if right hand or if left hand should carry the gun over to the holster
			float distanceForRightHand =
				Vector3.Distance(rightHandTransform.position, holsters[inventoryIndex].position);
			float distanceForLeftHand = Vector3.Distance(leftHandTransform.position, holsters[inventoryIndex].position);

			useRightHand = distanceForRightHand < distanceForLeftHand;
		}
		else
		{
			//find if right hand or if left hand should get the next item
			float distanceForRightHand = Vector3.Distance(rightHandTransform.position, holsters[nextSlot].position);
			float distanceForLeftHand = Vector3.Distance(leftHandTransform.position, holsters[nextSlot].position);

			useRightHand = distanceForRightHand < distanceForLeftHand;
		}

		//going from item to item or item to hands
		if (!fromHands)
		{
			if (useRightHand)
			{
				//child putting away hand to previtem so the hand can put it away
				rightHandTransform.parent = prevItem.transform;
			}
			else
			{
				leftHandTransform.parent = prevItem.transform;
			}

			//child item to target holster
			prevItem.transform.parent = holsters[inventoryIndex];

			//unactivate item
			prevItemScript.active = false;

			//set grab time
			grabTime = Vector3.Distance(prevItem.transform.position, holsters[inventoryIndex].position) * grabTimeMod;
			grabTime = Mathf.Clamp(grabTime, grabTimeLower, grabTimeUpper);

			//move and rotate gun to it's holster
			prevItem.transform.DOLocalMove(Vector3.zero, grabTime).SetEase(Ease.OutQuart);

			prevItem.transform.DOLocalRotateQuaternion(Quaternion.Euler(prevItemScript.localHoldRotation), grabTime)
				.SetEase(Ease.OutQuart);


			//don't need to get next holstered item if you are just going to end up with hands
			if (!toHands)
			{
				if (useRightHand)
				{
					//child non putting away hand to next item so it can move to the hand hold and prep to ready it
					leftHandTransform.parent = nextItem.transform;

					leftHandTransform.DOLocalMove(nextItemScript.leftHandHold.localPosition, grabTime)
						.SetEase(Ease.OutQuart);

					leftHandTransform.DOLocalRotate(nextItemScript.leftHandHold.localRotation.eulerAngles, grabTime)
						.SetEase(Ease.OutQuart);
				}
				else
				{
					//child non putting away hand to next item so it can move to the hand hold and prep to ready it
					rightHandTransform.parent = nextItem.transform;


					rightHandTransform.DOLocalMove(nextItemScript.rightHandHold.localPosition, grabTime)
						.SetEase(Ease.OutQuart);

					rightHandTransform.DOLocalRotate(nextItemScript.rightHandHold.localRotation.eulerAngles, grabTime)
						.SetEase(Ease.OutQuart);
				}
			}

			else
			{
				//you are going to hands so you need to move your non putting away hand back to its normal position and shit so it doesn't look weird 
				if (useRightHand)
				{
					leftHandTransform.parent = handsTransform.transform;


					leftHandTransform.DOLocalMove(leftHandOriginPos, dropTime)
						.SetEase(Ease.OutQuart);

					leftHandTransform.DOLocalRotate(leftHandOriginRot.eulerAngles, dropTime).SetEase(Ease.OutQuart);
				}
				else
				{
					//child non putting away hand to next item so it can move to the hand hold and prep to ready it
					rightHandTransform.parent = handsTransform.transform;

					rightHandTransform.DOLocalMove(rightHandOriginPos, dropTime)
						.SetEase(Ease.OutQuart);

					rightHandTransform.DOLocalRotate(rightHandOriginRot.eulerAngles, dropTime).SetEase(Ease.OutQuart);
				}
			}
		}

		else
		{
			//actually just move the closest hand to the next holstered item in preperation to ready it. you don't need both hands going to it like that just doesn't happen naturally duh hakan...


			if (useRightHand)
			{
				rightHandTransform.parent = nextItem.transform;

				grabTime = Vector3.Distance(rightHandTransform.position, nextItemScript.rightHandHold.position) *
						   grabTimeMod;
				grabTime = Mathf.Clamp(grabTime, grabTimeLower, grabTimeUpper);


				rightHandTransform.DOLocalMove(nextItemScript.rightHandHold.localPosition, grabTime)
					.SetEase(Ease.OutQuart);

				rightHandTransform.DOLocalRotate(nextItemScript.rightHandHold.localRotation.eulerAngles, grabTime)
					.SetEase(Ease.OutQuart);
			}
			else
			{
				leftHandTransform.parent = nextItem.transform;

				grabTime = Vector3.Distance(leftHandTransform.position, nextItemScript.leftHandHold.position) *
						   grabTimeMod;
				grabTime = Mathf.Clamp(grabTime, grabTimeLower, grabTimeUpper);


				leftHandTransform.DOLocalMove(nextItemScript.leftHandHold.localPosition, grabTime)
					.SetEase(Ease.OutQuart);

				leftHandTransform.DOLocalRotate(nextItemScript.leftHandHold.localRotation.eulerAngles, grabTime)
					.SetEase(Ease.OutQuart);
			}
		}

		/*
		ammoPanel.SetActive(false);

		selectorPanelImage.color = Color.clear;

		slotTexts[inventoryIndex].color = Color.black;
		slotTexts[inventoryIndex].fontStyle = FontStyle.Normal;
		*/

		//yeah we are done with the first phase dawg
		yield return new WaitForSeconds(grabTime);

		/*
		slotTexts[nextSlot].color = Color.white;
		slotTexts[nextSlot].fontStyle = FontStyle.Bold;

		selectorPanelImage.color = selectorPanelColor;


		Vector2 temp1 = selectorPanelTrans.anchoredPosition;
		temp1.y = 13 + 28 * nextSlot;
		selectorPanelTrans.anchoredPosition = temp1;
		*/


		if (nextItem != null && nextItemScript.type == ItemType.Gun)
		{
			//ammoPanel.SetActive(true);
			Gun gunScript = nextItem.GetComponent<Gun>();
			//ammoText.text = gunScript.Ammo.ToString();
			gunScript.inventoryScript = gameObject.GetComponent<Inventory>();
			gunScript.currentRecoil = 0;
			gunScript.xRecoilOffset = 0;
			gunScript.yRecoilOffset = 0;
		}


		if (!toHands)
		{
			//put new item on handtransform so you can ready it and shit
			nextItem.transform.parent = handsTransform;

			// //put item on Player Item Layer so it doesn't appear to clip through walls
			// nextItem.layer = LayerMask.NameToLayer("Player Item");
			//
			// //do this for all its children
			// Transform[] childTransforms = nextItem.GetComponentsInChildren<Transform>();
			// foreach (Transform trans in childTransforms)
			// {
			// 	trans.gameObject.layer = LayerMask.NameToLayer("Player Item");
			// }

			//the hands should already be on player item layer i think...


			//set grab time
			grabTime = Vector3.Distance(nextItem.transform.position,
						   handsTransform.TransformPoint(nextItemScript.localHoldPosition)) * grabTimeMod;
			grabTime = Mathf.Clamp(grabTime, grabTimeLower, grabTimeUpper);

			//move item to preferred hold position
			nextItem.transform.DOLocalMove(nextItemScript.localHoldPosition, grabTime).SetEase(Ease.OutQuart);


			//move item to preferred hold rotation
			nextItem.transform.DOLocalRotateQuaternion(Quaternion.Euler(nextItemScript.localHoldRotation), grabTime)
				.SetEase(Ease.OutQuart);


			if (!fromHands)
			{
				if (!useRightHand)
				{
					leftHandTransform.parent = nextItem.transform;


					leftHandTransform.DOLocalMove(nextItemScript.leftHandHold.localPosition, grabTime)
						.SetEase(Ease.OutQuart);

					leftHandTransform.DOLocalRotate(nextItemScript.leftHandHold.localRotation.eulerAngles, grabTime)
						.SetEase(Ease.OutQuart);
				}
				else
				{
					rightHandTransform.parent = nextItem.transform;


					rightHandTransform.DOLocalMove(nextItemScript.rightHandHold.localPosition, grabTime)
						.SetEase(Ease.OutQuart);

					rightHandTransform.DOLocalRotate(nextItemScript.rightHandHold.localRotation.eulerAngles, grabTime)
						.SetEase(Ease.OutQuart);
				}
			}
			else
			{
				if (useRightHand)
				{
					leftHandTransform.parent = nextItem.transform;


					leftHandTransform.DOLocalMove(nextItemScript.leftHandHold.localPosition, grabTime)
						.SetEase(Ease.OutQuart);

					leftHandTransform.DOLocalRotate(nextItemScript.leftHandHold.localRotation.eulerAngles, grabTime)
						.SetEase(Ease.OutQuart);
				}
				else
				{
					rightHandTransform.parent = nextItem.transform;


					rightHandTransform.DOLocalMove(nextItemScript.rightHandHold.localPosition, grabTime)
						.SetEase(Ease.OutQuart);

					rightHandTransform.DOLocalRotate(nextItemScript.rightHandHold.localRotation.eulerAngles, grabTime)
						.SetEase(Ease.OutQuart);
				}
			}
		}
		else
		{
			//if we are going to hands...
			//all you gotta do is send the putting away hand back to the normal orientation

			if (useRightHand)
			{
				//set grab time based on how far away the hand location is
				grabTime = Vector3.Distance(rightHandTransform.position,
							   handsTransform.TransformPoint(rightHandOriginPos)) * grabTimeMod;
				grabTime = Mathf.Clamp(grabTime, grabTimeLower, grabTimeUpper);


				//child putting away hand to hand transform so we can move it back to place
				rightHandTransform.parent = handsTransform.transform;


				rightHandTransform.DOLocalMove(rightHandOriginPos, dropTime)
					.SetEase(Ease.OutQuart);

				rightHandTransform.DOLocalRotate(rightHandOriginRot.eulerAngles, dropTime).SetEase(Ease.OutQuart);
			}
			else
			{
				//set grab time based on how far away the hand lockation is
				grabTime = Vector3.Distance(leftHandTransform.position,
							   handsTransform.TransformPoint(leftHandOriginPos)) * grabTimeMod;
				grabTime = Mathf.Clamp(grabTime, grabTimeLower, grabTimeUpper);

				leftHandTransform.parent = handsTransform.transform;

				leftHandTransform.DOLocalMove(leftHandOriginPos, dropTime).SetEase(Ease.OutExpo);

				leftHandTransform.DOLocalRotate(leftHandOriginRot.eulerAngles, dropTime).SetEase(Ease.OutExpo);
			}
		}


		yield return new WaitForSeconds(grabTime);


		//reactivate item
		if (nextItem)
			nextItemScript.active = true;

		//nullify working variables
		if (!fromHands)
		{
			prevItem = null;
			prevItemScript = null;
		}

		if (!toHands)
		{
			nextItem = null;
			nextItemScript = null;
		}

		stateChange = false;

		inventoryIndex = nextSlot;
	}

	/// <summary>
	/// this is called when your inventory is full and you want to pick up another item
	/// the item in the last slot of inventory is dropped from the holster
	/// and you pick up the new item from the ground
	/// the index switches to the last index of inventory (duh)
	/// </summary>
	void DropAndSwitchToItem(GameObject item)
	{
		if (inventoryIndex != 0)
		{
			print("K");

			DropItem(dropForce, inventory[0], true, 0);

			HolsterAndSwitchToItem(item, 0);
		}
		else
		{
			DropItem(dropForce, inventory[maxInventorySize - 1], true, maxInventorySize - 1);

			HolsterAndSwitchToItem(item, maxInventorySize - 1);
		}


		print("D");
	}

}