using UnityEngine;
using System;
using System.Collections;

public class CharacterBehavior : MonoBehaviour {

	private GameObject camera;

	public LayerMask ground;

	public Vector3 SlopeRaycastOffset;

	public Rigidbody rigidbody;
	public float gravity = 12.0f;
	public float jumpHeight = 1.0f;
	public float jumpDelay = 0.5f;
	private float jumpDelayTimer = 1.0f;
	private float velocityY = 0.0f;
	public float moveSpeed = 5.0f;

	private float targetRotation = 0.0f;
	private float curRotation = 0.0f;
	private float turnSmoothTime = 0.2f;
	private float turnSmoothVelocity;
	private float oneOver180 = 1.0f / 180.0f;
	private float maxTurnAngle = 35.0f;

	private float jumpScaleTimer = 1.0f;
	public float jumpScaleSpeed = 1.0f;

	// Allow the player to hit jump a tenth of a second before they hit the ground
	private float inputBufferTime = 0.1f;
	private float jumpInputBuffer = 0.0f;

	void Start() {
		camera = Camera.main.gameObject;
	}

	void Update() {
		// Get input and do an elementary way of normalizing diagonal movement
		Vector3 inputDir = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
		if (inputDir.magnitude > 1)
			inputDir.Normalize();

		Move(inputDir);

		// Squish squash
		if (jumpScaleTimer < 1.0f) {
			float sinValue = jumpScaleTimer * Mathf.PI;
			float scaleY = 1.0f - 0.2f * Mathf.Sin(sinValue * 2.0f) * Mathf.Sin(sinValue);
			transform.SetLocalScaleY(scaleY);
			jumpScaleTimer += Time.deltaTime * jumpScaleSpeed;
		} else {
			transform.SetLocalScaleY(1.0f);
		}
	}

	void FixedUpdate() {
		//
	}

	private void Move(Vector3 input) {
		Vector3 moveDir = InputToMovementVector(input);

		UpdateAngle(moveDir);

		// Apply gravity
		velocityY -= gravity * Time.deltaTime;
		if (IsGrounded())
			velocityY = 0.0f;

		// Get input for jumping
		if (Input.GetButtonDown("Jump")) {
			jumpInputBuffer = inputBufferTime;
		}

		// If the player has pressed jump, is it okay to do so?
		if (jumpInputBuffer > 0.0f) {
			// Make sure we're on the ground
			if ((IsGrounded()) && (jumpDelayTimer > jumpDelay)) {
				// Set up the timers for scaling and the actual jump
				jumpScaleTimer = 0.0f;
				jumpDelayTimer = jumpDelay;

				jumpInputBuffer = 0.0f;
			}

			jumpInputBuffer -= Time.deltaTime;
		}

		if (jumpDelayTimer <= jumpDelay) {
			if (jumpDelayTimer <= 0.0f) {
				velocityY = jumpHeight;
				jumpDelayTimer = jumpDelay + 1.0f;
			}
			jumpDelayTimer -= Time.deltaTime;
		}

		// Check for slopes bruh
		RaycastHit hit;
		Vector3 slopeOffset = transform.localToWorldMatrix.MultiplyVector(SlopeRaycastOffset);
		Ray ray = new Ray(transform.position + slopeOffset, Vector3.down);
		Debug.DrawRay(transform.position, Vector3.down);
		Quaternion rotation = transform.rotation;
		if (Physics.Raycast(ray, out hit)) {
			Vector3 newUp;
			if (Mathf.Abs(Vector3.Dot(hit.normal, Vector3.forward)) < 0.9f) // If it's a slope
				newUp = hit.normal;
			else
				newUp = Vector3.up;

			transform.rotation = Quaternion.FromToRotation(transform.up, newUp) * transform.rotation;

			Debug.DrawRay(transform.position + transform.up, transform.up * 2.0f, Color.green);
		}

		rigidbody.velocity = transform.forward * moveSpeed * input.magnitude + Vector3.up * velocityY;

		transform.rotation = rotation;
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
		return Physics.Raycast(transform.position, Vector3.down, 0.1f, ground);
	}
}
