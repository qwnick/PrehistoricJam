using System;
using UnityEngine;

/// <summary>
/// The hunter's water reserve — currently the only way to lose the game. It only
/// drains where something tells it to (the desert, in stage 3); everywhere else
/// the drain rate is zero, so this sits harmlessly on the player from the start.
/// </summary>
public class WaterMeter : MonoBehaviour
{
	public float Max { get; private set; }
	public float Current { get; private set; }
	public float Normalized => Max <= 0f ? 0f : Current / Max;
	public bool IsEmpty => Current <= 0.01f;

	public event Action Changed;

	/// <summary>Raised once when the reserve runs out. Game over hangs off this.</summary>
	public event Action Emptied;

	/// <summary>Set by whatever zone the hunter is standing in. Zero outside the desert.</summary>
	public float DrainPerSecond { get; set; }

	private PlayerTuning tuning;
	private SurfaceSensor sensor;
	private float storageMultiplier = 1f;

	private void Awake()
	{
		sensor = GetComponent<SurfaceSensor>();
	}

	public void Initialize(PlayerTuning playerTuning)
	{
		tuning = playerTuning;
		Max = tuning.maxWater * storageMultiplier;
		Current = Max;
		Changed?.Invoke();
	}

	/// <summary>Applied by the Water Storage passive.</summary>
	public void SetStorageMultiplier(float multiplier)
	{
		if (tuning == null) return;

		// Keep the fill ratio: earning a bigger tank should read as a reward, not
		// as suddenly being at a third of your water.
		float ratio = Normalized;
		storageMultiplier = Mathf.Max(0.01f, multiplier);
		Max = tuning.maxWater * storageMultiplier;
		Current = Max * ratio;
		Changed?.Invoke();
	}

	private void Update()
	{
		if (tuning == null) return;

		if (sensor != null && sensor.IsNearWater)
		{
			Fill(tuning.waterRefillPerSecond * Time.deltaTime);
			return;
		}

		if (DrainPerSecond > 0f) Drain(DrainPerSecond * Time.deltaTime);
	}

	private void Fill(float amount)
	{
		if (Current >= Max) return;

		Current = Mathf.Min(Max, Current + amount);
		Changed?.Invoke();
	}

	private void Drain(float amount)
	{
		if (IsEmpty) return;

		Current = Mathf.Max(0f, Current - amount);
		Changed?.Invoke();

		if (IsEmpty) Emptied?.Invoke();
	}
}
