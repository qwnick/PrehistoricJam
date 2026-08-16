using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Biting and eating. Both are innate — available from the first second of the
/// run — so their costs live in PlayerTuning rather than in an AbilityDefinition.
///
/// The bite is a cone in front of the body, not a circle: with tank controls the
/// player already commits to a facing, and a circle would make that commitment
/// meaningless.
/// </summary>
[RequireComponent(typeof(Hunter))]
public class HunterCombat : MonoBehaviour
{
	[Tooltip("Layers that can be bitten. Set this to the Enemy layer.")]
	[SerializeField] private LayerMask preyMask;

	[SerializeField] private bool drawDebugGizmos = true;

	private Hunter hunter;
	private HunterMovement movement;

	private ContactFilter2D preyFilter;
	private readonly List<Collider2D> overlapResults = new();

	private float attackReadyTime;

	private PlayerTuning Tuning => hunter.Tuning;

	public bool AttackReady => Time.time >= attackReadyTime;

	/// <summary>Seconds until the next bite is allowed. Zero when ready — the HUD reads this.</summary>
	public float AttackCooldownRemaining => Mathf.Max(0f, attackReadyTime - Time.time);

	private void Awake()
	{
		hunter = GetComponent<Hunter>();
		movement = GetComponent<HunterMovement>();

		if (movement == null)
		{
			Debug.LogError("[HunterCombat] Needs a HunterMovement on the same object to know which way the bite faces.", this);
			enabled = false;
			return;
		}

		preyFilter = new ContactFilter2D
		{
			useLayerMask = true,
			layerMask = preyMask,
			useTriggers = true
		};
	}

	private void Update()
	{
		if (hunter.Input == null) return;

		if (hunter.Input.AttackPressed) TryAttack();
		if (hunter.Input.EatPressed) TryEat();
	}

	/// <summary>Returns true if the bite went out (whether or not it connected).</summary>
	public bool TryAttack()
	{
		if (!AttackReady) return false;
		if (!hunter.Stamina.TryConsume(Tuning.attackStaminaCost)) return false;

		attackReadyTime = Time.time + Tuning.attackCooldown;

		Physics2D.OverlapCircle(transform.position, Tuning.attackRadius, preyFilter, overlapResults);

		float halfArc = Tuning.attackArcDegrees * 0.5f;

		foreach (var collider in overlapResults)
		{
			if (collider == null) continue;

			Vector2 toTarget = (Vector2)collider.bounds.ClosestPoint(transform.position) - (Vector2)transform.position;
			if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(movement.Facing, toTarget) > halfArc) continue;

			var health = collider.GetComponent<Health>();
			if (health == null) continue;

			health.TakeDamage(Tuning.attackDamage);
		}

		return true;
	}

	/// <summary>
	/// Eating is what actually advances progression — a kill on its own counts
	/// for nothing. Returns true if a corpse was consumed.
	/// </summary>
	public bool TryEat()
	{
		var corpse = Corpse.FindNearest(transform.position, Tuning.eatRadius);
		if (corpse == null) return false;

		hunter.Kills.RegisterEaten(corpse.Species);
		hunter.Stamina.Refill(Tuning.eatStaminaRestore);
		corpse.Consume();
		return true;
	}

	private void OnDrawGizmosSelected()
	{
		if (!drawDebugGizmos) return;

		var tuning = hunter != null ? hunter.Tuning : null;
		if (tuning == null) return;

		// Bite cone.
		Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.9f);
		Vector3 facing = transform.up;
		float halfArc = tuning.attackArcDegrees * 0.5f;

		Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, 0f, halfArc) * facing * tuning.attackRadius);
		Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, 0f, -halfArc) * facing * tuning.attackRadius);
		Gizmos.DrawWireSphere(transform.position, tuning.attackRadius);

		// Eat reach.
		Gizmos.color = new Color(0.5f, 1f, 0.4f, 0.6f);
		Gizmos.DrawWireSphere(transform.position, tuning.eatRadius);
	}
}
