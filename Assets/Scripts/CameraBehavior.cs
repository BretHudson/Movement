using UnityEngine;
using System.Collections;

public class CameraBehavior : MonoBehaviour {

	public GameObject target;
	public Vector3 cameraOffset;

	private float pitch, yaw;
	public float turnSpeed = 5.0f;

	void Start() {
		cameraOffset = target.transform.position - transform.position;
	}

	// LateUpdate so it's called after everything's Update is finished
	void LateUpdate() {
		var xx = Input.GetAxis("Mouse X");
		var yy = Input.GetAxis("Mouse Y");

		pitch += xx * turnSpeed * Time.deltaTime;
		yaw += yy * turnSpeed * Time.deltaTime;
		yaw = Mathf.Clamp(yaw, -45, 80);

		//Quaternion quat = Quaternion.AngleAxis(pitch, Vector3.up);
		Quaternion quat = Quaternion.EulerAngles(-yaw, pitch, 0);
		transform.position = target.transform.position - (quat * cameraOffset);
		
		transform.LookAt(target.transform);

		//transform.Translate(new Vector3(xx, yy));
		
	}
}
