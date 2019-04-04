using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrbit : MonoBehaviour {
    public float distance = 10f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;
    public float yMin = 15f;
    public float yMax = 80f;
    private float x = 0.0f;
    private float y = 0.0f;
    // Use this for initialization
    void Start () {
        Vector3 euler = transform.eulerAngles;
        x = euler.y;
        y = euler.x;
	}
	
	// Update is called once per frame
	void LateUpdate () {
        if (Input.GetMouseButton(1))
        {
            Cursor.visible = false;
            // Get input x and y offsets
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            // Offset rotation with mouse X and Y offset
            x += mouseX * xSpeed * Time.deltaTime;
            y -= mouseY * ySpeed * Time.deltaTime;
            // clamp the y between min and mas limits
            y = Mathf.Clamp(y, yMin, yMax);
        }
        else
        {
            Cursor.visible = true;
        }
        // Update transform
        transform.rotation = Quaternion.Euler(y, x, 0);
        transform.position = -transform.forward * distance;
	}
}
