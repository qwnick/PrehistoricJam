using System;
using UnityEngine;

/// <summary>
/// Opponent Search, earned from the Pterosaur. A pulse that forces every hidden
/// creature in range to show itself for a few seconds.
///
/// Not a convenience: the Crocodile is invisible while submerged, so without this
/// the river hunt is impossible rather than merely hard.
/// </summary>
public class EcholocationAbility : HunterAbility
{
	public override AbilityId Id => AbilityId.OpponentSearch;

	[SerializeField] private bool drawDebugGizmos = true;

	/// <summary>Fired with the pulse radius — for VFX and audio to hang off.</summary>
	public event Action<float> Pulsed;

	private void Update()
	{
		if (Owner.Input == null) return;
		if (Owner.Input.OpponentSearchPressed) TryPulse();
	}

	/// <summary>Returns false if locked, cooling down, or unaffordable.</summary>
	public bool TryPulse()
	{
		if (!CanActivate()) return false;
		if (!PayActivationCost()) return false;

		Vector2 origin = transform.position;
		float sqrRange = Definition.range * Definition.range;

		foreach (var hidden in Concealment.All)
		{
			if (hidden == null) continue;
			if (((Vector2)hidden.transform.position - origin).sqrMagnitude > sqrRange) continue;

			hidden.Reveal(Definition.duration);
		}

		Pulsed?.Invoke(Definition.range);
		return true;
	}

	private void OnDrawGizmosSelected()
	{
		if (!drawDebugGizmos || Definition == null) return;

		Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.8f);
		Gizmos.DrawWireSphere(transform.position, Definition.range);
	}
}
