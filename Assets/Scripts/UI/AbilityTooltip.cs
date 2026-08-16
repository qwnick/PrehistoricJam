using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The hover panel for an ability slot: what the ability is, how it is fired,
/// and — while it is still locked — which creature has to be eaten to earn it.
///
/// It builds its own widgets when none are assigned. The scene is edited by hand
/// by several people, so a HUD element that needs four inspector references is a
/// merge conflict waiting to happen; assign them only if you want a custom look.
/// </summary>
[DisallowMultipleComponent]
public class AbilityTooltip : MonoBehaviour
{
	[Header("Parts — leave empty to have them built in code")]
	[SerializeField] private RectTransform panel;
	[SerializeField] private TMP_Text titleLabel;
	[SerializeField] private TMP_Text bodyLabel;
	[SerializeField] private TMP_Text requirementLabel;

	[Header("Look")]
	[SerializeField] private float width = 320f;

	[Tooltip("Gap between the top of the slot and the bottom of the panel.")]
	[SerializeField] private float gap = 14f;

	[Tooltip("Smallest distance the panel keeps from the screen edge.")]
	[SerializeField] private float screenPadding = 12f;

	[SerializeField] private Color backgroundColor = new(0.07f, 0.09f, 0.07f, 0.95f);
	[SerializeField] private Color titleColor = new(1f, 0.93f, 0.66f);
	[SerializeField] private Color bodyColor = new(0.86f, 0.88f, 0.84f);
	[SerializeField] private Color lockedColor = new(1f, 0.55f, 0.45f);
	[SerializeField] private Color unlockedColor = new(0.6f, 0.9f, 0.55f);

	[Tooltip("Optional 9-sliced pixel-art frame. Falls back to a flat colour.")]
	[SerializeField] private Sprite backgroundSprite;

	private Canvas canvas;
	private RectTransform canvasRect;

	/// <summary>Built by <see cref="AbilityBar"/> the first time something is hovered.</summary>
	public static AbilityTooltip CreateUnder(Canvas parentCanvas)
	{
		if (parentCanvas == null)
		{
			Debug.LogError("[AbilityTooltip] The ability bar is not under a Canvas — no tooltip can be shown.");
			return null;
		}

		var go = new GameObject("AbilityTooltip", typeof(RectTransform));
		go.layer = parentCanvas.gameObject.layer;
		go.transform.SetParent(parentCanvas.transform, false);

		return go.AddComponent<AbilityTooltip>();
	}

	private void Awake()
	{
		canvas = GetComponentInParent<Canvas>();
		if (canvas != null) canvas = canvas.rootCanvas;
		canvasRect = canvas != null ? canvas.transform as RectTransform : null;

		if (panel == null) Build();

		Hide();
	}

	public void Show(AbilityButton slot)
	{
		if (slot == null || panel == null) return;

		titleLabel.text = slot.Title;
		titleLabel.color = slot.IsUnlocked ? titleColor : Color.Lerp(titleColor, Color.gray, 0.45f);

		bodyLabel.text = ComposeBody(slot);
		bodyLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(bodyLabel.text));

		string requirement = slot.RequirementLine();
		bool locked = !slot.IsUnlocked;

		requirementLabel.text = locked ? requirement : slot.IsInnate ? string.Empty : "Evolved";
		requirementLabel.color = locked ? lockedColor : unlockedColor;
		requirementLabel.gameObject.SetActive(!string.IsNullOrEmpty(requirementLabel.text));

		panel.gameObject.SetActive(true);

		// The panel has just changed height, so it has to be re-laid-out before
		// we can place it — otherwise it lands using last frame's size.
		LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
		PositionAbove(slot.transform as RectTransform);
	}

	public void Hide()
	{
		if (panel != null) panel.gameObject.SetActive(false);
	}

	private string ComposeBody(AbilityButton slot)
	{
		var parts = new System.Text.StringBuilder();

		string activation = slot.ActivationLine();
		if (!string.IsNullOrEmpty(activation)) parts.AppendLine(activation);

		if (!string.IsNullOrWhiteSpace(slot.Description)) parts.AppendLine(slot.Description);

		string cost = slot.CostLine();
		if (cost != null) parts.Append(cost);

		return parts.ToString().TrimEnd();
	}

	private void PositionAbove(RectTransform slot)
	{
		if (slot == null || canvasRect == null) return;

		var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

		var corners = new Vector3[4];
		slot.GetWorldCorners(corners);
		Vector2 topCentre = (corners[1] + corners[2]) * 0.5f;

		Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, topCentre);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, camera, out var local);

		// Pivot is bottom-centre, so y is simply the top of the slot plus the gap.
		float halfCanvas = canvasRect.rect.width * 0.5f;
		float halfPanel = panel.rect.width * 0.5f;
		float limit = Mathf.Max(0f, halfCanvas - halfPanel - screenPadding);

		panel.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -limit, limit), local.y + gap);
	}

	// ---- Built in code so the scene needs no wiring ----

	private void Build()
	{
		var self = (RectTransform)transform;
		self.anchorMin = self.anchorMax = new Vector2(0.5f, 0.5f);
		self.pivot = new Vector2(0.5f, 0.5f);
		self.anchoredPosition = Vector2.zero;
		self.sizeDelta = Vector2.zero;
		transform.SetAsLastSibling();

		var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
		panelGo.layer = gameObject.layer;
		panelGo.transform.SetParent(transform, false);

		panel = (RectTransform)panelGo.transform;
		panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
		panel.pivot = new Vector2(0.5f, 0f);
		panel.sizeDelta = new Vector2(width, 0f);

		// The panel must never swallow the pointer: hovering it would count as
		// leaving the slot, and the tooltip would strobe.
		var group = panelGo.GetComponent<CanvasGroup>();
		group.blocksRaycasts = false;
		group.interactable = false;

		var background = panelGo.GetComponent<Image>();
		background.sprite = backgroundSprite;
		background.type = backgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
		background.color = backgroundColor;
		background.raycastTarget = false;

		var layout = panelGo.AddComponent<VerticalLayoutGroup>();
		layout.padding = new RectOffset(14, 14, 12, 12);
		layout.spacing = 6f;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;

		var fitter = panelGo.AddComponent<ContentSizeFitter>();
		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		titleLabel = BuildLabel("Title", 26f, FontStyles.Bold, titleColor);
		bodyLabel = BuildLabel("Body", 18f, FontStyles.Normal, bodyColor);
		requirementLabel = BuildLabel("Requirement", 18f, FontStyles.Bold, lockedColor);
	}

	private TMP_Text BuildLabel(string labelName, float size, FontStyles style, Color colour)
	{
		var go = new GameObject(labelName, typeof(RectTransform));
		go.layer = gameObject.layer;
		go.transform.SetParent(panel, false);

		var label = go.AddComponent<TextMeshProUGUI>();
		label.fontSize = size;
		label.fontStyle = style;
		label.color = colour;
		label.alignment = TextAlignmentOptions.TopLeft;
		label.raycastTarget = false;

		if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;

		return label;
	}
}
