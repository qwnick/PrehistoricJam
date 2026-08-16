using System;
using UnityEngine;

/// <summary>
/// Only prey carries this — by design the hunter has no health and nothing
/// attacks it. If that changes, this component drops onto the player unmodified.
/// </summary>
public class Health : MonoBehaviour
{
	[SerializeField] private float max = 3f;

	public float Max => max;
	public float Current { get; private set; }
	public bool IsDead => Current <= 0f;

	public event Action<float> Damaged;
	public event Action<Health> Died;

	private void Awake()
	{
		Current = max;
	}

	public void Configure(float newMax)
	{
		max = newMax;
		Current = max;
	}

	/// <summary>Returns true if the hit landed (i.e. the target was still alive).</summary>
	public bool TakeDamage(float amount)
	{
		if (IsDead || amount <= 0f) return false;

		Current = Mathf.Max(0f, Current - amount);
		Damaged?.Invoke(amount);

		if (IsDead) Died?.Invoke(this);
		return true;
	}

	public void Kill() => TakeDamage(Current);
}
