using UnityEngine;

/// <summary>
/// Tank controls: A/D turn the body, W/S drive along it. Also executes the dash,
/// because a dash is a movement override rather than a separate actor.
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
	private float dashEndTime;

	public bool IsDashing => Time.time < dashEndTime;

	private PlayerTuning Tuning => hunter.Tuning;

	/// <summary>The body's facing. Sprites in this project point up.</summary>
	public Vector2 Facing => transform.up;

	private void Awake()
	{
		body = GetComponent<Rigidbody2D>();
		hunter = GetComponent<Hunter>();
	}

	private void Update()
	{
		if (hunter.Input == null) return;
		if (hunter.Input.DashPressed) TryDash();
	}

	private void FixedUpdate()
	{
		if (hunter.Input == null) return;

		// A dash owns the body outright — no steering mid-dash. With tank controls
		// that is the whole point: you commit to a direction before you launch.
		if (IsDashing)
		{
			body.linearVelocity = dashDirection * Tuning.DashSpeed;
			return;
		}

		float turn = hunter.Input.Turn;
		body.MoveRotation(body.rotation - turn * Tuning.turnSpeed * Time.fixedDeltaTime);

		float throttle = hunter.Input.Throttle;
		float speed = Tuning.moveSpeed * (throttle >= 0f ? 1f : Tuning.reverseSpeedFactor);
		body.linearVelocity = Facing * (throttle * speed);
	}

	/// <summary>Returns false if the dash is not unlocked, still cooling down, or unaffordable.</summary>
	public bool TryDash()
	{
		var definition = hunter.Config.GetAbility(AbilityId.Dash);
		if (definition == null)
		{
			Debug.LogWarning("[HunterMovement] No Dash AbilityDefinition in the GameConfig.", this);
			return false;
		}

		if (IsDashing) return false;
		if (!hunter.Abilities.CanUse(definition, hunter.Stamina)) return false;
		if (!hunter.Stamina.TryConsume(definition.staminaCost)) return false;

		dashDirection = Facing;
		dashEndTime = Time.time + Tuning.dashDuration;
		hunter.Abilities.StartCooldown(AbilityId.Dash, definition.cooldown);
		return true;
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
