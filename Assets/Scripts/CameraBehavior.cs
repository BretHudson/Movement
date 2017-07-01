using UnityEngine;
using System.Collections;

public class CameraBehavior : MonoBehaviour {

	public GameObject target;
	public Vector3 targetOffset;
	public Vector3 cameraOffset;
	private Camera cam;

	private Vector3 fakePos;

	public float pitch, yaw;
	public float turnSpeed = 5.0f;

	public LayerMask collisionLayer;

	private bool pauseMouseMovement = true;

	void Start() {
		yaw = -transform.eulerAngles.x * Mathf.Deg2Rad;
		cam = GetComponent<Camera>();
		fakePos = transform.position;
	}

	// LateUpdate so it's called after everything's Update is finished
	void LateUpdate() {
		var xx = Input.GetAxis("Mouse X");
		var yy = Input.GetAxis("Mouse Y");

		if (Input.GetKeyDown(KeyCode.P)) {
			pauseMouseMovement = !pauseMouseMovement;
		}

		if (!pauseMouseMovement) {
			pitch += xx * turnSpeed * Time.deltaTime;
			yaw += yy * turnSpeed * Time.deltaTime;
			yaw = Mathf.Clamp(yaw, -0.65f, 0.8f);
		}

		Vector3 targetPos = target.transform.position + targetOffset;

		//Quaternion quat = Quaternion.AngleAxis(pitch, Vector3.up);
		Quaternion quat = Quaternion.EulerAngles(-yaw, pitch, 0);
		Vector3 newPos = targetPos - (quat * cameraOffset);

		// Wall collision/clipping/whatever
		RaycastHit wallHit;
		Vector3 rayDir = newPos - targetPos;
		Debug.DrawRay(newPos, rayDir, Color.magenta);
		Ray ray = new Ray(targetPos, rayDir.normalized);
		if (Physics.Raycast(ray, out wallHit, rayDir.magnitude, collisionLayer)) {
			newPos = new Vector3(wallHit.point.x, newPos.y, wallHit.point.z);
		}

		SetPosition(newPos, targetPos);

		/*if (collidingWithWall) {
			transform.position += transform.forward * 0.5f;
		}*/

		// Old wall clipping code that didn't work :( It was kind of complex, so I decided perhaps it was best to find a simpler alternative
		/*Vector3 clipDir = new Vector3(0, 0, cam.nearClipPlane);
		float halfFoV = cam.fieldOfView * Mathf.Deg2Rad * 0.5f;
		clipDir.x = Mathf.Tan(halfFoV) * clipDir.z;
		clipDir.y = clipDir.x / cam.aspect;

		clipDir.Normalize();
		clipDir *= (targetPos - transform.position).magnitude;

		float startDistance = 999999.0f;
		float distance = startDistance;
		for (int clipCheckIndex = 0; clipCheckIndex < 4; ++clipCheckIndex) {
			// Flip either the x or y coords
			if (clipCheckIndex % 2 == 0)
				clipDir.y *= -1.0f;
			else
				clipDir.x *= -1.0f;

			Vector3 clipCheck = transform.rotation * clipDir;
			Ray ray = new Ray(transform.position, clipCheck);
			Debug.DrawRay(transform.position, clipCheck, Color.magenta);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, clipCheck.magnitude, collisionLayer)) {
				distance = Mathf.Min(distance, hit.distance);
			}
		}

		if (distance != startDistance) {
			// We can move it forward
			transform.position += transform.forward * distance;
		}*/
	}

	private Vector3 smoothVel;
	private void SetPosition(Vector3 newPos, Vector3 targetPos) {
		transform.position = Vector3.SmoothDamp(transform.position, newPos, ref smoothVel, 0.1f);
		transform.LookAt(targetPos);
	}
}
