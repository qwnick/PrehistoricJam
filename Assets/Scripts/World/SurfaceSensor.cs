using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Answers "what am I standing on" for the hunter. Kept separate from the
/// abilities so that Swim, the desert water drain, and the traversal rules in
/// stage 3 all read the same answer instead of each doing their own overlap test.
/// </summary>
public class SurfaceSensor : MonoBehaviour
{
	[Tooltip("Set to the Water layer.")]
	[SerializeField] private LayerMask waterMask;

	[Tooltip("How far from the body counts as being IN the water.")]
	[SerializeField] private float inWaterRadius = 0.2f;

	[Tooltip("How far from the water counts as being able to drink from it.")]
	[SerializeField] private float nearWaterRadius = 2f;

	[SerializeField] private bool drawDebugGizmos = true;

	/// <summary>Standing in water — Swim keys off this.</summary>
	public bool IsInWater { get; private set; }

	/// <summary>Close enough to drink — the water meter refills off this.</summary>
	public bool IsNearWater { get; private set; }

	private ContactFilter2D waterFilter;
	private readonly List<Collider2D> results = new();

	private void Awake()
	{
		waterFilter = new ContactFilter2D
		{
			useLayerMask = true,
			layerMask = waterMask,
			useTriggers = true
		};
	}

	private void FixedUpdate()
	{
		IsInWater = Overlaps(inWaterRadius);
		IsNearWater = IsInWater || Overlaps(nearWaterRadius);
	}

	private bool Overlaps(float radius)
	{
		Physics2D.OverlapCircle(transform.position, radius, waterFilter, results);
		return results.Count > 0;
	}

	private void OnDrawGizmosSelected()
	{
		if (!drawDebugGizmos) return;

		Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.9f);
		Gizmos.DrawWireSphere(transform.position, inWaterRadius);

		Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.35f);
		Gizmos.DrawWireSphere(transform.position, nearWaterRadius);
	}
}
