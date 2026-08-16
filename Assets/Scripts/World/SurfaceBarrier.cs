using UnityEngine;

/// <summary>
/// Turns a piece of terrain into a wall until the hunter evolves past it: the
/// river without Swim, the rocks without Wings.
///
/// It does this by flipping the collider between solid and trigger rather than by
/// vetoing movement in code. Physics then handles the edge case perfectly — no
/// jitter, no clipping, no snapping the snake back a frame later — and the
/// SurfaceSensor still detects the terrain either way, because its contact filter
/// includes triggers.
/// </summary>
public class SurfaceBarrier : MonoBehaviour
{
	[Tooltip("Once the hunter has this, the terrain becomes passable.")]
	[SerializeField] private AbilityId requiredAbility = AbilityId.Swim;

	[Tooltip("Leave empty to use the CompositeCollider2D (or any Collider2D) on this object.")]
	[SerializeField] private Collider2D barrier;

	private Hunter hunter;

	private void Awake()
	{
		if (barrier == null) barrier = GetComponent<CompositeCollider2D>();
		if (barrier == null) barrier = GetComponent<Collider2D>();

		if (barrier == null)
			Debug.LogError("[SurfaceBarrier] No collider to gate.", this);
	}

	private void Start()
	{
		hunter = Hunter.Instance;

		if (hunter == null)
		{
			Debug.LogWarning("[SurfaceBarrier] No hunter in the scene; leaving the terrain passable.", this);
			return;
		}

		hunter.Abilities.Unlocked += HandleUnlocked;
		Apply();
	}

	private void OnDestroy()
	{
		if (hunter != null) hunter.Abilities.Unlocked -= HandleUnlocked;
	}

	private void HandleUnlocked(AbilityDefinition definition)
	{
		if (definition.id == requiredAbility) Apply();
	}

	private void Apply()
	{
		if (barrier == null || hunter == null) return;

		barrier.isTrigger = hunter.Abilities.Has(requiredAbility);
	}
}
