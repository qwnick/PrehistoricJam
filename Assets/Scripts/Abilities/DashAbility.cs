using UnityEngine;

/// <summary>
/// The burst that makes fast prey catchable. Earned from the Velociraptor.
///
/// Its dash DISTANCE is also the yardstick the entire enemy roster is balanced
/// against — every flee radius in the game is written as a multiple of it — which
/// is why that number lives in PlayerTuning rather than in the ability asset.
/// Cost and cooldown are ability data and live in the AbilityDefinition.
/// </summary>
[RequireComponent(typeof(HunterMovement))]
public class DashAbility : HunterAbility
{
	public override AbilityId Id => AbilityId.Dash;

	private HunterMovement movement;

	protected override void Awake()
	{
		base.Awake();
		movement = GetComponent<HunterMovement>();
	}

	private void Update()
	{
		if (Owner.Input == null) return;
		if (Owner.Input.DashPressed) TryDash();
	}

	/// <summary>Returns false if locked, already dashing, cooling down, or unaffordable.</summary>
	public bool TryDash()
	{
		if (movement.IsDashing) return false;
		if (!CanActivate()) return false;
		if (!PayActivationCost()) return false;

		var tuning = Owner.Tuning;

		// Direction is locked in at launch: with tank controls, committing to a
		// heading before you fire is the whole skill of the ability.
		movement.BeginDash(movement.Facing, tuning.DashSpeed, tuning.dashDuration);
		return true;
	}
}
