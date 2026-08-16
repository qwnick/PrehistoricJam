using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Start, loss, and restart. Kept deliberately small because the win condition
/// is still undecided — <see cref="Win"/> is the single seam to hook it into once
/// the team picks one.
///
/// Right now there is exactly one way to lose: running out of water in the
/// desert. Nothing attacks the hunter.
/// </summary>
public class GameFlow : MonoBehaviour
{
	public static GameFlow Instance { get; private set; }

	public bool IsOver { get; private set; }

	/// <summary>(reason, won) — for the end screen to render.</summary>
	public event Action<string, bool> RunEnded;

	private Hunter hunter;

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		hunter = Hunter.Instance;

		if (hunter != null && hunter.Water != null)
			hunter.Water.Emptied += HandleDiedOfThirst;
	}

	private void OnDestroy()
	{
		if (hunter != null && hunter.Water != null)
			hunter.Water.Emptied -= HandleDiedOfThirst;

		if (Instance == this) Instance = null;
	}

	private void Update()
	{
		if (!IsOver) return;

		// Unscaled input: the run is frozen with timeScale, but the keyboard is not.
		if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) Restart();
	}

	private void HandleDiedOfThirst() => Lose("Died of thirst");

	public void Lose(string reason) => End(reason, won: false);

	/// <summary>Placeholder until the team settles on a win condition.</summary>
	public void Win(string reason) => End(reason, won: true);

	private void End(string reason, bool won)
	{
		if (IsOver) return;

		IsOver = true;
		Time.timeScale = 0f;

		Debug.Log($"[GameFlow] Run ended — {reason}");
		RunEnded?.Invoke(reason, won);
	}

	public void Restart()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}
}
