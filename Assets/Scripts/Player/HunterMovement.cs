using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody2D))]

public class HunterMovement : MonoBehaviour
{
	[Header("Tank Settings")]
	public float moveSpeed = 5f;
	public float turnSpeed = 200f;

	private Rigidbody2D rb;
	private Vector2 moveInput;

	// Define the input action directly in code
	private InputAction moveAction;

	void Awake()
	{
		// Set up a composite 2D Vector action (X for turning, Y for moving)
		moveAction = new InputAction(type: InputActionType.Value, expectedControlType: "Vector2");

		// Bind WASD and Arrow Keys to this action
		moveAction.AddCompositeBinding("2DVector")
			.With("Up", "<Keyboard>/w")
			.With("Down", "<Keyboard>/s")
			.With("Left", "<Keyboard>/a")
			.With("Right", "<Keyboard>/d")
			.With("Up", "<Keyboard>/upArrow")
			.With("Down", "<Keyboard>/downArrow")
			.With("Left", "<Keyboard>/leftArrow")
			.With("Right", "<Keyboard>/rightArrow");
	}

	// You MUST enable and disable InputActions when the object is toggled
	void OnEnable()
	{
		moveAction.Enable();
	}

	void OnDisable()
	{
		moveAction.Disable();
	}

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
	}

	void Update()
	{
		// 1. Read the input value as a Vector2
		// x = A/D (turning), y = W/S (forward/backward)
		moveInput = moveAction.ReadValue<Vector2>();
	}

	void FixedUpdate()
	{
		// 2. Apply Rotation (using moveInput.x)
		float newRotation = rb.rotation - moveInput.x * turnSpeed * Time.fixedDeltaTime;
		rb.MoveRotation(newRotation);

		// 3. Apply Movement (using moveInput.y)
		Vector2 movementDirection = transform.up;
		rb.MovePosition(rb.position + movementDirection * moveInput.y * moveSpeed * Time.fixedDeltaTime);
	}
}
