using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
	[Header("Tracking Settings")]
	public Transform target;

	// Z is set to -10 by default so the camera sits behind the 2D scene
	public Vector3 offset = new Vector3(0f, 0f, -10f);

	[Header("Smoothness")]
	[Range(0.01f, 1f)]
	public float smoothTime = 0.15f;

	// Used internally by SmoothDamp to calculate the current speed of the camera
	private Vector3 velocity = Vector3.zero;

	void LateUpdate()
	{
		// Don't do anything if the target hasn't been assigned or was destroyed
		if (target == null) return;

		// Where the camera should be
		Vector3 desiredPosition = target.position + offset;

		// Smoothly interpolate between the camera's current position and the desired position
		transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
	}
}