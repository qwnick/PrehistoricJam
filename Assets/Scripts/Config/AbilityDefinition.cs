using UnityEngine;

/// <summary>
/// One asset per ability. Progression lives here rather than in code: which
/// species drops it and how many of them must be EATEN (not merely killed) to
/// earn it. Designers can re-wire the whole evolution tree without a recompile.
/// </summary>
[CreateAssetMenu(fileName = "Ability", menuName = "PrehistoricJam/Ability")]
public class AbilityDefinition : ScriptableObject
{
	[Header("Identity")]
	public AbilityId id;
	public string displayName;
	[TextArea] public string description;
	public Sprite icon;

	[Tooltip("Passive abilities (Water Storage) apply on unlock and are never triggered.")]
	public bool isPassive;

	[Header("Unlock")]
	[Tooltip("Eating this species is what counts towards the unlock.")]
	public Species unlockedBy;

	[Min(1)] public int killsRequired = 3;

	[Header("Cost — leave at zero for passives")]
	[Tooltip("One-off cost per use (Dash, Opponent Search).")]
	public float staminaCost;

	[Tooltip("Cost per second while sustained (Swim, Wings).")]
	public float staminaDrainPerSecond;

	public float cooldown;

	[Header("Shape — meaning depends on the ability")]
	[Tooltip("Echolocation: how far the pulse reaches.")]
	public float range;

	[Tooltip("Echolocation: how long revealed prey stays visible.")]
	public float duration;
}
