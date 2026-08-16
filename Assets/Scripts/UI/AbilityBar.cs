using UnityEngine;

/// <summary>
/// The on-screen ability bar. Owns the slots below it and is the only thing that
/// listens to the hunter — a slot never subscribes to anything, so there is one
/// place to look when the bar stops updating.
///
/// It binds late rather than in Start: the bar is part of a UI scene that can load
/// before the player is spawned, and erroring out in that case would make the
/// whole HUD useless the moment scene loading order changes.
/// </summary>
[DisallowMultipleComponent]
public class AbilityBar : MonoBehaviour
{
	[Tooltip("Left empty, one is built in code under this canvas on first hover.")]
	[SerializeField] private AbilityTooltip tooltip;

	[Tooltip("Slots under this object. Filled automatically when left empty.")]
	[SerializeField] private AbilityButton[] slots;

	private Hunter hunter;
	private AbilityButton hovered;
	private bool tooltipUnavailable;

	private void Awake()
	{
		if (slots == null || slots.Length == 0)
			slots = GetComponentsInChildren<AbilityButton>(true);
	}

	private void Start() => TryBind();

	private void Update()
	{
		// Hunter.Instance is set in its Awake, so this normally succeeds on the
		// first frame; the retry only matters if the player is spawned later.
		if (hunter == null) TryBind();
	}

	private void OnDestroy()
	{
		if (hunter == null) return;

		hunter.Kills.Changed -= OnKillsChanged;
		hunter.Abilities.Unlocked -= OnAbilityUnlocked;
	}

	private void TryBind()
	{
		var found = Hunter.Instance;
		if (found == null) return;

		hunter = found;
		hunter.Kills.Changed += OnKillsChanged;
		hunter.Abilities.Unlocked += OnAbilityUnlocked;

		foreach (var slot in slots)
			if (slot != null) slot.Bind(hunter);
	}

	private void OnKillsChanged(Species species, int total) => RefreshAll();

	private void OnAbilityUnlocked(AbilityDefinition definition) => RefreshAll();

	public void RefreshAll()
	{
		foreach (var slot in slots)
			if (slot != null) slot.Refresh();

		// The hover text is now stale — "eat 2 more" may have become "eat 1 more".
		if (hovered != null) ShowTooltip(hovered);
	}

	// ---- Tooltip ----

	public void ShowTooltip(AbilityButton slot)
	{
		if (slot == null) return;

		hovered = slot;

		var panel = Tooltip();
		if (panel != null) panel.Show(slot);
	}

	/// <summary>
	/// Takes the slot so a stale exit from a slot the pointer already left cannot
	/// hide the tooltip of the one it moved onto.
	/// </summary>
	public void HideTooltip(AbilityButton slot)
	{
		if (hovered != slot) return;

		hovered = null;
		if (tooltip != null) tooltip.Hide();
	}

	private AbilityTooltip Tooltip()
	{
		// One failed attempt is enough — retrying would log the same error on
		// every single hover.
		if (tooltip != null || tooltipUnavailable) return tooltip;

		tooltip = AbilityTooltip.CreateUnder(GetComponentInParent<Canvas>());
		tooltipUnavailable = tooltip == null;

		return tooltip;
	}
}
