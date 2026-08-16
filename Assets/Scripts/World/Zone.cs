using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks an area of the map as one of the four zones. Put this on an object with
/// a trigger collider covering the region — a PolygonCollider2D drawn around the
/// forest, the river, and so on.
///
/// Zones are a lookup, not a physical barrier. What the hunter is allowed to
/// walk into is enforced separately by SurfaceBarrier.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Zone : MonoBehaviour
{
	public static readonly List<Zone> All = new();

	[SerializeField] private ZoneType type;

	[Tooltip("Higher wins where zones overlap — e.g. a river drawn on top of the forest.")]
	[SerializeField] private int priority;

	[SerializeField] private bool drawDebugGizmos = true;

	public ZoneType Type => type;
	public int Priority => priority;

	private Collider2D area;

	private void Awake()
	{
		area = GetComponent<Collider2D>();
	}

	private void OnEnable() => All.Add(this);
	private void OnDisable() => All.Remove(this);

	public bool Contains(Vector2 point) => area != null && area.OverlapPoint(point);

	/// <summary>The highest-priority zone covering this point, or null out in the open.</summary>
	public static Zone Find(Vector2 point)
	{
		Zone best = null;

		foreach (var zone in All)
		{
			if (zone == null || !zone.Contains(point)) continue;
			if (best != null && zone.priority <= best.priority) continue;

			best = zone;
		}

		return best;
	}

	/// <summary>A random point inside the zone. Returns false if it could not find one.</summary>
	public bool TryGetRandomPoint(out Vector2 point, int attempts = 24)
	{
		point = transform.position;
		if (area == null) return false;

		Bounds bounds = area.bounds;

		for (int i = 0; i < attempts; i++)
		{
			var candidate = new Vector2(
				Random.Range(bounds.min.x, bounds.max.x),
				Random.Range(bounds.min.y, bounds.max.y));

			if (!area.OverlapPoint(candidate)) continue;

			point = candidate;
			return true;
		}

		return false;
	}

	private void OnDrawGizmosSelected()
	{
		if (!drawDebugGizmos) return;

		var collider = GetComponent<Collider2D>();
		if (collider == null) return;

		Gizmos.color = new Color(1f, 1f, 0.3f, 0.4f);
		Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
	}
}
