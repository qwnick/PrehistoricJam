using UnityEngine;

/// <summary>
/// Swim, earned from the Crocodile. Not a button: it engages by itself whenever
/// the hunter is in water and stays engaged until it leaves.
///
/// Locked, the hunter simply cannot enter water at all (a traversal rule in stage
/// 3), which is what gates the river.
/// </summary>
[RequireComponent(typeof(HunterMovement))]
public class SwimAbility : HunterAbility
{
	public override AbilityId Id => AbilityId.Swim;

	private HunterMovement movement;
	private SurfaceSensor sensor;

	public bool IsSwimming { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		movement = GetComponent<HunterMovement>();
		sensor = GetComponent<SurfaceSensor>();
	}

	protected override void Start()
	{
		base.Start();

		if (sensor == null)
		{
			Debug.LogError("[SwimAbility] Needs a SurfaceSensor on the hunter to know it is in water.", this);
			enabled = false;
		}
	}

	private void Update()
	{
		if (!IsUnlocked || !sensor.IsInWater)
		{
			StopSwimming();
			return;
		}

		if (!IsSwimming)
		{
			IsSwimming = true;
			movement.Mode = LocomotionMode.Swimming;
		}

		// Running dry does not drown the hunter — there is no health. It just
		// makes the crossing a slow, ugly one.
		movement.IsExhausted = !PaySustainCost();
	}

	private void StopSwimming()
	{
		if (!IsSwimming) return;

		IsSwimming = false;
		movement.Mode = LocomotionMode.Ground;
		movement.IsExhausted = false;
	}

	private void OnDisable() => StopSwimming();
}
