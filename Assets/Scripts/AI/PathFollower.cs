using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Requests paths from A* and turns them into a steering direction. Sits between
/// the brains and the pathfinder so no brain has to know about grids or cells —
/// it says "go there" and asks which way to push each frame.
/// </summary>
public class PathFollower : MonoBehaviour
{
	[Tooltip("How close counts as having reached a waypoint.")]
	[SerializeField] private float waypointArriveRadius = 0.3f;

	[Tooltip("Minimum seconds between re-plans. Pathfinding every frame is wasted work.")]
	[SerializeField] private float repathInterval = 0.5f;

	[SerializeField] private bool drawDebugGizmos = true;

	private readonly List<Vector2> path = new();
	private int waypointIndex;
	private float nextRepathTime;
	private Vector2 destination;

	public bool HasPath => waypointIndex < path.Count;
	public Vector2 Destination => destination;

	/// <summary>
	/// Plans a route. Cheap to call every frame — it throttles itself and reuses
	/// the existing path when the target has barely moved.
	/// </summary>
	public bool SetDestination(Vector2 target, NavDomain domain, bool force = false)
	{
		bool sameTarget = (target - destination).sqrMagnitude < 1f;

		if (!force && sameTarget && HasPath && Time.time < nextRepathTime) return true;
		if (!force && Time.time < nextRepathTime) return HasPath;

		nextRepathTime = Time.time + repathInterval;
		destination = target;
		waypointIndex = 0;

		return AStar.TryFindPath(NavGrid.Instance, transform.position, target, domain, path);
	}

	/// <summary>Direction to steer right now. Vector2.zero once the path is finished.</summary>
	public Vector2 Steering()
	{
		Vector2 position = transform.position;

		while (waypointIndex < path.Count
		       && Vector2.Distance(position, path[waypointIndex]) <= waypointArriveRadius)
		{
			waypointIndex++;
		}

		if (waypointIndex >= path.Count) return Vector2.zero;

		return (path[waypointIndex] - position).normalized;
	}

	public void Clear()
	{
		path.Clear();
		waypointIndex = 0;
	}

	private void OnDrawGizmosSelected()
	{
		if (!drawDebugGizmos || path.Count == 0) return;

		Gizmos.color = new Color(0.4f, 1f, 1f, 0.9f);
		Vector2 previous = transform.position;

		for (int i = waypointIndex; i < path.Count; i++)
		{
			Gizmos.DrawLine(previous, path[i]);
			Gizmos.DrawWireSphere(path[i], 0.1f);
			previous = path[i];
		}
	}
}
