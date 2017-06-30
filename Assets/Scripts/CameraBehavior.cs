using UnityEngine;
using System.Collections;

public class CameraBehavior : MonoBehaviour {

	public GameObject target;
	public Vector3 cameraOffset;

	private float rotation;
	public float turnSpeed = 5.0f;

	void Start() {
		cameraOffset = target.transform.position - transform.position;
	}

	// LateUpdate so it's called after everything's Update is finished
	void LateUpdate() {
		var xx = Input.GetAxis("Mouse X");
		var yy = Input.GetAxis("Mouse Y");

		rotation += xx * turnSpeed * Time.deltaTime;

		Quaternion quat = Quaternion.AngleAxis(rotation, Vector3.up);
		transform.position = target.transform.position - (quat * cameraOffset);
		
		transform.LookAt(target.transform);

		//transform.Translate(new Vector3(xx, yy));
		
	}
}
