using UnityEngine;

/// <summary>
/// Tank controls: A/D turn the body, W/S drive along it.
///
/// This is the single owner of the snake's velocity. Abilities never write to the
/// Rigidbody themselves — they decide what should happen and call in here, which
/// keeps "who is moving the snake right now" answerable in one file.
///
/// Uses linearVelocity rather than MovePosition so the snake collides properly
/// with the world and with prey instead of tunnelling through it.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class HunterMovement : MonoBehaviour
{
	[SerializeField] private bool drawDebugGizmos = true;

	private Hunter hunter;
	private Rigidbody2D body;

	private Vector2 dashDirection;
	private float dashRotation;
	private float dashSpeed;
	private float dashEndTime;

	public bool IsDashing => Time.time < dashEndTime;

	/// <summary>Set by the Swim and Wings abilities; decides the speed factor.</summary>
	public LocomotionMode Mode { get; set; } = LocomotionMode.Ground;

	/// <summary>Set while a sustained ability is running on an empty stamina bar.</summary>
	public bool IsExhausted { get; set; }

	/// <summary>
	/// The body's facing. Sprites in this project point up.
	///
	/// Read off the Rigidbody rather than the transform: interpolation is on, so
	/// transform.up outside FixedUpdate is the smoothed *visual* rotation, which
	/// trails the physics rotation by up to a step. A dash launched from that stale
	/// value flies off-axis from the body — which is what made it look bent.
	/// </summary>
	public Vector2 Facing
	{
		get
		{
			float radians = body.rotation * Mathf.Deg2Rad;
			return new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians));
		}
	}

	private PlayerTuning Tuning => hunter.Tuning;

	private void Awake()
	{
		body = GetComponent<Rigidbody2D>();
		hunter = GetComponent<Hunter>();
	}

	private void FixedUpdate()
	{
		if (hunter.Input == null) return;

		// The heading is input-owned, never physics-owned. Clipping prey or scenery
		// hands the body an angular velocity that only decays (damping is 0.05), and
		// mid-dash there is no MoveRotation call to overwrite it — that leftover spin
		// is what made the snake keep turning after the burst.
		body.angularVelocity = 0f;

		// A dash owns the body outright — no steering mid-dash. With tank controls
		// that is the point: you commit to a heading before you launch. The rotation
		// is re-asserted every step so a glancing hit cannot bend the burst.
		if (IsDashing)
		{
			body.MoveRotation(dashRotation);
			body.linearVelocity = dashDirection * dashSpeed;
			return;
		}

		float turn = hunter.Input.Turn;
		body.MoveRotation(body.rotation - turn * Tuning.turnSpeed * Time.fixedDeltaTime);

		float throttle = hunter.Input.Throttle;
		float speed = Tuning.moveSpeed * SpeedFactor * (throttle >= 0f ? 1f : Tuning.reverseSpeedFactor);
		body.linearVelocity = Facing * (throttle * speed);
	}

	private float SpeedFactor
	{
		get
		{
			float factor = Mode switch
			{
				LocomotionMode.Swimming => Tuning.swimSpeedFactor,
				LocomotionMode.Flying => Tuning.flySpeedFactor,
				_ => 1f
			};

			// The hunter has no health, so running dry has to bite somewhere —
			// it costs speed rather than killing.
			if (IsExhausted) factor *= Tuning.exhaustedSpeedFactor;

			return factor;
		}
	}

	/// <summary>
	/// Called by DashAbility once it has paid the cost. The heading comes from the
	/// body itself rather than from the caller, so the burst can never fly at an
	/// angle to the sprite.
	/// </summary>
	public void BeginDash(float speed, float duration)
	{
		dashDirection = Facing;
		dashRotation = body.rotation;
		dashSpeed = speed;
		dashEndTime = Time.time + duration;

		body.angularVelocity = 0f;
	}

	private void OnDrawGizmosSelected()
	{
		// The dash distance is the yardstick every enemy flee radius is measured
		// against, so seeing it in the scene view is what makes tuning possible.
		if (!drawDebugGizmos) return;

		var tuning = hunter != null ? hunter.Tuning : null;
		if (tuning == null) return;

		Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
		Gizmos.DrawWireSphere(transform.position, tuning.dashDistance);
	}
}
