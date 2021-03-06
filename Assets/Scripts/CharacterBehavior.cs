﻿using UnityEngine;
using System;
using System.Collections;

public class CharacterBehavior : MonoBehaviour {

	public bool madeItToGoal = false;

	private GameObject camera;

	public LayerMask ground;

	private Vector3 startPos;

	[System.Serializable]
	public class RaycastInfo {
		public Vector3 slopeRaycastOffset;
		//public int numBorderRaycasts = 4;
		//public float raycastDistanceFromCenter = 0.45f;
	}

	[System.Serializable]
	public class MovementStats {
		public float gravity = 30.0f;
		public float moveSpeed = 12.0f;
		public float jumpHeight = 14.0f;
		public float jumpDelay = 0.23f;
		[HideInInspector]
		public float jumpDelayTimer = 1.0f;
		[HideInInspector]
		public float velocityY = 0.0f;
	}

	// NOTE(bret): Even though these are now a bit more code to write, while using Unity it certainly makes it easier to sort through variables in the inspector
	public RaycastInfo raycastInfo;
	public MovementStats movementStats;
	private bool isGrounded = false;

	public Rigidbody rigidbody;
	public CapsuleCollider collider;

	private GameObject goal;

	private float targetRotation = 0.0f;
	private float curRotation = 0.0f;
	private float turnSmoothTime = 0.2f;
	private float turnSmoothVelocity;
	private float oneOver180 = 1.0f / 180.0f;
	private float maxTurnAngle = 35.0f;

	private float scaleY = 1.0f;
	private float jumpScaleTimer = 1.0f;
	public float jumpScaleSpeed = 2.5f;

	private Vector3 sidedashMovement = Vector3.zero;

	// Allow the player to hit jump a tenth of a second before they hit the ground
	private float inputBufferTime = 0.1f;
	private float jumpInputBuffer = 0.0f;

	public float angleLerpAmount = 70.0f;

	void Start() {
		camera = Camera.main.gameObject;
		startPos = transform.position;
	}

	void FixedUpdate() {
		// Get input and do an elementary way of normalizing diagonal movement
		Vector3 inputDir = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
		if (inputDir.magnitude > 1)
			inputDir.Normalize();

		// If we're looking at the goal, please don't move
		if (Input.GetKey(KeyCode.G))
			inputDir = Vector3.zero;

		if (Input.GetKeyDown(KeyCode.Q))
			DoSidedash(1);
		if (Input.GetKeyDown(KeyCode.E))
			DoSidedash(-1);

		if (madeItToGoal) {
			UpdateAngle(goal.transform.forward);
			Vector3 vel = rigidbody.velocity;
			vel.x = vel.z = 0.0f;
			rigidbody.velocity = vel;
			// Infinitely jump, ha!
			RegisterJumpPressed();
			Move(Vector3.zero);
		} else {
			Move(inputDir);
		}

		if ((transform.position.y < -40.0f) || (Input.GetKeyDown(KeyCode.R)))
			ResetToStart();

		// Squish squash
		float scaleXZ = 1.0f / scaleY;
		transform.SetLocalScaleX(scaleXZ);
		transform.SetLocalScaleY(scaleY);
		transform.SetLocalScaleZ(scaleXZ);
	}

	private void DoSidedash(int dir) {
		sidedashMovement = -transform.right * 20.0f * (float)dir;
	}

	private void ResetToStart() {
		rigidbody.velocity = Vector3.zero;
		transform.position = startPos;
		transform.rotation = Quaternion.AngleAxis(0, Vector3.up);
		scaleY = 1.0f;
		madeItToGoal = false;
		camera.GetComponent<CameraBehavior>().GameReset();
	}

	private IEnumerator DoScaleStuff(float scaleSpeed, float amount) {
		float timer = 0.0f;
		while (timer < 1.0f) {
			float sinValue = timer * Mathf.PI;
			scaleY = 1.0f - amount * Mathf.Sin(sinValue * 2.0f) * Mathf.Sin(sinValue);
			timer += Time.deltaTime * scaleSpeed;
			yield return 0;
		}
	}

	// Taken from Kyle Pulver's Otter2D Util class
	public float ScaleClamp(float value, float min, float max, float min2, float max2) {
		value = min2 + ((value - min) / (max - min)) * (max2 - min2);
		if (max2 > min2) {
			value = value < max2 ? value : max2;
			return value > min2 ? value : min2;
		}
		value = value < min2 ? value : min2;
		return value > max2 ? value : max2;
	}

	public Renderer cylinder;
	private void Move(Vector3 inputDir) {
		Vector3 moveDir = InputToMovementVector(inputDir);

		UpdateAngle(moveDir);

		// Apply gravity
		movementStats.velocityY -= movementStats.gravity * Time.deltaTime;
		if (isGrounded) {
			float velY = Mathf.Abs(movementStats.velocityY);
			if (velY > 5.0f) {
				float amount = ScaleClamp(velY, 5.0f, 20.0f, 0.05f, 0.2f);
				StartCoroutine(DoScaleStuff(4.0f, amount));
			}
			movementStats.velocityY = -1.0f; // Just enough where we don't fall through the floor, but continue to have collisions
		}

		// Get input for jumping
		if (Input.GetButtonDown("Jump"))
			RegisterJumpPressed();

		// Variable jumping because variable jumping is usually good
		if ((movementStats.velocityY > 0) && (!Input.GetButton("Jump"))) {
			movementStats.velocityY -= movementStats.gravity * Time.deltaTime;
		}

		// If the player has pressed jump, is it okay to do so?
		if (jumpInputBuffer > 0.0f) {
			// Make sure we're on the ground
			if ((isGrounded) && (movementStats.jumpDelayTimer > movementStats.jumpDelay)) {
				// Set up the timers for scaling and the actual jump
				jumpScaleTimer = 0.0f;
				StartCoroutine(DoScaleStuff(jumpScaleSpeed, 0.2f));
				movementStats.jumpDelayTimer = movementStats.jumpDelay;

				jumpInputBuffer = 0.0f;
			}

			jumpInputBuffer -= Time.deltaTime;
		}

		if (movementStats.jumpDelayTimer <= movementStats.jumpDelay) {
			if (movementStats.jumpDelayTimer <= 0.0f) {
				movementStats.velocityY = movementStats.jumpHeight;
				isGrounded = false;
				movementStats.jumpDelayTimer = movementStats.jumpDelay + 1.0f;
			}
			movementStats.jumpDelayTimer -= Time.deltaTime;
		}

		// Check for slopes bruh
		RaycastHit hit;
		Vector3 slopeOffset = transform.localToWorldMatrix.MultiplyVector(raycastInfo.slopeRaycastOffset);
		Ray ray = new Ray(transform.position + slopeOffset, Vector3.down);
		Debug.DrawRay(transform.position, Vector3.down);
		Quaternion rotation = transform.rotation;
		if (Physics.Raycast(ray, out hit, 2.0f)) {
			Vector3 newUp;
			if (Mathf.Abs(Vector3.Dot(hit.normal, Vector3.forward)) < 0.9f) // If it's a slope
				newUp = hit.normal;
			else
				newUp = Vector3.up;

			transform.rotation = Quaternion.FromToRotation(transform.up, newUp) * transform.rotation;

			Debug.DrawRay(transform.position + transform.up, transform.up * 2.0f, Color.green);
		}

		Vector3 forwardMovement = transform.forward * movementStats.moveSpeed * inputDir.magnitude;
		Vector3 verticalMovement = Vector3.up * movementStats.velocityY;
		rigidbody.velocity = forwardMovement + verticalMovement + sidedashMovement;

		transform.rotation = rotation;

		// Make the sidedash weaken over time, duh
		if (sidedashMovement.magnitude > 1.0f)
			sidedashMovement = Vector3.Lerp(sidedashMovement, Vector3.zero, 4.5f * Time.deltaTime);
		else
			sidedashMovement = Vector3.zero;
	}

	private void RegisterJumpPressed() {
		jumpInputBuffer = inputBufferTime;
	}

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
		float turnLean = -(angleBetween * oneOver180) * 50.0f;
		float dashLean = ScaleClamp(sidedashMovement.magnitude, 0.0f, 20.0f, 0.0f, 40.0f);

		// Figure out if we're going left or right
		dashLean *= Mathf.Sign((sidedashMovement - transform.right).magnitude - (sidedashMovement + transform.right).magnitude);

		float lean = (Mathf.Abs(turnLean) > Mathf.Abs(dashLean)) ? turnLean : dashLean;
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
	}

	private void HandleCollision(Collision collision) {
		// If the impulse is pushing us up, then we've hit it going downwards (thus, it's the ground)
		if (collision.impulse.y > 0.0f) {
			isGrounded = true;
		}
	}

	void OnCollisionEnter(Collision collision) {
		HandleCollision(collision);
	}

	void OnCollisionStay(Collision collision) {
		HandleCollision(collision);
	}

	void OnCollisionExit(Collision collision) {
		// No matter what, always assume that we just left the ground
		// The reason why, is that OnCollisionEnter/Stay will set it back to grounded if we're grounded
		isGrounded = false;
	}

	void OnTriggerEnter(Collider collider) {
		if ((!madeItToGoal) && (collider.CompareTag("Goal"))) {
			madeItToGoal = true;
			goal = collider.gameObject;
			camera.GetComponent<CameraBehavior>().AtGoal();
		}
	}
}
