using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a zone stocked with one species. Population and respawn timing are
/// inspector fields rather than config assets on purpose: they describe this
/// particular placement in the world, not the species itself.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
	[Tooltip("Where to spawn. Leave empty to use a Zone on this same object.")]
	[SerializeField] private Zone zone;

	[SerializeField] private EnemyBrain prefab;

	[Tooltip("How many of this species the zone holds at once.")]
	[Min(0)] [SerializeField] private int population = 3;

	[SerializeField] private float respawnDelay = 8f;

	[Tooltip("Never spawn this close to the hunter — popping into view breaks the hunt.")]
	[SerializeField] private float minDistanceFromHunter = 10f;

	private readonly List<EnemyBrain> alive = new();
	private float nextSpawnTime;

	private void Awake()
	{
		if (zone == null) zone = GetComponent<Zone>();
	}

	private void Start()
	{
		if (zone == null || prefab == null)
		{
			Debug.LogError("[EnemySpawner] Needs both a Zone and a prefab.", this);
			enabled = false;
			return;
		}

		// Fill the zone immediately so the world is populated on the first frame.
		for (int i = 0; i < population; i++) TrySpawn();
	}

	private void Update()
	{
		alive.RemoveAll(enemy => enemy == null);

		if (alive.Count >= population) return;
		if (Time.time < nextSpawnTime) return;

		if (TrySpawn()) nextSpawnTime = Time.time + respawnDelay;
	}

	private bool TrySpawn()
	{
		if (!zone.TryGetRandomPoint(out Vector2 point)) return false;

		var hunter = Hunter.Instance;
		if (hunter != null && Vector2.Distance(point, hunter.transform.position) < minDistanceFromHunter)
			return false;

		var enemy = Instantiate(prefab, point, Quaternion.identity);
		alive.Add(enemy);
		return true;
	}
}
