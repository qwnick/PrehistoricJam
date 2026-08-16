using UnityEditor;
using UnityEngine;

/// <summary>
/// Idempotent project bootstrap: makes sure the layers and tags the gameplay
/// code expects actually exist. Runs by itself when the editor loads, so nobody
/// on the team has to remember a setup step after pulling. The menu item is just
/// a manual fallback.
///
/// This is an editor script rather than a hand-edited TagManager.asset because
/// the editor keeps that file in memory and overwrites edits made on disk while
/// it is open.
/// </summary>
[InitializeOnLoad]
public static class ProjectSetup
{
	private static readonly string[] RequiredLayers = { "Player", "Enemy", "Corpse", "Obstacle" };
	private static readonly string[] RequiredTags = { "Player", "Enemy", "Corpse" };

	// Layers 0-7 are reserved by Unity; user layers start at 8.
	private const int FirstUserLayer = 8;

	static ProjectSetup()
	{
		// Deferred: the asset database is not ready during static construction.
		EditorApplication.delayCall += () => Run(verbose: false);
	}

	[MenuItem("PrehistoricJam/Setup/Ensure Layers and Tags")]
	private static void RunFromMenu() => Run(verbose: true);

	private static void Run(bool verbose)
	{
		var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
		if (assets == null || assets.Length == 0) return;

		var tagManager = new SerializedObject(assets[0]);

		int added = EnsureLayers(tagManager) + EnsureTags(tagManager);

		if (added > 0)
		{
			tagManager.ApplyModifiedProperties();
			AssetDatabase.SaveAssets();
			Debug.Log($"[ProjectSetup] Added {added} missing layer/tag entries.");
		}
		else if (verbose)
		{
			Debug.Log("[ProjectSetup] Layers and tags already up to date.");
		}
	}

	private static int EnsureLayers(SerializedObject tagManager)
	{
		var layers = tagManager.FindProperty("layers");
		int added = 0;

		foreach (var name in RequiredLayers)
		{
			if (Contains(layers, name)) continue;

			int slot = FindFreeLayerSlot(layers);
			if (slot < 0)
			{
				Debug.LogError($"[ProjectSetup] No free user layer left for '{name}'.");
				continue;
			}

			layers.GetArrayElementAtIndex(slot).stringValue = name;
			added++;
		}

		return added;
	}

	private static int EnsureTags(SerializedObject tagManager)
	{
		var tags = tagManager.FindProperty("tags");
		int added = 0;

		foreach (var name in RequiredTags)
		{
			if (Contains(tags, name)) continue;

			tags.InsertArrayElementAtIndex(tags.arraySize);
			tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = name;
			added++;
		}

		return added;
	}

	private static bool Contains(SerializedProperty array, string name)
	{
		for (int i = 0; i < array.arraySize; i++)
			if (array.GetArrayElementAtIndex(i).stringValue == name) return true;
		return false;
	}

	private static int FindFreeLayerSlot(SerializedProperty layers)
	{
		for (int i = FirstUserLayer; i < layers.arraySize; i++)
			if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) return i;
		return -1;
	}
}
