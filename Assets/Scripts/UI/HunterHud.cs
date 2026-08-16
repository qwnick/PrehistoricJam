using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal run HUD: stamina, progress towards the next evolution, and a banner
/// when one lands. Reads the ability list from the config, so adding an ability
/// asset makes it show up here with no code change.
/// </summary>
public class HunterHud : MonoBehaviour
{
	[Header("Stamina")]
	[Tooltip("An Image set to Filled — its fillAmount is driven directly.")]
	[SerializeField] private Image staminaFill;

	[Header("Progress")]
	[SerializeField] private TMP_Text progressLabel;

	[Header("Unlock banner")]
	[SerializeField] private TMP_Text unlockBanner;
	[SerializeField] private float bannerDuration = 3f;

	private Hunter hunter;
	private float bannerHideTime;
	private readonly StringBuilder builder = new();

	private void Start()
	{
		hunter = Hunter.Instance;

		if (hunter == null)
		{
			Debug.LogError("[HunterHud] No Hunter in the scene.", this);
			enabled = false;
			return;
		}

		hunter.Kills.Changed += OnKillsChanged;
		hunter.Abilities.Unlocked += OnAbilityUnlocked;

		if (unlockBanner != null) unlockBanner.gameObject.SetActive(false);
		RefreshProgress();
	}

	private void OnDestroy()
	{
		if (hunter == null) return;

		hunter.Kills.Changed -= OnKillsChanged;
		hunter.Abilities.Unlocked -= OnAbilityUnlocked;
	}

	private void Update()
	{
		if (staminaFill != null) staminaFill.fillAmount = hunter.Stamina.Normalized;

		if (unlockBanner != null && unlockBanner.gameObject.activeSelf && Time.time >= bannerHideTime)
			unlockBanner.gameObject.SetActive(false);
	}

	private void OnKillsChanged(Species species, int total) => RefreshProgress();

	private void OnAbilityUnlocked(AbilityDefinition definition)
	{
		RefreshProgress();

		if (unlockBanner == null) return;

		unlockBanner.text = $"EVOLVED — {definition.displayName}";
		unlockBanner.gameObject.SetActive(true);
		bannerHideTime = Time.time + bannerDuration;
	}

	private void RefreshProgress()
	{
		if (progressLabel == null) return;

		builder.Clear();

		foreach (var definition in hunter.Config.abilities)
		{
			if (definition == null) continue;

			if (hunter.Abilities.Has(definition.id))
			{
				builder.AppendLine($"{definition.displayName} — unlocked");
				continue;
			}

			int eaten = hunter.Kills.EatenCount(definition.unlockedBy);
			builder.AppendLine($"{definition.unlockedBy} {eaten}/{definition.killsRequired} → {definition.displayName}");
		}

		progressLabel.text = builder.ToString();
	}
}
