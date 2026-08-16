using UnityEngine;

/// <summary>
/// One asset per zone. Everything here is a property of the PLACE, not of the
/// hunter — which is why the desert's water drain lives here rather than in
/// PlayerTuning. Adding a fifth zone should never mean editing the player.
/// </summary>
[CreateAssetMenu(fileName = "ZoneTuning", menuName = "PrehistoricJam/Zone Tuning")]
public class ZoneTuning : ScriptableObject
{
	public ZoneType type;
	public string displayName;

	[Header("Survival")]
	[Tooltip("Water lost per second while here. Only the desert should be non-zero — it is the one thing in the game that can end a run.")]
	public float waterDrainPerSecond;

	[Header("Entry")]
	[Tooltip("Which ability the hunter needs before this zone is survivable at all. Purely informational — traversal is enforced by SurfaceBarrier colliders.")]
	public bool gated;

	public AbilityId requiredAbility;
}
