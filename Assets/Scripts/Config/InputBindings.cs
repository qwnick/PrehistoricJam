using UnityEngine;

/// <summary>
/// Key layout as data. Values are Input System control paths, e.g.
/// "&lt;Keyboard&gt;/w", "&lt;Keyboard&gt;/1", "&lt;Gamepad&gt;/buttonSouth".
///
/// We deliberately do NOT use a .inputactions asset: that would need wiring in the
/// scene, and the scene is edited by hand by several people at once. This keeps
/// rebinding to a single asset with no merge conflicts.
/// </summary>
[CreateAssetMenu(fileName = "InputBindings", menuName = "PrehistoricJam/Input Bindings")]
public class InputBindings : ScriptableObject
{
	[Header("Movement — tank controls")]
	[Tooltip("W/S drive forward and back along the body.")]
	public string[] forward = { "<Keyboard>/w", "<Keyboard>/upArrow" };
	public string[] backward = { "<Keyboard>/s", "<Keyboard>/downArrow" };

	[Tooltip("A/D turn the body rather than strafing.")]
	public string[] turnLeft = { "<Keyboard>/a", "<Keyboard>/leftArrow" };
	public string[] turnRight = { "<Keyboard>/d", "<Keyboard>/rightArrow" };

	[Header("Actions — number keys")]
	public string[] attack = { "<Keyboard>/1" };
	public string[] dash = { "<Keyboard>/2" };
	public string[] opponentSearch = { "<Keyboard>/3" };
	public string[] eat = { "<Keyboard>/4" };
	public string[] toggleFly = { "<Keyboard>/5" };
}
