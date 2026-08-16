using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

/// <summary>
/// Bite and Eat are innate — they work from second one and have no
/// AbilityDefinition, so their cost and cooldown come from PlayerTuning instead.
/// They are separate values rather than one "Innate" flag because the bar has to
/// know WHICH innate action a slot is to read the right cooldown for it.
/// </summary>
public enum AbilitySlotKind
{
	Ability,
	Bite,
	Eat
}

/// <summary>
/// How the slot is fired, which decides whether it is a button at all.
/// Swim engages by itself in water and Water Storage is passive, so neither
/// gets an OnScreenButton — they are read-outs that show whether the hunter
/// owns them.
/// </summary>
public enum AbilityActivation
{
	Press,
	Automatic,
	Passive
}

/// <summary>
/// One slot of the on-screen ability bar. Locked slots stay visible — the bar is
/// the player's map of the evolution tree — but go dark, lose their OnScreenButton
/// so tapping them sends no input, and show how many more of that species are
/// still to be eaten.
///
/// All the wording comes from the AbilityDefinition asset, so a designer changes
/// what a tooltip says without touching this file.
/// </summary>
[DisallowMultipleComponent]
public class AbilityButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[Header("What this slot is")]
	[SerializeField] private AbilitySlotKind kind = AbilitySlotKind.Ability;

	[Tooltip("Ignored for innate slots.")]
	[SerializeField] private AbilityId ability;

	[SerializeField] private AbilityActivation activation = AbilityActivation.Press;

	[Header("Innate slots only — they have no AbilityDefinition to read")]
	[SerializeField] private string innateTitle = "Bite";
	[TextArea] [SerializeField] private string innateDescription;

	[Header("Parts")]
	[Tooltip("The creature sprite. Tinted dark while locked.")]
	[SerializeField] private Image icon;

	[Tooltip("Optional radial Filled image drained while the ability cools down.")]
	[SerializeField] private Image cooldownFill;

	[Tooltip("Shows how many more of the species still have to be eaten.")]
	[SerializeField] private TMP_Text counterLabel;

	[SerializeField] private Button button;

	[Tooltip("Disabled while locked so a tap on a dark slot sends no input at all.")]
	[SerializeField] private OnScreenButton onScreenButton;

	[Header("Locked look")]
	[SerializeField] private Color lockedIconTint = new(0.22f, 0.24f, 0.26f, 0.85f);

	[Tooltip("{0} = kills still needed, {1} = eaten so far, {2} = total required.")]
	[SerializeField] private string lockedCounterFormat = "{0}";

	[Header("Unlocked look")]
	[SerializeField] private Color readyIconTint = Color.white;

	[Tooltip("Applied while the ability is on cooldown or unaffordable.")]
	[SerializeField] private Color unavailableIconTint = new(0.65f, 0.65f, 0.7f, 0.75f);

	[Header("Press feedback")]
	[Tooltip("What gets squashed on a press. Defaults to this whole slot.")]
	[SerializeField] private Transform pressTarget;

	[SerializeField] [Range(0.5f, 1f)] private float pressScale = 0.86f;
	[SerializeField] [Min(0.01f)] private float pressDuration = 0.16f;

	private AbilityBar bar;
	private Hunter hunter;
	private AbilityDefinition definition;
	private bool unlocked;

	/// <summary>Negative means no press is playing.</summary>
	private float pressElapsed = -1f;

	public AbilitySlotKind Kind => kind;
	public AbilityId Ability => ability;
	public AbilityActivation Activation => activation;
	public bool IsUnlocked => unlocked;
	public AbilityDefinition Definition => definition;
	public bool IsInnate => kind != AbilitySlotKind.Ability;

	public string Title => definition != null ? definition.displayName : innateTitle;

	public string Description => definition != null ? definition.description : innateDescription;

	private PlayerTuning Tuning => hunter != null && hunter.Config != null ? hunter.Config.player : null;

	/// <summary>Species this slot is earned from — meaningless for innate slots.</summary>
	public Species UnlockedBy => definition != null ? definition.unlockedBy : default;

	public int Eaten => hunter != null && definition != null
		? hunter.Kills.EatenCount(definition.unlockedBy)
		: 0;

	public int Required => definition != null ? definition.killsRequired : 0;

	public int Remaining => hunter != null && definition != null
		? hunter.Kills.RemainingFor(definition)
		: 0;

	private void Awake()
	{
		bar = GetComponentInParent<AbilityBar>(true);

		// Wiring these by hand on seven buttons is seven chances to miss one.
		if (button == null) button = GetComponent<Button>();
		if (onScreenButton == null) onScreenButton = GetComponent<OnScreenButton>();
		if (pressTarget == null) pressTarget = transform;
	}

	/// <summary>Driven by <see cref="AbilityBar"/> so every slot refreshes from one place.</summary>
	public void Bind(Hunter owner)
	{
		hunter = owner;

		definition = kind == AbilitySlotKind.Ability && hunter.Config != null
			? hunter.Config.GetAbility(ability)
			: null;

		if (kind == AbilitySlotKind.Ability && definition == null)
			Debug.LogWarning($"[AbilityButton] '{name}' wants '{ability}' but the GameConfig has no such ability.", this);

		Refresh();
	}

	/// <summary>Re-reads unlocked state. Cheap, and only called when something actually changed.</summary>
	public void Refresh()
	{
		if (hunter == null) return;

		unlocked = IsInnate || hunter.Abilities.Has(ability);

		// A locked slot must not be able to send input, and interactable alone
		// would not stop that — OnScreenButton has its own pointer handler.
		if (onScreenButton != null)
			onScreenButton.enabled = unlocked && activation == AbilityActivation.Press;

		// Not gated on activation: an owned passive should still read as owned,
		// and its Button has no listener to fire anyway.
		if (button != null) button.interactable = unlocked;

		if (icon != null) icon.color = unlocked ? readyIconTint : lockedIconTint;

		if (counterLabel != null)
		{
			bool showCounter = !unlocked && definition != null;
			counterLabel.gameObject.SetActive(showCounter);

			if (showCounter)
				counterLabel.text = string.Format(lockedCounterFormat, Remaining, Eaten, Required);
		}

		if (cooldownFill != null) cooldownFill.gameObject.SetActive(false);
	}

	private void Update()
	{
		AnimatePress();

		if (hunter == null || !unlocked) return;
		if (activation != AbilityActivation.Press) return;

		if (WasPressedThisFrame()) pressElapsed = 0f;

		float remaining = CooldownRemaining(out float total);
		bool cooling = remaining > 0f && total > 0f;

		if (cooldownFill != null)
		{
			cooldownFill.gameObject.SetActive(cooling);

			if (cooling) cooldownFill.fillAmount = remaining / total;
		}

		if (icon != null)
			icon.color = cooling || !Affordable() ? unavailableIconTint : readyIconTint;
	}

	/// <summary>
	/// Where the cooldown comes from depends on the slot: abilities keep theirs in
	/// their AbilityDefinition, the innate bite keeps its in PlayerTuning. Same
	/// project rule the costs follow, so the bar has to look in both places.
	/// </summary>
	private float CooldownRemaining(out float total)
	{
		switch (kind)
		{
			case AbilitySlotKind.Bite:
				var tuning = Tuning;
				total = tuning != null ? tuning.attackCooldown : 0f;
				return hunter.Combat != null ? hunter.Combat.AttackCooldownRemaining : 0f;

			// Eating has no cooldown at all — it is gated by there being a corpse.
			case AbilitySlotKind.Eat:
				total = 0f;
				return 0f;

			default:
				total = definition != null ? definition.cooldown : 0f;
				return definition != null ? hunter.Abilities.CooldownRemaining(ability) : 0f;
		}
	}

	/// <summary>
	/// Read from the InputReader rather than from a pointer handler on purpose:
	/// the OnScreenButton feeds the same actions the keyboard does, so one branch
	/// gives a tap and a key press identical feedback — and Eat, which has no
	/// cooldown to animate, still confirms it registered the input.
	/// </summary>
	private bool WasPressedThisFrame()
	{
		var input = hunter.Input;
		if (input == null) return false;

		return kind switch
		{
			AbilitySlotKind.Bite => input.AttackPressed,
			AbilitySlotKind.Eat => input.EatPressed,
			_ => ability switch
			{
				AbilityId.Dash => input.DashPressed,
				AbilityId.OpponentSearch => input.OpponentSearchPressed,
				AbilityId.Wings => input.ToggleFlyPressed,
				_ => false
			}
		};
	}

	private void AnimatePress()
	{
		if (pressElapsed < 0f) return;

		pressElapsed += Time.unscaledDeltaTime;
		float t = pressElapsed / pressDuration;

		if (t >= 1f)
		{
			pressElapsed = -1f;
			pressTarget.localScale = Vector3.one;
			return;
		}

		pressTarget.localScale = Vector3.one * Mathf.LerpUnclamped(pressScale, 1f, EaseOutBack(t));
	}

	/// <summary>Overshoots past 1 and settles back, which is what makes a tap feel like a tap.</summary>
	private static float EaseOutBack(float t)
	{
		const float overshoot = 1.70158f;
		float p = t - 1f;

		return 1f + (overshoot + 1f) * p * p * p + overshoot * p * p;
	}

	private bool Affordable()
	{
		if (hunter.Stamina == null) return true;

		return kind switch
		{
			AbilitySlotKind.Bite => Tuning == null || hunter.Stamina.CanAfford(Tuning.attackStaminaCost),
			AbilitySlotKind.Eat => true,
			_ => definition == null || hunter.Stamina.CanAfford(definition.staminaCost)
		};
	}

	// ---- Tooltip ----

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (bar != null) bar.ShowTooltip(this);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (bar != null) bar.HideTooltip(this);
	}

	private void OnDisable()
	{
		if (bar != null) bar.HideTooltip(this);

		// Otherwise the slot comes back mid-squash and stays that size.
		pressElapsed = -1f;
		if (pressTarget != null) pressTarget.localScale = Vector3.one;
	}

	/// <summary>
	/// The "how do I get this" line, in English, or null when there is nothing
	/// left to earn. Lives here rather than in the tooltip so the same sentence
	/// can be reused by a banner or a codex screen later.
	/// </summary>
	public string RequirementLine()
	{
		if (unlocked || definition == null) return null;

		int remaining = Remaining;
		string prey = definition.unlockedBy.ToString();

		return remaining == 1
			? $"Eat 1 more {prey}  ({Eaten}/{Required})"
			: $"Eat {remaining} more {prey}  ({Eaten}/{Required})";
	}

	/// <summary>The one-liner under the title: how the slot is fired once owned.</summary>
	public string ActivationLine() => activation switch
	{
		AbilityActivation.Automatic => "Engages by itself.",
		AbilityActivation.Passive => "Passive — always on once earned.",
		_ => null
	};

	/// <summary>
	/// Stamina and cooldown, in English, or null when the slot costs nothing.
	/// Built here rather than in the tooltip because only the slot knows whether
	/// to read PlayerTuning or an AbilityDefinition.
	/// </summary>
	public string CostLine()
	{
		var tuning = Tuning;

		return kind switch
		{
			AbilitySlotKind.Bite => tuning == null
				? null
				: Join($"{tuning.attackStaminaCost:0} stamina", $"{tuning.attackCooldown:0.#}s cooldown"),

			AbilitySlotKind.Eat => tuning == null
				? null
				: $"Restores {tuning.eatStaminaRestore:0} stamina",

			_ => definition == null
				? null
				: Join(
					definition.staminaCost > 0f ? $"{definition.staminaCost:0} stamina" : null,
					definition.staminaDrainPerSecond > 0f ? $"{definition.staminaDrainPerSecond:0}/s stamina" : null,
					definition.cooldown > 0f ? $"{definition.cooldown:0.#}s cooldown" : null)
		};
	}

	private static string Join(params string[] parts)
	{
		string joined = string.Join("  ·  ", parts.Where(p => !string.IsNullOrEmpty(p)));
		return joined.Length > 0 ? joined : null;
	}
}
