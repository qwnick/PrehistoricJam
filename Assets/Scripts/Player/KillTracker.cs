using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Counts prey the hunter has EATEN — killing alone does not advance progression,
/// the body has to be consumed. Crossing a species' threshold unlocks that
/// species' ability. The thresholds and the species-to-ability mapping both live
/// in AbilityDefinition assets, so this class never hardcodes the evolution tree.
/// </summary>
public class KillTracker : MonoBehaviour
{
	private readonly Dictionary<Species, int> eaten = new();

	private GameConfig config;
	private AbilityInventory abilities;

	/// <summary>(species, new total)</summary>
	public event Action<Species, int> Changed;

	public void Initialize(GameConfig gameConfig, AbilityInventory abilityInventory)
	{
		config = gameConfig;
		abilities = abilityInventory;
		eaten.Clear();
	}

	public int EatenCount(Species species)
		=> eaten.TryGetValue(species, out int count) ? count : 0;

	/// <summary>How many more of this species are needed for that ability.</summary>
	public int RemainingFor(AbilityDefinition definition)
		=> definition == null ? 0 : Mathf.Max(0, definition.killsRequired - EatenCount(definition.unlockedBy));

	public void RegisterEaten(Species species)
	{
		int count = EatenCount(species) + 1;
		eaten[species] = count;
		Changed?.Invoke(species, count);

		foreach (var definition in config.AbilitiesUnlockedBy(species))
		{
			if (count < definition.killsRequired) continue;
			abilities.Unlock(definition);
		}
	}
}
