using UnityEngine;

/// <summary>
/// Base for every unlockable ability the hunter has. Handles the part all of them
/// repeat — finding its definition, checking it is unlocked, off cooldown and
/// affordable, then paying — so a subclass only writes its actual effect.
///
/// Abilities decide WHETHER something happens and what it costs. They never move
/// the body themselves: HunterMovement stays the single owner of velocity, and
/// abilities ask it to do things. That is why Dash lives here but its physics
/// do not.
/// </summary>
[RequireComponent(typeof(Hunter))]
public abstract class HunterAbility : MonoBehaviour
{
	protected Hunter Owner { get; private set; }

	/// <summary>
	/// Which AbilityDefinition this component implements. Declared in code rather
	/// than serialised: DashAbility is never anything but Dash, and an inspector
	/// field would just be one more thing to get wrong when building a prefab.
	/// </summary>
	public abstract AbilityId Id { get; }

	public AbilityDefinition Definition { get; private set; }

	public bool IsUnlocked => Owner != null && Owner.Abilities.Has(Id);

	protected virtual void Awake()
	{
		Owner = GetComponent<Hunter>();
	}

	protected virtual void Start()
	{
		Definition = Owner.Config != null ? Owner.Config.GetAbility(Id) : null;

		if (Definition == null)
		{
			Debug.LogError($"[{GetType().Name}] No AbilityDefinition for '{Id}' in the GameConfig.", this);
			enabled = false;
			return;
		}

		Owner.Abilities.Unlocked += HandleUnlocked;

		// A passive granted before this component started still has to take effect.
		if (Definition.isPassive && IsUnlocked) ApplyPassive();
	}

	protected virtual void OnDestroy()
	{
		if (Owner != null) Owner.Abilities.Unlocked -= HandleUnlocked;
	}

	private void HandleUnlocked(AbilityDefinition definition)
	{
		if (definition.id != Id) return;

		if (definition.isPassive) ApplyPassive();
		OnUnlocked();
	}

	/// <summary>Passives apply once, the moment they are earned.</summary>
	protected virtual void ApplyPassive() { }

	protected virtual void OnUnlocked() { }

	// ---- Cost helpers ----

	/// <summary>Unlocked, off cooldown, and affordable.</summary>
	protected bool CanActivate()
		=> Definition != null && Owner.Abilities.CanUse(Definition, Owner.Stamina);

	/// <summary>Pays the one-off cost and starts the cooldown. Call only after CanActivate().</summary>
	protected bool PayActivationCost()
	{
		if (!Owner.Stamina.TryConsume(Definition.staminaCost)) return false;

		Owner.Abilities.StartCooldown(Id, Definition.cooldown);
		return true;
	}

	/// <summary>Per-frame cost for sustained abilities. Returns false once stamina runs out.</summary>
	protected bool PaySustainCost()
		=> Owner.Stamina.Drain(Definition.staminaDrainPerSecond);
}
