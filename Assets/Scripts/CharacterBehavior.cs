using UnityEngine;
using System;
using System.Collections;

public class CharacterBehavior : MonoBehaviour {

	private GameObject camera;

	public float moveSpeed = 3.0f;

	private float targetRotation = 0.0f;
	private float curRotation = 0.0f;
	private float turnSmoothTime = 0.2f;
	private float turnSmoothVelocity;
	private float oneOver180 = 1.0f / 180.0f;
	private float maxTurnAngle = 35.0f;

	private float jumpScaleTimer = 1.0f;
	public float jumpScaleSpeed = 1.0f;

	void Start() {
		camera = Camera.main.gameObject;
	}

	void Update() {
		// Get input and do an elementary way of normalizing diagonal movement
		Vector3 inputDir = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
		if (inputDir.magnitude > 1)
			inputDir.Normalize();

		// Go ahead and move the player
		Vector3 moveDir = InputToMovementVector(inputDir);

		UpdateAngle(moveDir);

		// Now let's move forward!
		transform.Translate(Vector3.forward * moveSpeed * inputDir.magnitude * Time.deltaTime);

		if (Input.GetButtonDown("Jump")) {
			// Make sure we're on the ground
			if (IsGrounded()) {
				jumpScaleTimer = 0.0f;
				Debug.Log("JUMP");
			}
			// TODO(bret): Do jump stuff
		}

		if (jumpScaleTimer < 1.0f) {
			Debug.Log(jumpScaleTimer);
			Vector3 scale = transform.localScale;
			float sinValue = jumpScaleTimer * Mathf.PI;
			scale.y = 1.0f - 0.2f * Mathf.Sin(sinValue * 2.0f) * Mathf.Sin(sinValue);
			transform.localScale = scale;
			jumpScaleTimer += Time.deltaTime * jumpScaleSpeed;
		}
	}

	public float angleLerpAmount = 50.0f;

	private void UpdateAngle(Vector3 moveDir) {
		if (moveDir.magnitude > 0) {
			// Get the target and new rotations
			targetRotation = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
		}

		// Get the angle between and adjust the target rotation if need be
		float angleBetween = Mathf.DeltaAngle(curRotation, targetRotation);
		angleBetween = Mathf.Clamp(angleBetween, -maxTurnAngle, maxTurnAngle);

		float newRotation = Mathf.SmoothDampAngle(curRotation, targetRotation, ref turnSmoothVelocity, turnSmoothTime);

		// Set the new rotation
		transform.rotation = Quaternion.AngleAxis(newRotation, Vector3.up);

		// Pop it! Tilt it!
		float lean = -(angleBetween * oneOver180) * 50.0f;
		Vector3 euler = transform.eulerAngles;
		euler.z = Mathf.Lerp(euler.z, lean, angleLerpAmount * Time.deltaTime);
		transform.eulerAngles = euler;

		// Update variables
		curRotation = newRotation;
	}

	private Vector3 InputToMovementVector(Vector3 input) {
		Matrix4x4 cameraMatrix = camera.transform.localToWorldMatrix;
		cameraMatrix.m01 = cameraMatrix.m10 = cameraMatrix.m12 = cameraMatrix.m21 = 0;
		cameraMatrix.m11 = 1;
		return cameraMatrix.MultiplyVector(input);
	}

	private float Ease(float t) {
		return t * t;
		//return -t * (t - 2);
	}

	private bool IsGrounded() {
		return Physics.Raycast(transform.position, -Vector3.up, 1.0f);
	}
}
