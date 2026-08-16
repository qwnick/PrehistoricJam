using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the whole config asset tree in one click, pre-filled with the values
/// from the design doc, and wires everything into a single GameConfig.
///
/// Idempotent and non-destructive: assets that already exist are reused and their
/// values are left alone, so running this never overwrites a designer's tuning.
/// It only fills in what is missing.
/// </summary>
public static class ConfigBootstrap
{
	private const string Root = "Assets/Config";
	private const string EnemiesFolder = Root + "/Enemies";
	private const string AbilitiesFolder = Root + "/Abilities";
	private const string ZonesFolder = Root + "/Zones";

	[MenuItem("PrehistoricJam/Setup/Create Default Config Assets")]
	public static void CreateDefaults()
	{
		EnsureFolder(Root);
		EnsureFolder(EnemiesFolder);
		EnsureFolder(AbilitiesFolder);
		EnsureFolder(ZonesFolder);

		var player = LoadOrCreate<PlayerTuning>($"{Root}/PlayerTuning.asset", out _);
		var input = LoadOrCreate<InputBindings>($"{Root}/InputBindings.asset", out _);

		var enemies = new List<EnemyTuning>
		{
			CreateEnemy(Species.Velociraptor),
			CreateEnemy(Species.Pterosaur),
			CreateEnemy(Species.Crocodile),
			CreateEnemy(Species.Camelsaur),
			CreateEnemy(Species.Vulturesaur)
		};

		var abilities = new List<AbilityDefinition>
		{
			CreateAbility(AbilityId.Dash),
			CreateAbility(AbilityId.OpponentSearch),
			CreateAbility(AbilityId.Swim),
			CreateAbility(AbilityId.WaterStorage),
			CreateAbility(AbilityId.Wings)
		};

		var zones = new List<ZoneTuning>
		{
			CreateZone(ZoneType.Forest),
			CreateZone(ZoneType.River),
			CreateZone(ZoneType.Desert),
			CreateZone(ZoneType.Rocks)
		};

		var config = LoadOrCreate<GameConfig>($"{Root}/GameConfig.asset", out _);
		config.player = player;
		config.input = input;
		config.enemies = enemies;
		config.abilities = abilities;
		config.zones = zones;
		EditorUtility.SetDirty(config);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Selection.activeObject = config;
		EditorGUIUtility.PingObject(config);
		Debug.Log($"[ConfigBootstrap] GameConfig ready at {Root}/GameConfig.asset", config);
	}

	// ---- Enemies. Values come straight from the design doc; see docs/PLAN.md ----

	private static EnemyTuning CreateEnemy(Species species)
	{
		var asset = LoadOrCreate<EnemyTuning>($"{EnemiesFolder}/{species}.asset", out bool created);
		if (!created) return asset;

		asset.species = species;
		asset.displayName = species.ToString();

		switch (species)
		{
			// "run slightly slower than the snake" + "enough stamina for 3 dashes,
			// restores FAST during rest" — the entire tutorial enemy.
			case Species.Velociraptor:
				asset.health = 2f;
				asset.walkSpeedFactor = 0.35f;
				asset.runSpeedFactor = 0.9f;
				asset.fleeRadiusInDashes = 1.2f;
				asset.calmRadiusInDashes = 2f;
				asset.usesStamina = true;
				asset.maxStamina = 3f;
				asset.staminaRegenPerSecond = 1.5f;
				asset.staminaRegenDelay = 1.2f;
				asset.canDash = true;
				asset.dashTriggerRadiusInDashes = 0.6f;
				asset.dashDistance = 3f;
				asset.dashDuration = 0.2f;
				asset.dashCooldown = 0.8f;
				asset.dashStaminaCost = 1f;
				asset.wanderRadius = 4f;
				asset.navDomain = NavDomain.Land;
				break;

			// No stamina at all: it sits still, then flees at full speed forever.
			// Flee radius must sit between the snake's attack radius and its dash
			// distance, which is what makes the dash the only way to reach it.
			case Species.Pterosaur:
				asset.health = 2f;
				asset.walkSpeedFactor = 0.15f;
				asset.runSpeedFactor = 2f;
				asset.fleeRadiusInDashes = 0.7f;
				asset.calmRadiusInDashes = 1.5f;
				asset.usesStamina = false;
				asset.canDash = false;
				asset.wanderRadius = 2f;
				asset.navDomain = NavDomain.Air;
				asset.fleeDistanceInDashes = 10f;   // "flies to the other side of the zone"
				break;

			// "Swim speed same as snake movement speed", rests on shore for a
			// minimum of two seconds before diving again.
			case Species.Crocodile:
				asset.health = 3f;
				asset.walkSpeedFactor = 0.3f;
				asset.runSpeedFactor = 1f;
				asset.fleeRadiusInDashes = 2f;
				asset.calmRadiusInDashes = 3f;
				asset.usesStamina = true;
				asset.maxStamina = 6f;
				asset.staminaRegenPerSecond = 3f;
				asset.staminaRegenDelay = 0.5f;
				asset.wanderRadius = 3f;
				asset.navDomain = NavDomain.Amphibious;
				asset.staminaDrainPerSecond = 1f;   // burns while swimming
				asset.shoreRestSeconds = 2f;
				asset.fleeDistanceInDashes = 4f;
				break;

			// Genuinely faster than the snake and spooked from six dashes away —
			// it cannot be chased at all, only ambushed from the water.
			case Species.Camelsaur:
				asset.health = 3f;
				asset.walkSpeedFactor = 0.3f;
				asset.runSpeedFactor = 1.3f;
				asset.fleeRadiusInDashes = 6f;
				asset.calmRadiusInDashes = 7f;
				asset.usesStamina = true;
				asset.maxStamina = 8f;
				asset.staminaRegenPerSecond = 1f;
				asset.staminaRegenDelay = 2f;
				asset.wanderRadius = 5f;
				asset.navDomain = NavDomain.Land;
				asset.staminaDrainPerSecond = 0.5f; // bleeds slowly away from water
				asset.drinkSeconds = 6f;
				asset.nearWaterDistance = 2f;
				asset.fleeDistanceInDashes = 8f;
				break;

			// "3-4 flights" of stamina, refilled slowly on the ground (~20s) or
			// fast off a corpse — which is the hook the player exploits.
			case Species.Vulturesaur:
				asset.health = 2f;
				asset.walkSpeedFactor = 0.2f;
				asset.runSpeedFactor = 1.1f;
				asset.fleeRadiusInDashes = 3f;
				asset.calmRadiusInDashes = 4f;
				asset.usesStamina = true;
				asset.maxStamina = 4f;
				asset.staminaRegenPerSecond = 0.2f;
				asset.staminaRegenDelay = 1f;
				asset.corpseNutrition = 4f;
				asset.wanderRadius = 3f;
				asset.navDomain = NavDomain.Air;
				asset.flightStaminaCost = 1f;       // 4 stamina => 4 take-offs
				asset.corpseEatSeconds = 6f;
				asset.lowStaminaThreshold = 0.5f;
				asset.fleeDistanceInDashes = 5f;
				break;
		}

		EditorUtility.SetDirty(asset);
		return asset;
	}

	// ---- Zones ----

	private static ZoneTuning CreateZone(ZoneType type)
	{
		var asset = LoadOrCreate<ZoneTuning>($"{ZonesFolder}/{type}.asset", out bool created);
		if (!created) return asset;

		asset.type = type;
		asset.displayName = type.ToString();

		switch (type)
		{
			case ZoneType.Forest:
				break;

			case ZoneType.River:
				asset.gated = true;
				asset.requiredAbility = AbilityId.Swim;
				break;

			// The one place that can kill you. Drain is sized so that crossing is
			// survivable only once Water Storage has tripled the tank.
			case ZoneType.Desert:
				asset.waterDrainPerSecond = 2f;
				asset.gated = true;
				asset.requiredAbility = AbilityId.WaterStorage;
				break;

			case ZoneType.Rocks:
				asset.gated = true;
				asset.requiredAbility = AbilityId.Wings;
				break;
		}

		EditorUtility.SetDirty(asset);
		return asset;
	}

	// ---- Abilities ----

	private static AbilityDefinition CreateAbility(AbilityId id)
	{
		var asset = LoadOrCreate<AbilityDefinition>($"{AbilitiesFolder}/{id}.asset", out bool created);
		if (!created) return asset;

		asset.id = id;
		asset.killsRequired = 3;

		switch (id)
		{
			case AbilityId.Dash:
				asset.displayName = "Dash";
				asset.description = "A burst of speed. The only way to close on prey that outruns you.";
				asset.unlockedBy = Species.Velociraptor;
				asset.staminaCost = 30f;
				asset.cooldown = 0.4f;
				break;

			case AbilityId.OpponentSearch:
				asset.displayName = "Echolocation";
				asset.description = "Reveals nearby prey, including anything hidden underwater.";
				asset.unlockedBy = Species.Pterosaur;
				asset.staminaCost = 25f;
				asset.cooldown = 60f;
				asset.range = 12f;      // ~3 dashes: has to reach across the river
				asset.duration = 5f;    // long enough to line up a dash on what it finds
				break;

			case AbilityId.Swim:
				asset.displayName = "Swim";
				asset.description = "Cross the river and hunt from the water.";
				asset.unlockedBy = Species.Crocodile;
				asset.staminaDrainPerSecond = 10f;
				break;

			case AbilityId.WaterStorage:
				asset.displayName = "Water Storage";
				asset.description = "Carry enough water to survive the desert crossing.";
				asset.unlockedBy = Species.Camelsaur;
				asset.isPassive = true;
				break;

			case AbilityId.Wings:
				asset.displayName = "Wings";
				asset.description = "Reach the rocks.";
				asset.unlockedBy = Species.Vulturesaur;
				asset.staminaDrainPerSecond = 15f;
				break;
		}

		EditorUtility.SetDirty(asset);
		return asset;
	}

	// ---- Plumbing ----

	private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
	{
		var existing = AssetDatabase.LoadAssetAtPath<T>(path);
		if (existing != null)
		{
			created = false;
			return existing;
		}

		var asset = ScriptableObject.CreateInstance<T>();
		AssetDatabase.CreateAsset(asset, path);
		created = true;
		return asset;
	}

	private static void EnsureFolder(string path)
	{
		if (AssetDatabase.IsValidFolder(path)) return;

		string parent = Path.GetDirectoryName(path).Replace('\\', '/');
		string leaf = Path.GetFileName(path);

		EnsureFolder(parent);
		AssetDatabase.CreateFolder(parent, leaf);
	}
}
