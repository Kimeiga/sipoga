using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

public class SimpleInventory : MonoBehaviour
{
	/// <summary>
	/// Starting fresh from the inventory script so we can slowly work on getting it working with AI
	/// all we want to do now is pick up an item when we touch it with oncontrollercollider hit
	/// </summary>

	// references
	private Player playerScript;

	private Transform handsTransform;
	private Transform rightHandTransform;
	private Transform leftHandTransform;


	public GameObject CurrentItem
	{
		get => currentItem;
		set
		{
			currentItem = value;
			onCurrentItemChanged?.Invoke(currentItem);
		}
	}

	private GameObject currentItem;

	private Item currentItemScript;

	public float grabTimeLower = 0.5f;
	public float grabTimeUpper = 1.5f;

	public float dropForceMod = 0.5f;

	public float handResetTime = 0.3f;

	// original positions to revert to after dropping an item.
	private Vector3 rightHandOriginPos;
	private Quaternion rightHandOriginRot;
	private Vector3 leftHandOriginPos;
	private Quaternion leftHandOriginRot;
	private Transform[] leftHandChildren;
	private Transform[] rightHandChildren;

	public Action<GameObject> onCurrentItemChanged;

	// Start is called before the first frame update
	void Start()
	{
		playerScript = gameObject.GetComponent<Player>();
		rightHandTransform = playerScript.body.rightHandTransform;
		leftHandTransform = playerScript.body.leftHandTransform;
		handsTransform = playerScript.body.handsTransform;

		//set hand origins
		rightHandOriginPos = rightHandTransform.localPosition;
		rightHandOriginRot = rightHandTransform.localRotation;
		leftHandOriginPos = leftHandTransform.localPosition;
		leftHandOriginRot = leftHandTransform.localRotation;

		//store right/left hand children to make the layer switching faster
		rightHandChildren = rightHandTransform.GetComponentsInChildren<Transform>();
		leftHandChildren = leftHandTransform.GetComponentsInChildren<Transform>();
	}

	// Update is called once per frame
	void Update()
	{
		// need to modify so AI can use it too, see the jason weimann video on this
		if (Input.GetButtonDown("Drop") && CurrentItem)
		{
			DropItem(CurrentItem, dropForceMod);
		}
	}

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		// if we hit an item
		if (hit.gameObject.layer == LayerMask.NameToLayer("Item"))
		{
			// if we don't have anything in our inventory, pick up that item automatically
			if (CurrentItem == null)
			{
				PickUpItem(hit.gameObject);
			}
		}
	}

	private void PickUpItem(GameObject item)
	{
		// set the inventory equal to that item.
		CurrentItem = item;

		// we need to deactivate itemRigibodybody physics and all sorts of things
		// previously we were calling this code from the inventory.
		// but I actually think it would make things cleaner if we did it 
		// on the item script itself, and just call that method from here.

		// since we are picking up from holding nothing, i can do this
		currentItemScript = item.GetComponent<Item>();

		currentItemScript.PrepareForGrab();

		// if the item is at your feet and you collide with it, the distance between is and your super hand is around 1.5
		// but you can grab an item on a shelf that is close to your face, say like 0.5f, but of course you will have to 
		// move your hands up to get to it. So maybe 1 meter is the right lower bound

		// then if you have a grab range of around 2.5 meters (impossible in real life, but this would allow you to grab 
		// an item that is 2 meters away from your feet on the ground). Then 2.5 should be the upper bound

		// prevent repeated property access
		Vector3 handsTransformPosition = handsTransform.position;
		Vector3 itemTransformPosition = item.transform.position;

		//set grab time to something proportional to the distance to the next item
		float grabTime = Mathf.Lerp(grabTimeLower, grabTimeUpper,
			(Vector3.Distance(handsTransformPosition, itemTransformPosition) - 1) / 2.5f);

		// Instead it should be one pass where the gun moves to its relative holding position and your hands move and catch
		// their respective hand holds on the gun halfway, and then child themselves to the hand holds like normal. This way
		// there is immediate feedback for picking up the gun, and it looks like you are picking it up too.
		item.transform.parent = handsTransform;

		// move item to preferred hold position
		item.transform.DOLocalMove(currentItemScript.localHoldPosition, grabTime).SetEase(Ease.OutQuart);

		// rotate item to preferred hold rotation
		item.transform.DOLocalRotateQuaternion(Quaternion.Euler(currentItemScript.localHoldRotation), grabTime)
			.SetEase(Ease.OutQuart);

		// move hands to their hand holds in half the time
		rightHandTransform.parent = item.transform;
		leftHandTransform.parent = item.transform;

		//rotate left hand to left hand hold on item
		leftHandTransform.DOLocalRotate(
			Quaternion.Euler(currentItemScript.leftHandHold.localEulerAngles)
				.eulerAngles, grabTime / 2).SetEase(Ease.OutQuart);

		// move left hand to left hand hold on item
		leftHandTransform.DOLocalMove(currentItemScript.leftHandHold.localPosition, grabTime / 2)
			.SetEase(Ease.OutQuart);


		// rotate right hand to right hand hold on item
		rightHandTransform.DOLocalRotate(
			Quaternion.Euler(currentItemScript.rightHandHold.localEulerAngles)
				.eulerAngles, grabTime / 2).SetEase(Ease.OutQuart);

		// move right hand to right hand hold on item
		rightHandTransform.DOLocalMove(currentItemScript.rightHandHold.localPosition, grabTime / 2)
			.SetEase(Ease.OutQuart);

		// if this is the rendering player, put this item on top
		if (playerScript.currentPlayerRendering)
		{
			ChangeItemRendering(CurrentItem, "Current Player Item");
		}
	}

	private void DropItem(GameObject item, float force)
	{
		// unchild item from super hand
		item.transform.parent = null;

		currentItemScript.PrepareForRelease();

		Vector3 playerMovementMod = playerScript.velocity * dropForceMod;

		Rigidbody itemRigibody = currentItemScript.rigidbody;


		//throw the fucker
		itemRigibody.AddForce((playerScript.body.head.forward * force) + playerMovementMod, ForceMode.Impulse);

		//keep your hands on it for a bit and then retract them to make it seem as if you are throwing it with your hands hahahahhahaha
		StartCoroutine(ThrowWithYourHands(CurrentItem, 0.2f));


		//make sure that its trajectory is not going to be influenced by the current player so long as they are intersecting
		StartCoroutine(DontCollideWithDroppingItem(currentItemScript.collider));

		CurrentItem = null;
		currentItemScript = null;
	}

	IEnumerator DontCollideWithDroppingItem(Collider itemCol)
	{
		Physics.IgnoreCollision(itemCol, playerScript.body.collider);

		//turn collider back on alright fine jeez
		itemCol.enabled = true;

		yield return new WaitUntil(() => itemCol.bounds.Intersects(playerScript.body.collider.bounds) == false);

		Physics.IgnoreCollision(itemCol, playerScript.body.collider, false);
	}


	IEnumerator ThrowWithYourHands(GameObject item, float holdTime)
	{
		yield return new WaitForSeconds(holdTime);

		// put item on back on Item Laygamer after releasing it
		// regardless of whether this is the rendering player or not
		ChangeItemRendering(item, "Item");

		var root = transform.root;
		leftHandTransform.parent = root;
		rightHandTransform.parent = root;

		rightHandTransform.DOLocalMove(rightHandOriginPos, handResetTime).SetEase(Ease.OutExpo);
		rightHandTransform.DOLocalRotate(rightHandOriginRot.eulerAngles, handResetTime).SetEase(Ease.OutExpo);
		leftHandTransform.DOLocalMove(leftHandOriginPos, handResetTime).SetEase(Ease.OutExpo);
		leftHandTransform.DOLocalRotate(leftHandOriginRot.eulerAngles, handResetTime).SetEase(Ease.OutExpo);
	}

	public void MakeCurrentItemRenderOnTop()
	{
		if (CurrentItem)
		{
			ChangeItemRendering(CurrentItem, "Current Player Item");
		}
	}

	public void MakeCurrentItemRenderNormally()
	{
		if (CurrentItem)
		{
			ChangeItemRendering(CurrentItem, "Item");
		}
	}

	private void ChangeItemRendering(GameObject item, string layer)
	{
		//put item on back on Item Layer
		item.layer = LayerMask.NameToLayer(layer);
		Transform[] childTransforms = item.GetComponentsInChildren<Transform>();
		foreach (Transform trans in childTransforms)
		{
			if (trans.gameObject != rightHandTransform.gameObject && trans.gameObject != leftHandTransform.gameObject)
			{
				if (System.Array.IndexOf(rightHandChildren, trans) == -1 &&
				    System.Array.IndexOf(leftHandChildren, trans) == -1)
				{
					trans.gameObject.layer = LayerMask.NameToLayer(layer);
				}
			}
		}
	}
}