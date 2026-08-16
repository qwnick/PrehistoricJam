using UnityEngine;

/// <summary>
/// One asset per species. Speeds and radii are stored as MULTIPLIERS of the
/// hunter's own numbers rather than absolute values, because that is how the
/// design doc describes them — and because it keeps the roster coherent when the
/// snake gets retuned. Resolve them through the helpers, never read the raw field.
/// </summary>
[CreateAssetMenu(fileName = "EnemyTuning", menuName = "PrehistoricJam/Enemy Tuning")]
public class EnemyTuning : ScriptableObject
{
	[Header("Identity")]
	public Species species;
	public string displayName;

	[Header("Health")]
	public float health = 2f;

	[Header("Speed — multipliers of the hunter's move speed")]
	[Tooltip("Idle wandering pace.")]
	[Range(0f, 3f)] public float walkSpeedFactor = 0.35f;

	[Tooltip("Fleeing pace. Velociraptor is 'slightly slower than the snake' => just under 1.")]
	[Range(0f, 3f)] public float runSpeedFactor = 0.9f;

	[Header("Perception — multipliers of the hunter's DASH DISTANCE")]
	[Tooltip("Start fleeing when the snake gets this close. 1 = exactly one dash away.")]
	public float fleeRadiusInDashes = 1f;

	[Tooltip("Stop fleeing once the snake is this far. Must exceed the flee radius, otherwise the enemy flickers between states on the boundary.")]
	public float calmRadiusInDashes = 1.6f;

	[Header("Stamina")]
	[Tooltip("Pterosaur has none — it flees at full speed forever.")]
	public bool usesStamina = true;

	[Tooltip("Sized in ability uses, e.g. 3 = enough for exactly three dashes.")]
	public float maxStamina = 3f;

	public float staminaRegenPerSecond = 1f;
	public float staminaRegenDelay = 1.5f;

	[Header("Own dash (Velociraptor)")]
	public bool canDash;

	[Tooltip("Trigger the burst when the snake is this close, in hunter dash distances. Keep below fleeRadiusInDashes.")]
	public float dashTriggerRadiusInDashes = 0.5f;

	public float dashDistance = 3f;
	public float dashDuration = 0.2f;
	public float dashCooldown = 1f;
	public float dashStaminaCost = 1f;

	[Header("Navigation")]
	[Tooltip("What terrain this species can cross. Drives A* and nothing else.")]
	public NavDomain navDomain = NavDomain.Land;

	[Tooltip("How far it relocates when it bolts, in hunter dash distances. Pterosaur crosses the whole forest.")]
	public float fleeDistanceInDashes = 6f;

	[Header("Species-specific costs")]
	[Tooltip("Crocodile: drained while swimming. Camelsaur: drained while away from water.")]
	public float staminaDrainPerSecond = 1f;

	[Tooltip("Vulturesaur / Pterosaur: stamina spent to take off once.")]
	public float flightStaminaCost = 1f;

	[Header("Species-specific timings")]
	[Tooltip("Crocodile: minimum seconds resting on the shore before diving again.")]
	public float shoreRestSeconds = 2f;

	[Tooltip("Camelsaur: seconds spent drinking once it reaches water. Long — this is the opening the player uses.")]
	public float drinkSeconds = 6f;

	[Tooltip("Camelsaur: how close to water counts as being at the water.")]
	public float nearWaterDistance = 2f;

	[Tooltip("Vulturesaur: below this fraction of stamina it will run for a corpse instead of just fleeing.")]
	[Range(0f, 1f)] public float lowStaminaThreshold = 0.5f;

	[Tooltip("Vulturesaur: seconds of eating a corpse to refill stamina completely.")]
	public float corpseEatSeconds = 6f;

	[Header("Wandering")]
	public float wanderRadius = 4f;
	public float wanderPauseMin = 1f;
	public float wanderPauseMax = 3f;

	[Header("Corpse left behind")]
	public float corpseLifetime = 60f;

	[Tooltip("Stamina a scavenger (Vulturesaur) gains from eating this corpse.")]
	public float corpseNutrition = 1f;

	// ---- Resolvers: always go through these, never read the raw multipliers ----

	public float WalkSpeed(PlayerTuning player) => player.moveSpeed * walkSpeedFactor;
	public float RunSpeed(PlayerTuning player) => player.moveSpeed * runSpeedFactor;
	public float FleeRadius(PlayerTuning player) => player.dashDistance * fleeRadiusInDashes;
	public float CalmRadius(PlayerTuning player) => player.dashDistance * calmRadiusInDashes;
	public float DashTriggerRadius(PlayerTuning player) => player.dashDistance * dashTriggerRadiusInDashes;
	public float FleeDistance(PlayerTuning player) => player.dashDistance * fleeDistanceInDashes;

	public float DashSpeed => dashDistance / Mathf.Max(dashDuration, 0.01f);

	private void OnValidate()
	{
		// Hysteresis only works if the calm radius sits outside the flee radius.
		if (calmRadiusInDashes < fleeRadiusInDashes)
			calmRadiusInDashes = fleeRadiusInDashes;
	}
}
