using UnityEngine;

/// <summary>
/// Every number describing the hunter. This asset is also the balance anchor for
/// the whole enemy roster: the design doc expresses enemy behaviour relative to
/// the snake ("bigger than snake attack radius", "dash radius x 2/x 3/x 6"), so
/// enemies store their radii as multipliers of <see cref="dashDistance"/> and
/// their speeds as multipliers of <see cref="moveSpeed"/>. Change a number here
/// and the whole roster rescales with it.
/// </summary>
[CreateAssetMenu(fileName = "PlayerTuning", menuName = "PrehistoricJam/Player Tuning")]
public class PlayerTuning : ScriptableObject
{
	[Header("Movement — tank controls")]
	[Tooltip("Forward crawl speed. THE reference speed for every enemy in the game.")]
	public float moveSpeed = 4f;

	[Range(0f, 1f)]
	[Tooltip("How much slower the snake crawls backwards.")]
	public float reverseSpeedFactor = 0.5f;

	[Tooltip("Degrees per second the body turns with A/D.")]
	public float turnSpeed = 220f;

	[Header("Stamina")]
	public float maxStamina = 100f;
	public float staminaRegenPerSecond = 25f;

	[Tooltip("Seconds without spending before stamina starts coming back.")]
	public float staminaRegenDelay = 0.6f;

	[Header("Attack")]
	[Tooltip("THE reference attack radius. Enemy flee radii are tuned against this.")]
	public float attackRadius = 1.2f;

	[Range(10f, 360f)]
	[Tooltip("Tank controls make attacks directional, so the bite is a cone in front, not a full circle.")]
	public float attackArcDegrees = 120f;

	public float attackDamage = 1f;
	public float attackCooldown = 0.5f;
	public float attackStaminaCost = 15f;

	[Header("Dash — geometry only")]
	// Stamina cost and cooldown live on the Dash AbilityDefinition, not here.
	// Rule for the whole project: unlockable abilities keep their costs in
	// AbilityDefinition, innate actions (move, attack, eat) keep them here.
	[Tooltip("THE reference radius for the whole roster. Enemies store flee radii as multiples of this.")]
	public float dashDistance = 4f;

	public float dashDuration = 0.2f;

	[Header("Eating")]
	public float eatRadius = 1.2f;

	[Tooltip("Stamina returned by eating a corpse. Eating is also what advances the kill counters.")]
	public float eatStaminaRestore = 40f;

	// Swim and Wings are abilities, so their stamina drain lives on their
	// AbilityDefinition. Only the movement geometry belongs here.
	[Header("Swim / Wings — speed only")]
	[Range(0f, 3f)] public float swimSpeedFactor = 1f;
	[Range(0f, 3f)] public float flySpeedFactor = 1.3f;

	[Header("Water — the only fail state right now")]
	public float maxWater = 100f;

	[Tooltip("Water lost per second while in the desert. Hitting zero is game over.")]
	public float desertWaterDrainPerSecond = 2f;

	[Tooltip("Water regained per second while next to water.")]
	public float waterRefillPerSecond = 25f;

	[Tooltip("Water Storage (passive, from Camelsaur) multiplies max water by this.")]
	public float waterStorageMultiplier = 3f;

	/// <summary>Dash is defined by distance and duration; speed falls out of them.</summary>
	public float DashSpeed => dashDistance / Mathf.Max(dashDuration, 0.01f);
}
