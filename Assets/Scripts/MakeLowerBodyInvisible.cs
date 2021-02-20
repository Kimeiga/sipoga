using UnityEngine;

public class MakeLowerBodyInvisible : MonoBehaviour
{
	/// <summary>
	/// This script runs when this player's camera is turned on.
	/// It makes this player's lower body invisible.
	/// It makes this player's hands (and item in a sec) render on top of things.
	/// </summary>
	
	private Player player;

	void Awake()
	{
		player = transform.root.GetComponent<Player>();
	}

	private void OnEnable()
	{
		player.MakeLowerBodyInvisible();
	}

	private void OnDisable()
	{
		player.MakeLowerBodyVisible();
	}

	void OnDestroy()
	{
		player.MakeLowerBodyVisible();
	}
}