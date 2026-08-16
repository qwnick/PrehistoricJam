using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Builds every player action from an <see cref="InputBindings"/> asset and
/// exposes them as plain properties. Gameplay code asks this class what the
/// player is doing and never touches the Input System directly, so rebinding is
/// a data change and adding a device is a one-file change.
/// </summary>
public class InputReader : IDisposable
{
	private readonly InputAction move;
	private readonly InputAction attack;
	private readonly InputAction dash;
	private readonly InputAction opponentSearch;
	private readonly InputAction eat;
	private readonly InputAction toggleFly;

	/// <summary>x = turn (A/D), y = forward/back (W/S). Tank controls, so x is rotation, not strafe.</summary>
	public Vector2 Move => move.ReadValue<Vector2>();

	public float Turn => Move.x;
	public float Throttle => Move.y;

	public bool AttackPressed => attack.WasPressedThisFrame();
	public bool DashPressed => dash.WasPressedThisFrame();
	public bool OpponentSearchPressed => opponentSearch.WasPressedThisFrame();
	public bool EatPressed => eat.WasPressedThisFrame();
	public bool ToggleFlyPressed => toggleFly.WasPressedThisFrame();

	public InputReader(InputBindings bindings)
	{
		if (bindings == null)
			throw new ArgumentNullException(nameof(bindings), "Assign an InputBindings asset in the GameConfig.");

		move = BuildMoveAction(bindings);
		attack = BuildButton(nameof(attack), bindings.attack);
		dash = BuildButton(nameof(dash), bindings.dash);
		opponentSearch = BuildButton(nameof(opponentSearch), bindings.opponentSearch);
		eat = BuildButton(nameof(eat), bindings.eat);
		toggleFly = BuildButton(nameof(toggleFly), bindings.toggleFly);
	}

	public void Enable()
	{
		move.Enable();
		attack.Enable();
		dash.Enable();
		opponentSearch.Enable();
		eat.Enable();
		toggleFly.Enable();
	}

	public void Disable()
	{
		move.Disable();
		attack.Disable();
		dash.Disable();
		opponentSearch.Disable();
		eat.Disable();
		toggleFly.Disable();
	}

	public void Dispose()
	{
		move.Dispose();
		attack.Dispose();
		dash.Dispose();
		opponentSearch.Dispose();
		eat.Dispose();
		toggleFly.Dispose();
	}

	private static InputAction BuildMoveAction(InputBindings bindings)
	{
		var action = new InputAction("move", InputActionType.Value, expectedControlType: "Vector2");

		// CompositeSyntax is a struct, so every With() must be assigned back.
		var composite = action.AddCompositeBinding("2DVector");
		composite = AddToComposite(composite, "Up", bindings.forward);
		composite = AddToComposite(composite, "Down", bindings.backward);
		composite = AddToComposite(composite, "Left", bindings.turnLeft);
		AddToComposite(composite, "Right", bindings.turnRight);

		return action;
	}

	private static InputActionSetupExtensions.CompositeSyntax AddToComposite(
		InputActionSetupExtensions.CompositeSyntax composite, string part, string[] paths)
	{
		if (paths == null) return composite;

		foreach (var path in paths)
		{
			if (string.IsNullOrWhiteSpace(path)) continue;
			composite = composite.With(part, path);
		}

		return composite;
	}

	private static InputAction BuildButton(string name, string[] paths)
	{
		var action = new InputAction(name, InputActionType.Button);

		if (paths != null)
		{
			foreach (var path in paths)
			{
				if (string.IsNullOrWhiteSpace(path)) continue;
				action.AddBinding(path);
			}
		}

		return action;
	}
}
