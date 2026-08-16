using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

/// <summary>
/// Wires Buttons.prefab up to the ability system: one AbilityButton per slot,
/// the right AbilityId on each, control paths taken from the InputBindings asset,
/// and a counter label plus a cooldown overlay on every button.
///
/// Done from a menu item rather than by hand because the scene and the prefab are
/// edited by several people at once — seven buttons wired by hand is seven chances
/// to bind the wrong ability, and the mistake only shows up in a playtest.
/// Re-running it is safe: everything it does is idempotent.
/// </summary>
public static class AbilityBarWizard
{
	private const string PrefabPath = "Assets/Prefabs/Buttons.prefab";
	private const string BindingsPath = "Assets/Config/InputBindings.asset";
	private const string IconFolder = "Assets/Sprites/Buttons";

	/// <summary>What one slot of the bar is, keyed by the sprite already on its Icon.</summary>
	private readonly struct Slot
	{
		public readonly string IconSprite;
		public readonly AbilitySlotKind Kind;
		public readonly AbilityId Ability;
		public readonly AbilityActivation Activation;
		public readonly string InnateTitle;
		public readonly string InnateDescription;

		public Slot(string iconSprite, AbilityId ability, AbilityActivation activation)
		{
			IconSprite = iconSprite;
			Kind = AbilitySlotKind.Ability;
			Ability = ability;
			Activation = activation;
			InnateTitle = null;
			InnateDescription = null;
		}

		public Slot(string iconSprite, AbilitySlotKind innateKind, string title, string description)
		{
			IconSprite = iconSprite;
			Kind = innateKind;
			Ability = default;
			Activation = AbilityActivation.Press;
			InnateTitle = title;
			InnateDescription = description;
		}
	}

	/// <summary>
	/// In bar order. Matched by icon sprite first so re-ordering the buttons in the
	/// hierarchy cannot silently rebind them; sibling order is only the fallback.
	/// </summary>
	private static readonly Slot[] Slots =
	{
		new("Gemini_Generated_Image_z6q0dkz6q0dkz6q0", AbilitySlotKind.Bite, "Bite",
			"Your only attack, a cone in front of the head. Tank controls mean you commit to a facing before you strike."),
		new("Gemini_Generated_Image_kkpgpxkkpgpxkkpg", AbilitySlotKind.Eat, "Eat",
			"Swallow a corpse. Only eating counts towards evolution — a kill on its own earns you nothing."),
		new("Leg", AbilityId.Dash, AbilityActivation.Press),
		new("Echo", AbilityId.OpponentSearch, AbilityActivation.Press),
		new("Tail", AbilityId.Swim, AbilityActivation.Automatic),
		new("Hump", AbilityId.WaterStorage, AbilityActivation.Passive),
		new("Wing", AbilityId.Wings, AbilityActivation.Press)
	};

	[MenuItem("PrehistoricJam/UI/Wire Ability Bar")]
	private static void Wire()
	{
		var bindings = AssetDatabase.LoadAssetAtPath<InputBindings>(BindingsPath);

		if (bindings == null)
		{
			Debug.LogError($"[AbilityBarWizard] No InputBindings at '{BindingsPath}'.");
			return;
		}

		var root = PrefabUtility.LoadPrefabContents(PrefabPath);

		if (root == null)
		{
			Debug.LogError($"[AbilityBarWizard] No prefab at '{PrefabPath}'.");
			return;
		}

		try
		{
			EnsureComponent<AbilityBar>(root);

			var children = Enumerable.Range(0, root.transform.childCount)
				.Select(i => root.transform.GetChild(i).gameObject)
				.ToList();

			for (int i = 0; i < children.Count; i++)
			{
				var child = children[i];

				if (!TryResolveSlot(child, i, out var slot))
				{
					Debug.LogWarning($"[AbilityBarWizard] '{child.name}' matches no known slot — left alone.");
					continue;
				}

				WireSlot(child, slot, bindings);
			}

			PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			Debug.Log($"[AbilityBarWizard] Wired {children.Count} slots in '{PrefabPath}'.");
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	private static bool TryResolveSlot(GameObject button, int index, out Slot slot)
	{
		string iconSprite = IconSpriteName(button);

		foreach (var candidate in Slots)
		{
			if (candidate.IconSprite != iconSprite) continue;

			slot = candidate;
			return true;
		}

		if (index < Slots.Length)
		{
			slot = Slots[index];
			Debug.LogWarning($"[AbilityBarWizard] '{button.name}' has an unrecognised icon; " +
				$"falling back to bar position {index} ({slot.IconSprite}).");
			return true;
		}

		slot = default;
		return false;
	}

	private static string IconSpriteName(GameObject button)
	{
		var iconTransform = button.transform.Find("Icon");
		var image = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

		return image != null && image.sprite != null ? image.sprite.name : null;
	}

	private static void WireSlot(GameObject button, Slot slot, InputBindings bindings)
	{
		var icon = button.transform.Find("Icon")?.GetComponent<Image>();
		var counter = ConfigureCounter(button);
		var cooldown = EnsureCooldownOverlay(button, icon);
		var onScreen = button.GetComponent<OnScreenButton>();

		if (onScreen != null) ApplyControlPath(onScreen, slot, bindings);

		var slotComponent = EnsureComponent<AbilityButton>(button);
		var serialized = new SerializedObject(slotComponent);

		serialized.FindProperty("kind").enumValueIndex = (int)slot.Kind;
		serialized.FindProperty("ability").enumValueIndex = (int)slot.Ability;
		serialized.FindProperty("activation").enumValueIndex = (int)slot.Activation;
		serialized.FindProperty("innateTitle").stringValue = slot.InnateTitle ?? string.Empty;
		serialized.FindProperty("innateDescription").stringValue = slot.InnateDescription ?? string.Empty;
		serialized.FindProperty("icon").objectReferenceValue = icon;
		serialized.FindProperty("cooldownFill").objectReferenceValue = cooldown;
		serialized.FindProperty("counterLabel").objectReferenceValue = counter;
		serialized.FindProperty("button").objectReferenceValue = button.GetComponent<Button>();
		serialized.FindProperty("onScreenButton").objectReferenceValue = onScreen;

		serialized.ApplyModifiedPropertiesWithoutUndo();

		// Rename so the hierarchy says what each button is instead of "Button (4)".
		button.name = slot.Kind == AbilitySlotKind.Ability ? slot.Ability.ToString() : slot.InnateTitle;
	}

	/// <summary>
	/// Non-Press slots get their path cleared: Swim engages by itself and Water
	/// Storage is passive, so a stray tap must not fake a key press.
	/// </summary>
	private static void ApplyControlPath(OnScreenButton onScreen, Slot slot, InputBindings bindings)
	{
		string path = slot.Activation != AbilityActivation.Press
			? string.Empty
			: First(PathsFor(slot, bindings));

		if (slot.Activation == AbilityActivation.Press && string.IsNullOrEmpty(path))
		{
			Debug.LogWarning($"[AbilityBarWizard] No binding for '{onScreen.name}' in the InputBindings asset.");
			return;
		}

		var serialized = new SerializedObject(onScreen);
		serialized.FindProperty("m_ControlPath").stringValue = path;
		serialized.ApplyModifiedPropertiesWithoutUndo();

		// AbilityButton re-enables it the moment the ability is unlocked.
		onScreen.enabled = slot.Activation == AbilityActivation.Press;
	}

	private static string[] PathsFor(Slot slot, InputBindings bindings) => slot.Kind switch
	{
		AbilitySlotKind.Bite => bindings.attack,
		AbilitySlotKind.Eat => bindings.eat,
		_ => slot.Ability switch
		{
			AbilityId.Dash => bindings.dash,
			AbilityId.OpponentSearch => bindings.opponentSearch,
			AbilityId.Wings => bindings.toggleFly,
			_ => null
		}
	};

	private static string First(IEnumerable<string> paths)
		=> paths?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p)) ?? string.Empty;

	/// <summary>Re-uses the placeholder "Text (TMP)" child every button already has.</summary>
	private static TMP_Text ConfigureCounter(GameObject button)
	{
		var label = button.transform.Find("Text (TMP)")?.GetComponent<TMP_Text>();

		if (label == null)
		{
			var go = new GameObject("Counter", typeof(RectTransform));
			go.layer = button.layer;
			go.transform.SetParent(button.transform, false);

			var rect = (RectTransform)go.transform;
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = rect.offsetMax = Vector2.zero;

			label = go.AddComponent<TextMeshProUGUI>();
		}

		label.gameObject.name = "Counter";
		label.text = string.Empty;
		label.fontSize = 26f;
		label.fontStyle = FontStyles.Bold;
		label.color = Color.white;
		label.alignment = TextAlignmentOptions.BottomRight;
		label.margin = new Vector4(0f, 0f, 8f, 6f);
		label.raycastTarget = false;
		label.transform.SetAsLastSibling();

		return label;
	}

	/// <summary>A radial wipe over the icon. Hidden until the ability is on cooldown.</summary>
	private static Image EnsureCooldownOverlay(GameObject button, Image icon)
	{
		var existing = button.transform.Find("Cooldown");

		if (existing == null)
		{
			var go = new GameObject("Cooldown", typeof(RectTransform));
			go.layer = button.layer;
			go.transform.SetParent(button.transform, false);
			go.AddComponent<Image>();
			existing = go.transform;
		}

		var rect = (RectTransform)existing;
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = new Vector2(6f, 6f);
		rect.offsetMax = new Vector2(-6f, -6f);

		var overlay = existing.GetComponent<Image>();
		overlay.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
		overlay.type = Image.Type.Filled;
		overlay.fillMethod = Image.FillMethod.Radial360;
		overlay.fillOrigin = (int)Image.Origin360.Top;
		overlay.fillClockwise = false;
		overlay.color = new Color(0f, 0f, 0f, 0.6f);
		overlay.raycastTarget = false;

		// Above the icon, below the counter, which ConfigureCounter pushes last.
		if (icon != null) existing.SetSiblingIndex(icon.transform.GetSiblingIndex() + 1);

		existing.gameObject.SetActive(false);
		return overlay;
	}

	private static T EnsureComponent<T>(GameObject target) where T : Component
	{
		var existing = target.GetComponent<T>();
		return existing != null ? existing : target.AddComponent<T>();
	}

	// ---- Ability assets ----

	[MenuItem("PrehistoricJam/UI/Fill Ability Icons and Descriptions")]
	private static void FillAbilityAssets()
	{
		var icons = new Dictionary<AbilityId, string>
		{
			{ AbilityId.Dash, "Leg" },
			{ AbilityId.OpponentSearch, "Echo" },
			{ AbilityId.Swim, "Tail" },
			{ AbilityId.WaterStorage, "Hump" },
			{ AbilityId.Wings, "Wing" }
		};

		// Only used where the asset has none — never overwrites a designer's text.
		var descriptions = new Dictionary<AbilityId, string>
		{
			{ AbilityId.OpponentSearch,
				"A pulse that forces every hidden creature in range to show itself for a few seconds. " +
				"The crocodile is invisible while submerged, so the river hunt is impossible without it." },
			{ AbilityId.WaterStorage,
				"A second stomach for water. It does not refill any faster, it simply holds far more — " +
				"which is the whole difference between crossing the desert and dying in it." }
		};

		foreach (var guid in AssetDatabase.FindAssets("t:AbilityDefinition"))
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			var definition = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
			if (definition == null) continue;

			bool changed = false;

			if (definition.icon == null && icons.TryGetValue(definition.id, out string spriteName))
			{
				var sprite = LoadSprite(spriteName);

				if (sprite != null)
				{
					definition.icon = sprite;
					changed = true;
				}
			}

			if (string.IsNullOrWhiteSpace(definition.description)
				&& descriptions.TryGetValue(definition.id, out string text))
			{
				definition.description = text;
				changed = true;
			}

			if (!changed) continue;

			EditorUtility.SetDirty(definition);
			Debug.Log($"[AbilityBarWizard] Updated '{definition.name}'.", definition);
		}

		AssetDatabase.SaveAssets();
	}

	private static Sprite LoadSprite(string spriteName)
	{
		string path = $"{IconFolder}/{spriteName}.png";

		var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

		if (sprite == null) Debug.LogWarning($"[AbilityBarWizard] No sprite at '{path}'.");

		return sprite;
	}
}
