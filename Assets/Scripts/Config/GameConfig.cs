using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The single asset every system reads from. Anything that wants tuning takes a
/// reference to this, not to the individual assets, so there is exactly one place
/// to look when a number needs changing.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "PrehistoricJam/Game Config")]
public class GameConfig : ScriptableObject
{
	public PlayerTuning player;
	public InputBindings input;

	[Header("Abilities")]
	public List<AbilityDefinition> abilities = new();

	[Tooltip("Abilities granted at the start of a run. Leave empty for a real run; fill it to test late-game zones.")]
	public List<AbilityId> startingAbilities = new();

	[Header("Enemies — one asset per species")]
	public List<EnemyTuning> enemies = new();

	public AbilityDefinition GetAbility(AbilityId id)
	{
		foreach (var ability in abilities)
			if (ability != null && ability.id == id) return ability;
		return null;
	}

	/// <summary>Every ability that eating this species progresses towards.</summary>
	public IEnumerable<AbilityDefinition> AbilitiesUnlockedBy(Species species)
	{
		foreach (var ability in abilities)
			if (ability != null && ability.unlockedBy == species) yield return ability;
	}

	public EnemyTuning GetEnemy(Species species)
	{
		foreach (var enemy in enemies)
			if (enemy != null && enemy.species == species) return enemy;
		return null;
	}
}
