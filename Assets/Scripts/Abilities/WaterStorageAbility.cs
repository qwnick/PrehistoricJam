using UnityEngine;

/// <summary>
/// Water Storage, earned from the Camelsaur. The only passive: it is never
/// triggered, it just enlarges the water reserve the moment it is earned, which
/// is what makes the desert crossing survivable.
/// </summary>
public class WaterStorageAbility : HunterAbility
{
	public override AbilityId Id => AbilityId.WaterStorage;

	protected override void ApplyPassive()
	{
		var meter = Owner.Water;

		if (meter == null)
		{
			Debug.LogWarning("[WaterStorageAbility] No WaterMeter on the hunter — the passive has nothing to enlarge.", this);
			return;
		}

		meter.SetStorageMultiplier(Owner.Tuning.waterStorageMultiplier);
	}
}
