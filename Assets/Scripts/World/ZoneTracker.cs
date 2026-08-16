using System;
using UnityEngine;

/// <summary>
/// Keeps track of which zone the hunter is standing in and applies that zone's
/// effects — right now, the desert draining its water.
///
/// Lives on the hunter rather than on the zones so that there is exactly one
/// answer to "where am I", instead of overlapping trigger callbacks fighting
/// over it.
/// </summary>
[RequireComponent(typeof(Hunter))]
public class ZoneTracker : MonoBehaviour
{
	private Hunter hunter;
	private Zone currentZone;

	/// <summary>Null while the hunter is outside every zone.</summary>
	public Zone CurrentZone => currentZone;
	public ZoneTuning CurrentTuning { get; private set; }

	/// <summary>Fires on entering a different zone (or none). Good hook for music and titles.</summary>
	public event Action<ZoneTuning> ZoneChanged;

	private void Awake()
	{
		hunter = GetComponent<Hunter>();
	}

	private void FixedUpdate()
	{
		var found = Zone.Find(transform.position);
		if (found != currentZone) EnterZone(found);

		ApplyZoneEffects();
	}

	private void EnterZone(Zone zone)
	{
		currentZone = zone;
		CurrentTuning = zone != null && hunter.Config != null
			? hunter.Config.GetZone(zone.Type)
			: null;

		ZoneChanged?.Invoke(CurrentTuning);
	}

	private void ApplyZoneEffects()
	{
		if (hunter.Water == null) return;

		// No zone, or a zone with no drain, means water simply stops ticking down.
		hunter.Water.DrainPerSecond = CurrentTuning != null ? CurrentTuning.waterDrainPerSecond : 0f;
	}
}
