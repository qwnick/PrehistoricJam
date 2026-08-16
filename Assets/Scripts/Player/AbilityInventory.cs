using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Which abilities the hunter has evolved, plus their cooldowns. Knows nothing
/// about what any ability does — the ability's own component asks this whether
/// it is allowed to fire.
/// </summary>
public class AbilityInventory : MonoBehaviour
{
	private readonly HashSet<AbilityId> unlocked = new();
	private readonly Dictionary<AbilityId, float> readyAtTime = new();

	public event Action<AbilityDefinition> Unlocked;

	public IReadOnlyCollection<AbilityId> All => unlocked;

	/// <summary>Driven by <see cref="Hunter"/> so initialisation order is explicit.</summary>
	public void Initialize(GameConfig config)
	{
		unlocked.Clear();
		readyAtTime.Clear();

		// Debug shortcut: fill startingAbilities in the GameConfig to test late zones.
		foreach (var id in config.startingAbilities) unlocked.Add(id);
	}

	public bool Has(AbilityId id) => unlocked.Contains(id);

	/// <summary>Returns false if it was already unlocked, so callers can gate the fanfare.</summary>
	public bool Unlock(AbilityDefinition definition)
	{
		if (definition == null || !unlocked.Add(definition.id)) return false;

		Unlocked?.Invoke(definition);
		return true;
	}

	public bool IsReady(AbilityId id)
		=> !readyAtTime.TryGetValue(id, out float readyAt) || Time.time >= readyAt;

	public float CooldownRemaining(AbilityId id)
		=> readyAtTime.TryGetValue(id, out float readyAt) ? Mathf.Max(0f, readyAt - Time.time) : 0f;

	public void StartCooldown(AbilityId id, float cooldown)
	{
		if (cooldown > 0f) readyAtTime[id] = Time.time + cooldown;
	}

	/// <summary>Unlocked, off cooldown, and affordable — the full "can I fire this" check.</summary>
	public bool CanUse(AbilityDefinition definition, Stamina stamina)
	{
		if (definition == null) return false;
		if (!Has(definition.id)) return false;
		if (!IsReady(definition.id)) return false;

		return stamina == null || stamina.CanAfford(definition.staminaCost);
	}
}
