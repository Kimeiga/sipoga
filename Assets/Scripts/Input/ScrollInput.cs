using UnityEngine;

public class ScrollInput : MonoBehaviour
{
	public static bool mouseScrollUpButton;
	public static bool mouseScrollDownButton;

	// Update is called once per frame
	void Update()
	{
		mouseScrollUpButton = Input.GetAxis("Mouse ScrollWheel") > 0;

		mouseScrollDownButton = Input.GetAxis("Mouse ScrollWheel") < 0;
	}
}