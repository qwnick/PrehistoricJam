using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// What a creature is able to cross. The roster splits cleanly along this axis:
/// the Camelsaur and Velociraptor walk, the Crocodile is amphibious, and anything
/// with wings ignores the terrain entirely.
/// </summary>
public enum NavDomain
{
	Land,
	Water,
	Amphibious,
	Air
}

/// <summary>
/// Turns the scene's tilemaps into a walkability grid for A* to search.
///
/// Built by hand rather than pulled from a pathfinding package: the map is
/// already a tilemap, so the grid is free, and a package would mean an import
/// everyone on the team has to keep in sync.
/// </summary>
public class NavGrid : MonoBehaviour
{
	public enum CellType : byte
	{
		Blocked = 0,
		Ground = 1,
		Water = 2
	}

	public static NavGrid Instance { get; private set; }

	[Tooltip("Defines the extent of the map. Anything outside it is impassable.")]
	[SerializeField] private Tilemap groundTilemap;

	[SerializeField] private Tilemap waterTilemap;

	[Tooltip("Optional — rocks, cliffs, anything solid.")]
	[SerializeField] private Tilemap obstacleTilemap;

	private CellType[] cells;
	private BoundsInt bounds;

	public BoundsInt Bounds => bounds;
	public bool IsBuilt => cells != null;

	private void Awake()
	{
		Instance = this;
		Rebuild();
	}

	private void OnDestroy()
	{
		if (Instance == this) Instance = null;
	}

	[ContextMenu("Rebuild")]
	public void Rebuild()
	{
		if (groundTilemap == null)
		{
			Debug.LogError("[NavGrid] No ground tilemap assigned — nothing to build from.", this);
			return;
		}

		groundTilemap.CompressBounds();
		bounds = groundTilemap.cellBounds;
		cells = new CellType[bounds.size.x * bounds.size.y];

		foreach (var cell in bounds.allPositionsWithin)
		{
			cells[Index(cell)] = Classify(cell);
		}
	}

	private CellType Classify(Vector3Int cell)
	{
		// Obstacles win over everything, then water, then plain ground. A cell with
		// no ground tile at all is off the edge of the world.
		if (obstacleTilemap != null && obstacleTilemap.HasTile(cell)) return CellType.Blocked;
		if (waterTilemap != null && waterTilemap.HasTile(cell)) return CellType.Water;
		if (groundTilemap.HasTile(cell)) return CellType.Ground;

		return CellType.Blocked;
	}

	public Vector3Int WorldToCell(Vector2 world) => groundTilemap.WorldToCell(world);
	public Vector2 CellToWorld(Vector3Int cell) => groundTilemap.GetCellCenterWorld(cell);

	public bool InBounds(Vector3Int cell)
		=> cell.x >= bounds.xMin && cell.x < bounds.xMax
		&& cell.y >= bounds.yMin && cell.y < bounds.yMax;

	/// <summary>Flat array index for a cell. Only valid when InBounds.</summary>
	public int Index(Vector3Int cell)
		=> (cell.y - bounds.yMin) * bounds.size.x + (cell.x - bounds.xMin);

	public int CellCount => cells?.Length ?? 0;

	public CellType Get(Vector3Int cell)
		=> InBounds(cell) && cells != null ? cells[Index(cell)] : CellType.Blocked;

	public bool IsPassable(Vector3Int cell, NavDomain domain)
	{
		if (!InBounds(cell)) return false;

		// Fliers ignore the terrain entirely — that is the whole point of Wings.
		if (domain == NavDomain.Air) return true;

		var type = Get(cell);

		return domain switch
		{
			NavDomain.Land => type == CellType.Ground,
			NavDomain.Water => type == CellType.Water,
			NavDomain.Amphibious => type is CellType.Ground or CellType.Water,
			_ => false
		};
	}

	/// <summary>Nearest passable cell to a world point, for snapping a target onto the grid.</summary>
	public bool TryFindNearestPassable(Vector2 world, NavDomain domain, out Vector3Int result, int maxRadius = 12)
	{
		var origin = WorldToCell(world);
		result = origin;

		if (IsPassable(origin, domain)) return true;

		for (int radius = 1; radius <= maxRadius; radius++)
		{
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dy = -radius; dy <= radius; dy++)
				{
					// Only the ring at this radius, not the filled square.
					if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;

					var candidate = new Vector3Int(origin.x + dx, origin.y + dy, origin.z);
					if (!IsPassable(candidate, domain)) continue;

					result = candidate;
					return true;
				}
			}
		}

		return false;
	}
}
