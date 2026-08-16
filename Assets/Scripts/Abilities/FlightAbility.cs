using UnityEngine;

/// <summary>
/// Wings, earned from the Vulturesaur. A toggle, unlike Swim — the hunter chooses
/// when to spend stamina staying airborne.
///
/// It deliberately does NOT drop the hunter when stamina runs out: the whole point
/// of Wings is crossing into the rocks, and force-landing mid-crossing would
/// strand the player somewhere they cannot leave. Exhaustion costs speed instead.
/// </summary>
[RequireComponent(typeof(HunterMovement))]
public class FlightAbility : HunterAbility
{
	public override AbilityId Id => AbilityId.Wings;

	private HunterMovement movement;

	public bool IsFlying { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		movement = GetComponent<HunterMovement>();
	}

	private void Update()
	{
		if (Owner.Input == null) return;

		if (Owner.Input.ToggleFlyPressed) Toggle();
		if (!IsFlying) return;

		movement.IsExhausted = !PaySustainCost();
	}

	public void Toggle()
	{
		if (IsFlying) Land();
		else TakeOff();
	}

	public bool TakeOff()
	{
		if (IsFlying || !IsUnlocked) return false;

		IsFlying = true;
		movement.Mode = LocomotionMode.Flying;
		return true;
	}

	public void Land()
	{
		if (!IsFlying) return;

		IsFlying = false;
		movement.Mode = LocomotionMode.Ground;
		movement.IsExhausted = false;
	}

	private void OnDisable() => Land();
}
