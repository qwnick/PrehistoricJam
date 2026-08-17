using UnityEngine;

/// <summary>
/// Genuinely faster than the snake, and it spots you from six dash-lengths away.
/// It cannot be chased — that is deliberate. What it cannot do is go without
/// water: away from the river its stamina bleeds off, and when it runs dry it has
/// to walk to the bank and drink for a long, oblivious moment.
///
/// So the only way to take one is to already be in the water when it arrives,
/// which is why Swim has to come before the desert.
/// </summary>
public class CamelsaurBrain : EnemyBrain
{
	private enum State
	{
		Roaming,
		Fleeing,
		SeekingWater,
		Drinking
	}

	private State state = State.Roaming;
	private Vector2 waterTarget;
	private float drinkEndTime;

	protected override void Think()
	{
		// Being spotted overrides everything, including a half-finished drink.
		if (state != State.Fleeing && HasHunter && DistanceToHunter <= FleeRadius)
			state = State.Fleeing;

		switch (state)
		{
			case State.Roaming: Roaming_Think(); break;
			case State.Fleeing: Fleeing_Think(); break;
			case State.SeekingWater: SeekingWater_Think(); break;
			case State.Drinking: Drinking_Think(); break;
		}
	}

	private void Roaming_Think()
	{
		if (DrainAwayFromWater()) return;

		Wander();
	}

	private void Fleeing_Think()
	{
		DrainAwayFromWater();

		if (!HasHunter || DistanceToHunter >= CalmRadius)
		{
			state = State.Roaming;
			return;
		}

		if (MoveTowards(RetreatPoint(FleeDistance), RunSpeed) == MoveResult.NoRoute)
			Steer(DirectionAwayFromHunter, RunSpeed);
	}

	private void SeekingWater_Think()
	{
		if (!TryFindWater(out waterTarget))
		{
			// Nothing to drink anywhere — carry on and hope.
			state = State.Roaming;
			return;
		}

		if (Vector2.Distance(Position, waterTarget) <= tuning.nearWaterDistance)
		{
			state = State.Drinking;
			drinkEndTime = Time.time + tuning.drinkSeconds;
			return;
		}

		// Arrived is handled by the proximity check above, so anything that is not
		// still moving means the water is unreachable — give up and roam.
		if (MoveTowards(waterTarget, WalkSpeed) != MoveResult.Moving) state = State.Roaming;
	}

	private void Drinking_Think()
	{
		Halt();

		// Stamina refills on its own while nothing is draining it.
		if (Time.time < drinkEndTime) return;

		Stamina?.RefillFully();
		state = State.Roaming;
	}

	/// <summary>
	/// Bleeds stamina while away from water. Returns true once it has run out and
	/// switched to looking for a drink.
	/// </summary>
	private bool DrainAwayFromWater()
	{
		if (Stamina == null) return false;

		bool nearWater = TryFindWater(out var water)
		                 && Vector2.Distance(Position, water) <= tuning.nearWaterDistance;

		if (nearWater) return false;

		if (Stamina.Drain(tuning.staminaDrainPerSecond)) return false;

		state = State.SeekingWater;
		return true;
	}

	private bool TryFindWater(out Vector2 world)
	{
		world = Position;

		var grid = NavGrid.Instance;
		if (grid == null) return false;
		if (!grid.TryFindNearestPassable(Position, NavDomain.Water, out var cell, maxRadius: 40)) return false;

		world = grid.CellToWorld(cell);
		return true;
	}
}
