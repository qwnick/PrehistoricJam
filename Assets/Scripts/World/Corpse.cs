using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What prey leaves behind. Corpses are a real world resource, not a death
/// effect: eating one is what advances the hunter's kill counters, and the
/// Vulturesaur feeds on them to refill its stamina. That shared dependency is
/// what makes the "kill something, leave the body, ambush the scavenger"
/// strategy work, so the world keeps a registry of every corpse in it.
/// </summary>
public class Corpse : MonoBehaviour
{
	/// <summary>Every corpse currently in the world. Scavenger AI searches this.</summary>
	public static readonly List<Corpse> All = new();

	[SerializeField] private Species species;
	[SerializeField] private float nutrition = 1f;
	[SerializeField] private float lifetime = 60f;

	public Species Species => species;

	/// <summary>Stamina a scavenger gains from eating this.</summary>
	public float Nutrition => nutrition;

	private float decayTime;

	/// <summary>Called by whatever spawned the corpse, so tuning stays in the enemy's asset.</summary>
	public void Initialize(Species newSpecies, float newLifetime, float newNutrition)
	{
		species = newSpecies;
		lifetime = newLifetime;
		nutrition = newNutrition;
		decayTime = Time.time + lifetime;
	}

	private void Awake()
	{
		decayTime = Time.time + lifetime;
	}

	private void OnEnable() => All.Add(this);
	private void OnDisable() => All.Remove(this);

	private void Update()
	{
		if (Time.time >= decayTime) Destroy(gameObject);
	}

	/// <summary>Eaten by the hunter or a scavenger.</summary>
	public void Consume()
	{
		Destroy(gameObject);
	}

	public static Corpse FindNearest(Vector2 position, float maxDistance)
	{
		Corpse best = null;
		float bestSqr = maxDistance * maxDistance;

		foreach (var corpse in All)
		{
			if (corpse == null) continue;

			float sqr = ((Vector2)corpse.transform.position - position).sqrMagnitude;
			if (sqr > bestSqr) continue;

			bestSqr = sqr;
			best = corpse;
		}

		return best;
	}
}
