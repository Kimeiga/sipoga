using UnityEngine;
using UnityEngine.Rendering;
using Vector3 = UnityEngine.Vector3;

[System.Serializable]
public class PlayerBodyParts
{
	public Transform head;
	public Transform legsJoint;
	public Transform leftLegJoint;
	public Transform rightLegJoint;
	public Transform rightHandTransform;
	public Transform leftHandTransform;
	public Transform handsTransform;
	
	// need the hands themselves for inventory script i think 
	public GameObject rightHand;
	public GameObject leftHand;

	// body part component references
	// these have to be public because they need to be set before MakeLowerBodyInvisible is called
	public Renderer torsoRenderer;
	public Renderer leftLegRenderer;
	public Renderer rightLegRenderer;
	public Renderer[] bodyTextRenderers;
	
	public MouseLook bodyMouseLook;
	public MouseLook headMouseLook;
	public MouseLook lookMouseLook;

	public Collider collider;

	public SimpleInventory inventory;
}

public class Player : MonoBehaviour
{
	public PlayerBodyParts body;
	
	public bool isU; // true = u, false = t

	// 1 = U, 2 = T, 3 = S, 4 = R
	// but we use a new number for each player in a free for all, or each squad in a battle royale game mode
	public int teamNumber;

	public bool isAI = false; // false = player, true = AI 

	public float health;
	public float maxHealth = 100;

	public float armor;
	public float maxArmor = 100;

	// if this player (AI or Human) is the one that currently has their camera turned on (and thus lower body should be invisible,
	// and their gun and hands should be drawing over the top of the screen), then this is true, else false
	// This is set in the OnEnable in MakeLowerBodyInvisible on the player camera.
	internal bool currentPlayerRendering = false;

	// both AI and players need velocity, best place is to put it here
	internal Vector3 velocity = Vector3.zero;
	private Vector3 lastPosition;


	// Start is called before the first frame update

	void Awake()
	{
		health = maxHealth;
		armor = 0;

		lastPosition = transform.position;
	}

	private void FixedUpdate()
	{
		Vector3 transformPosition = transform.position;
		velocity = transformPosition - lastPosition;
		lastPosition = transformPosition;

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
		body.torsoRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
		body.leftLegRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
		body.rightLegRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

		foreach (Renderer bodyTextRenderer in body.bodyTextRenderers)
		{
			bodyTextRenderer.enabled = false;
		}
		
		// make hands draw on top of everything
		body.rightHand.layer = LayerMask.NameToLayer("Current Player Hands");
		body.leftHand.layer = LayerMask.NameToLayer("Current Player Hands");
		
		// set the bool so that the current item is rendered on top when a new item is gotten
		currentPlayerRendering = true;
		
		// if there is an item currently held, then make it render on top
		body.inventory.MakeCurrentItemRenderOnTop();
	}

	public void MakeLowerBodyVisible()
	{
		body.torsoRenderer.shadowCastingMode = ShadowCastingMode.On;
		body.leftLegRenderer.shadowCastingMode = ShadowCastingMode.On;
		body.rightLegRenderer.shadowCastingMode = ShadowCastingMode.On;

		foreach (Renderer bodyTextRenderer in body.bodyTextRenderers)
		{
			bodyTextRenderer.enabled = true;
		}
		
		// make hands normal again
		body.rightHand.layer = LayerMask.NameToLayer("Poga");
		body.leftHand.layer = LayerMask.NameToLayer("Poga");
		
		// set the bool so that the current item not is rendered on top when a new item is gotten
		currentPlayerRendering = false;
		
		// if there is an item currently held, then make it render not on top
		body.inventory.MakeCurrentItemRenderNormally();
	}

	public void MakeHandsRenderOnTop()
	{
		// //you should do this to the hands so that it looks ok:
		// rightHandTransform.gameObject.layer = LayerMask.NameToLayer("Player ");
		// foreach (Transform trans in rightHandChildren)
		// {
		// 	trans.gameObject.layer = LayerMask.NameToLayer("Body Part Alive");
		// }
		//
		// //store right/left hand children to make the layer switching faster
		// rightHandChildren = rightHandTransform.GetComponentsInChildren<Transform>();
	}

	public void MakeHandsNotRenderOnTop()
	{
		
	}
}