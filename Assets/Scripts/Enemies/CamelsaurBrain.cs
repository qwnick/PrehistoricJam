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
	// Water does not move, and the nearest-water search is by far the most
	// expensive thing this brain does. Re-running it every physics step was
	// burning real CPU for an answer that barely changes.
	private const float WaterSearchInterval = 1.5f;

	// After failing to find water, roam for a while before trying again. Without
	// this, an exhausted camelsaur out of range flips between Roaming and
	// SeekingWater on every single frame, paying for a full search each time.
	private const float GiveUpSeconds = 5f;

	private enum State
	{
		Roaming,
		Fleeing,
		SeekingWater,
		Drinking
	}

	private State state = State.Roaming;

	private Vector2 waterTarget;
	private bool hasWaterTarget;
	private float drinkEndTime;
	private float retryWaterTime;

	private Vector2 cachedWater;
	private bool cachedWaterValid;
	private float cacheExpiryTime;

	protected override void Think()
	{
		// Being spotted overrides everything
		if (state != State.Fleeing && state!=State.Drinking && HasHunter && DistanceToHunter <= FleeRadius)
			state = State.Fleeing;

		switch (state)
		{
			case State.Roaming: Roaming_Think(); break;
			case State.Fleeing: Fleeing_Think(); break;
			case State.SeekingWater: SeekingWater_Think(); break;
			case State.Drinking: Drinking_Think(); break;
		}
	}

	// ---- Roaming ----

	private void Roaming_Think()
	{
		DrainStamina();

		// "When stamina is exhausted, if it doesn't see the snake, it goes to
		// drink" — so this transition belongs here and not while fleeing.
		if (Stamina != null && Stamina.IsEmpty && Time.time >= retryWaterTime)
		{
			hasWaterTarget = false;
			state = State.SeekingWater;
			return;
		}

		Wander();
	}

	// ---- Fleeing ----

	private void Fleeing_Think()
	{
		DrainStamina();

		if (!HasHunter || DistanceToHunter >= CalmRadius)
		{
			state = State.Roaming;
			return;
		}

		if (MoveTowards(RetreatPoint(FleeDistance), RunSpeed) == MoveResult.NoRoute)
			Steer(DirectionAwayFromHunter, RunSpeed);
	}

	// ---- Seeking water ----

	private void SeekingWater_Think()
	{
		if (!hasWaterTarget)
		{
			if (!TryFindWater(out waterTarget))
			{
				GiveUpOnWater();
				return;
			}

			hasWaterTarget = true;
		}

		if (Vector2.Distance(Position, waterTarget) <= tuning.nearWaterDistance)
		{
			BeginDrinking();
			return;
		}

		switch (MoveTowards(waterTarget, WalkSpeed))
		{
			case MoveResult.Moving:
				return;

			// The target is a WATER cell but the camelsaur walks on land, so A*
			// routes it to the bank beside it. Arriving there is arriving at the
			// water — treating it as a failure is why it never drank.
			case MoveResult.Arrived:
				BeginDrinking();
				return;

			case MoveResult.NoRoute:
				GiveUpOnWater();
				return;
		}
	}

	private void GiveUpOnWater()
	{
		hasWaterTarget = false;
		retryWaterTime = Time.time + GiveUpSeconds;
		state = State.Roaming;
	}

	// ---- Drinking ----

	private void BeginDrinking()
	{
		state = State.Drinking;
		drinkEndTime = Time.time + tuning.drinkSeconds;
		Halt();
	}

	private void Drinking_Think()
	{
		Halt();

		if (Time.time < drinkEndTime) return;

		Stamina?.RefillFully();
		hasWaterTarget = false;
		state = State.Roaming;
	}

	// ---- Water and stamina ----

	/// <summary>Bleeds stamina while away from water. Never changes state.</summary>
	private void DrainStamina()
	{
		if (Stamina == null) return;
		if (IsNearWater()) return;

		Stamina.Drain(tuning.staminaDrainPerSecond);
	}

	private bool IsNearWater()
		=> TryFindWater(out var water) && Vector2.Distance(Position, water) <= tuning.nearWaterDistance;

	/// <summary>Nearest water, cached — see <see cref="WaterSearchInterval"/>.</summary>
	private bool TryFindWater(out Vector2 world)
	{
		if (Time.time < cacheExpiryTime)
		{
			world = cachedWater;
			return cachedWaterValid;
		}

		cacheExpiryTime = Time.time + WaterSearchInterval;
		cachedWater = Position;
		cachedWaterValid = false;

		var grid = NavGrid.Instance;

		// HasWater short-circuits the whole search on maps with no river at all.
		if (grid != null && grid.IsBuilt && grid.HasWater
		    && grid.TryFindNearestPassable(Position, NavDomain.Water, out var cell, maxRadius: 40))
		{
			cachedWater = grid.CellToWorld(cell);
			cachedWaterValid = true;
		}

		world = cachedWater;
		return cachedWaterValid;
	}
}
