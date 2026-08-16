using System;
using UnityEngine;

/// <summary>
/// A shared stamina pool. The hunter spends it on abilities; every enemy that has
/// to pace itself while fleeing runs on the exact same component. That is what
/// makes "chase it until its stamina runs out" readable to the player.
///
/// All values are pushed in from a tuning asset via <see cref="Configure"/> —
/// the inspector fields are only fallbacks for prototype objects.
/// </summary>
public class Stamina : MonoBehaviour
{
	[SerializeField] private float max = 100f;
	[SerializeField] private float regenPerSecond = 25f;

	[Tooltip("Seconds without spending before regeneration starts again.")]
	[SerializeField] private float regenDelay = 0.6f;

	public float Max => max;
	public float Current { get; private set; }
	public float Normalized => max <= 0f ? 0f : Current / max;
	public bool IsEmpty => Current <= 0.01f;
	public bool IsFull => Current >= max - 0.01f;

	public event Action Changed;

	/// <summary>Raised the moment a cost or drain empties the pool.</summary>
	public event Action Depleted;

	private float lastSpendTime = float.NegativeInfinity;

	private void Awake()
	{
		Current = max;
	}

	public void Configure(float newMax, float newRegenPerSecond, float newRegenDelay, bool refill = true)
	{
		max = newMax;
		regenPerSecond = newRegenPerSecond;
		regenDelay = newRegenDelay;
		Current = refill ? max : Mathf.Min(Current, max);
		Changed?.Invoke();
	}

	public bool CanAfford(float amount) => Current >= amount;

	/// <summary>One-off cost (a dash, a bite). Returns false if unaffordable — nothing is spent.</summary>
	public bool TryConsume(float amount)
	{
		if (Current < amount) return false;
		Spend(amount);
		return true;
	}

	/// <summary>
	/// Continuous cost (swimming, flying, sprinting). Call once per frame.
	/// Returns false once the pool has run dry, so the caller can drop out of the action.
	/// </summary>
	public bool Drain(float perSecond)
	{
		if (IsEmpty) return false;
		Spend(perSecond * Time.deltaTime);
		return !IsEmpty;
	}

	public void Refill(float amount)
	{
		if (amount <= 0f) return;
		Current = Mathf.Min(max, Current + amount);
		Changed?.Invoke();
	}

	public void RefillFully()
	{
		Current = max;
		Changed?.Invoke();
	}

	private void Spend(float amount)
	{
		bool wasEmpty = IsEmpty;
		Current = Mathf.Max(0f, Current - amount);
		lastSpendTime = Time.time;
		Changed?.Invoke();

		if (!wasEmpty && IsEmpty) Depleted?.Invoke();
	}

	private void Update()
	{
		if (Current >= max) return;
		if (Time.time - lastSpendTime < regenDelay) return;

		Current = Mathf.Min(max, Current + regenPerSecond * Time.deltaTime);
		Changed?.Invoke();
	}
}
