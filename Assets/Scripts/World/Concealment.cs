using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes something invisible until echolocation finds it. The Crocodile is the
/// reason this exists: it is unhittable while submerged, so echolocation is not a
/// convenience there — it is the only way that hunt is possible at all.
/// </summary>
public class Concealment : MonoBehaviour
{
	/// <summary>Everything that can currently be hiding. Echolocation searches this.</summary>
	public static readonly List<Concealment> All = new();

	[Tooltip("Left empty, every SpriteRenderer under this object is used.")]
	[SerializeField] private SpriteRenderer[] renderers;

	private bool concealed;
	private float revealEndTime;

	/// <summary>Hiding right now and not currently revealed.</summary>
	public bool IsHidden => concealed && Time.time >= revealEndTime;

	private void Awake()
	{
		if (renderers == null || renderers.Length == 0)
			renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
	}

	private void OnEnable() => All.Add(this);
	private void OnDisable() => All.Remove(this);

	/// <summary>Driven by AI — e.g. the crocodile conceals itself while submerged.</summary>
	public void SetConcealed(bool value)
	{
		concealed = value;
		Apply();
	}
	[ContextMenu("hide")]
	public void Hide()
	{
		SetConcealed(true);
	}

	/// <summary>Forces it visible for a while, however it is behaving.</summary>
	public void Reveal(float duration)
	{
		revealEndTime = Mathf.Max(revealEndTime, Time.time + duration);
		Apply();
	}

	private void Update()
	{
		Apply();
	}

	private void Apply()
	{
		bool visible = !IsHidden;

		foreach (var renderer in renderers)
		{
			if (renderer != null) renderer.enabled = visible;
		}
	}
}
