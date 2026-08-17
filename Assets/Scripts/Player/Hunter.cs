using System.Reflection;
using UnityEngine;

/// <summary>
/// The player snake. Owns the config, the input, and the sub-components, and
/// initialises them in an explicit order so nothing depends on Unity's Awake
/// ordering. Everything else on the player reaches its siblings through here.
///
/// The static Instance is a deliberate shortcut: enemy AI needs the hunter every
/// frame and there is exactly one, so a lookup service would be ceremony.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Stamina))]
[RequireComponent(typeof(AbilityInventory))]
[RequireComponent(typeof(KillTracker))]
public class Hunter : MonoBehaviour
{
	public Animator animator;
	public static Hunter Instance { get; private set; }

	[Tooltip("The one asset every tunable number comes from.")]
	[SerializeField] private GameConfig config;

	public GameConfig Config => config;
	public PlayerTuning Tuning => config.player;

	public InputReader Input { get; private set; }
	public Stamina Stamina { get; private set; }
	public AbilityInventory Abilities { get; private set; }
	public KillTracker Kills { get; private set; }
	public HunterMovement Movement { get; private set; }
	public HunterCombat Combat { get; private set; }

	/// <summary>Optional until the desert exists — null is a valid state.</summary>
	public WaterMeter Water { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogError($"[Hunter] A second hunter ('{name}') was spawned; destroying it.", this);
			Destroy(gameObject);
			return;
		}

		Instance = this;

		if (!ValidateConfig()) return;

		Stamina = GetComponent<Stamina>();
		Abilities = GetComponent<AbilityInventory>();
		Kills = GetComponent<KillTracker>();
		Movement = GetComponent<HunterMovement>();
		Combat = GetComponent<HunterCombat>();
		Water = GetComponent<WaterMeter>();

		Stamina.Configure(Tuning.maxStamina, Tuning.staminaRegenPerSecond, Tuning.staminaRegenDelay);
		Abilities.Initialize(config);
		Kills.Initialize(config, Abilities);

		// Abilities read this in Start, so it has to be ready before then.
		if (Water != null) Water.Initialize(Tuning);

		Input = new InputReader(config.input);
	}

	private bool ValidateConfig()
	{
		if (config == null)
		{
			Debug.LogError("[Hunter] No GameConfig assigned — the hunter cannot run.", this);
			enabled = false;
			return false;
		}

		if (config.player == null)
		{
			Debug.LogError("[Hunter] The GameConfig has no PlayerTuning assigned.", config);
			enabled = false;
			return false;
		}

		return true;
	}

	private void OnEnable() => Input?.Enable();
	private void OnDisable() => Input?.Disable();
	public void PlayAttack()
	{
		animator.SetTrigger("Attack");
	}

	private void OnDestroy()
	{
		if (Instance == this) Instance = null;
		Input?.Dispose();
	}
}
