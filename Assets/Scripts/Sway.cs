using UnityEngine;
using System.Collections;

public class Sway : MonoBehaviour
{
    private float xSway;
    private float ySway;
    public float moveAmount = 1;
    public float moveSpeed = 2;

    private Vector3 basePosition;

    private Vector3 swayOffset;

    public float zOffset;


    // Use this for initialization
    void Start()
    {
        basePosition = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        xSway = Input.GetAxis("Mouse X") * 0.025f * moveAmount;
        ySway = Input.GetAxis("Mouse Y") * 0.025f * moveAmount;

        Vector3 swayOffsetTarget = new Vector3(xSway, ySway, 0);
        swayOffset = Vector3.Lerp(swayOffset, swayOffsetTarget, Time.deltaTime * moveSpeed);


        Vector3 offset = new Vector3(0, 0, zOffset);


        transform.localPosition = basePosition + swayOffset + offset;
    }
}