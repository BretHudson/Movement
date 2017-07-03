using UnityEngine;
using System.Collections;

public class CameraBehavior : MonoBehaviour {

	public GameObject goal;
	public GameObject target;
	public Vector3 targetOffset;
	public Vector3 cameraOffset;
	private Camera cam;

	private Vector3 fakePos;

	public float pitch, yaw;
	public float turnSpeed = 5.0f;

	public LayerMask collisionLayer;

	public bool pauseMouseMovement = true;

	void Start() {
		yaw = -transform.eulerAngles.x * Mathf.Deg2Rad;
		cam = GetComponent<Camera>();
		fakePos = transform.position;
	}

	// LateUpdate so it's called after everything's Update is finished
	private float lookAtGoal = 0.0f;
	void LateUpdate() {
		var xx = Input.GetAxis("Mouse X");
		var yy = Input.GetAxis("Mouse Y");

		// Input
		lookAtGoal += (Input.GetKey(KeyCode.G) ? 1.0f : -1.0f) * Time.deltaTime * 1.5f;
		lookAtGoal = Mathf.Clamp01(lookAtGoal);

		if (Input.GetKeyDown(KeyCode.P)) {
			pauseMouseMovement = !pauseMouseMovement;
		}

		if (!pauseMouseMovement) {
			pitch += xx * turnSpeed * Time.deltaTime;
			yaw += yy * turnSpeed * Time.deltaTime;
			yaw = Mathf.Clamp(yaw, -0.65f, 0.5f);
		}

		// Rotate and move the cameera
		Vector3 targetPos = target.transform.position + targetOffset;

		if (lookAtGoal > 0.0f) {
			SetPosition(transform.position, targetPos);
		} else {
			Quaternion quat = Quaternion.EulerAngles(-yaw, pitch, 0);
			Vector3 newPos = targetPos - (quat * cameraOffset);

			// Wall collision/clipping/whatever
			RaycastHit wallHit;
			Vector3 rayDir = newPos - targetPos;
			Debug.DrawRay(targetPos, rayDir, Color.magenta);
			Ray ray = new Ray(targetPos, rayDir.normalized);
			if (Physics.Raycast(ray, out wallHit, rayDir.magnitude, collisionLayer)) {
				newPos = new Vector3(wallHit.point.x, newPos.y, wallHit.point.z);
			}

			SetPosition(newPos, targetPos);
		}
	}

	private Vector3 smoothVel;
	private void SetPosition(Vector3 newPos, Vector3 targetPos) {
		transform.position = Vector3.SmoothDamp(transform.position, newPos, ref smoothVel, 0.1f);

		Quaternion targetRotation = Quaternion.LookRotation(targetPos - transform.position);
		Quaternion goalRotation = Quaternion.LookRotation(goal.transform.position - transform.position);

		transform.rotation = Quaternion.Slerp(targetRotation, goalRotation, EaseInOut(lookAtGoal));
	}

	private float EaseInOut(float t) {
		return t <= .5 ? t * t * 2 : 1 - (--t) * t * 2;
	}

	public void AtGoal() {
		Debug.Log("At goal holla");
		pitch = Mathf.PI;
		yaw = -0.4f;
		pauseMouseMovement = true;
	}

	public void GameReset() {
		pauseMouseMovement = false;
	}
}
