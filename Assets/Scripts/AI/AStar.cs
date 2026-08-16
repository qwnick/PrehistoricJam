using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 8-directional A* over the NavGrid.
///
/// Working buffers are static and reused between searches: prey re-plans often,
/// and allocating a few thousand nodes every time would hand the garbage
/// collector a steady stream of work for no reason. Pathfinding is therefore
/// single-threaded and not reentrant, which is fine — it is only ever called
/// from enemy Think().
/// </summary>
public static class AStar
{
	private static readonly Vector3Int[] Neighbours =
	{
		new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0),
		new(1, 1, 0), new(1, -1, 0), new(-1, 1, 0), new(-1, -1, 0)
	};

	private const float StraightCost = 1f;
	private const float DiagonalCost = 1.41421356f;

	private static float[] gScore;
	private static int[] cameFrom;
	private static bool[] closed;
	private static int generationStamp;
	private static int[] visitedIn;

	private static readonly List<int> openHeap = new();
	private static readonly List<float> openPriority = new();

	/// <summary>
	/// Fills <paramref name="result"/> with world-space waypoints from start to
	/// goal. Returns false if no route exists within the node budget.
	/// </summary>
	public static bool TryFindPath(
		NavGrid grid, Vector2 startWorld, Vector2 goalWorld,
		NavDomain domain, List<Vector2> result, int maxNodes = 4000)
	{
		result.Clear();

		if (grid == null || !grid.IsBuilt) return false;
		if (!grid.TryFindNearestPassable(startWorld, domain, out var start)) return false;
		if (!grid.TryFindNearestPassable(goalWorld, domain, out var goal)) return false;

		if (start == goal)
		{
			result.Add(grid.CellToWorld(goal));
			return true;
		}

		EnsureBuffers(grid.CellCount);
		generationStamp++;

		int startIndex = grid.Index(start);
		int goalIndex = grid.Index(goal);

		OpenClear();
		Visit(startIndex, 0f, -1);
		OpenPush(startIndex, Heuristic(start, goal));

		int expanded = 0;

		while (openHeap.Count > 0)
		{
			int current = OpenPop();
			if (closed[current]) continue;

			closed[current] = true;

			if (current == goalIndex)
			{
				Reconstruct(grid, current, startIndex, result);
				return true;
			}

			if (++expanded > maxNodes) return false;

			var cell = IndexToCell(grid, current);

			for (int i = 0; i < Neighbours.Length; i++)
			{
				var next = cell + Neighbours[i];
				if (!grid.IsPassable(next, domain)) continue;

				// No cutting corners diagonally past a blocked cell — it looks like
				// clipping through terrain.
				if (i >= 4)
				{
					var sideA = new Vector3Int(cell.x + Neighbours[i].x, cell.y, cell.z);
					var sideB = new Vector3Int(cell.x, cell.y + Neighbours[i].y, cell.z);

					if (!grid.IsPassable(sideA, domain) || !grid.IsPassable(sideB, domain)) continue;
				}

				int nextIndex = grid.Index(next);
				if (closed[nextIndex] && visitedIn[nextIndex] == generationStamp) continue;

				float tentative = gScore[current] + (i < 4 ? StraightCost : DiagonalCost);

				if (visitedIn[nextIndex] == generationStamp && tentative >= gScore[nextIndex]) continue;

				Visit(nextIndex, tentative, current);
				OpenPush(nextIndex, tentative + Heuristic(next, goal));
			}
		}

		return false;
	}

	private static void Visit(int index, float g, int parent)
	{
		visitedIn[index] = generationStamp;
		gScore[index] = g;
		cameFrom[index] = parent;
		closed[index] = false;
	}

	private static void Reconstruct(NavGrid grid, int goalIndex, int startIndex, List<Vector2> result)
	{
		int node = goalIndex;

		while (node != -1 && node != startIndex)
		{
			result.Add(grid.CellToWorld(IndexToCell(grid, node)));
			node = cameFrom[node];
		}

		result.Reverse();
	}

	private static Vector3Int IndexToCell(NavGrid grid, int index)
	{
		var bounds = grid.Bounds;
		int width = bounds.size.x;

		return new Vector3Int(
			bounds.xMin + index % width,
			bounds.yMin + index / width,
			0);
	}

	/// <summary>Octile distance — the exact cost of an unobstructed 8-way walk.</summary>
	private static float Heuristic(Vector3Int a, Vector3Int b)
	{
		int dx = Mathf.Abs(a.x - b.x);
		int dy = Mathf.Abs(a.y - b.y);

		return StraightCost * (dx + dy) + (DiagonalCost - 2f * StraightCost) * Mathf.Min(dx, dy);
	}

	private static void EnsureBuffers(int cellCount)
	{
		if (gScore != null && gScore.Length >= cellCount) return;

		gScore = new float[cellCount];
		cameFrom = new int[cellCount];
		closed = new bool[cellCount];
		visitedIn = new int[cellCount];
		generationStamp = 0;
	}

	// ---- Binary min-heap ----

	private static void OpenClear()
	{
		openHeap.Clear();
		openPriority.Clear();
	}

	private static void OpenPush(int index, float priority)
	{
		openHeap.Add(index);
		openPriority.Add(priority);

		int child = openHeap.Count - 1;

		while (child > 0)
		{
			int parent = (child - 1) / 2;
			if (openPriority[parent] <= openPriority[child]) break;

			Swap(parent, child);
			child = parent;
		}
	}

	private static int OpenPop()
	{
		int result = openHeap[0];
		int last = openHeap.Count - 1;

		openHeap[0] = openHeap[last];
		openPriority[0] = openPriority[last];
		openHeap.RemoveAt(last);
		openPriority.RemoveAt(last);

		int parent = 0;

		while (true)
		{
			int left = parent * 2 + 1;
			int right = left + 1;
			int smallest = parent;

			if (left < openHeap.Count && openPriority[left] < openPriority[smallest]) smallest = left;
			if (right < openHeap.Count && openPriority[right] < openPriority[smallest]) smallest = right;
			if (smallest == parent) break;

			Swap(parent, smallest);
			parent = smallest;
		}

		return result;
	}

	private static void Swap(int a, int b)
	{
		(openHeap[a], openHeap[b]) = (openHeap[b], openHeap[a]);
		(openPriority[a], openPriority[b]) = (openPriority[b], openPriority[a]);
	}
}
