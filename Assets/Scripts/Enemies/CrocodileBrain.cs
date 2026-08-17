using UnityEngine;

/// <summary>
/// Lives in the river, invisible while submerged, and keeps as much water as it
/// can between itself and the snake. Swimming burns its stamina; when it runs out
/// it hauls out onto the bank, rests, and slides back in. Being ashore is the
/// only moment it can be killed.
///
/// Because it is unseeable underwater, this hunt is impossible without
/// echolocation rather than merely difficult — which is what makes the Pterosaur
/// a required step before the river.
///
/// The full cycle is four states, not three: returning to the water has to be its
/// own step. It is amphibious, so without an explicit "get back in the river" the
/// pathfinder is perfectly happy to let it swim away across dry land.
/// </summary>
public class CrocodileBrain : EnemyBrain
{
	private enum State
	{
		Submerged,
		Beaching,
		Resting,
		ReturningToWater
	}

	private State state = State.Submerged;
	private Concealment concealment;

	private Vector2 shoreTarget;
	private Vector2 waterTarget;
	private Vector2 patrolTarget;
	private float restEndTime;
	private float nextReturnPlanTime;
	private float submergedSince;
	private Vector2 retreatTarget;
	private float nextRetreatPlanTime;

	/// <summary>
	/// Standing on a water cell right now, as the nav grid sees it.
	///
	/// When the grid has no water data at all, this answers TRUE rather than
	/// FALSE. Answering false would be honest but fatal: the crocodile would
	/// decide it was beached while floating mid-river, walk to where it already
	/// is, check again, and loop there forever. Assuming it is in its element
	/// degrades to the old free-swimming behaviour instead of freezing.
	/// </summary>
	private bool IsInWater
	{
		get
		{
			var grid = NavGrid.Instance;
			if (grid == null || !grid.IsBuilt || !grid.HasWater) return true;

			return grid.Get(grid.WorldToCell(Position)) == NavGrid.CellType.Water;
		}
	}

	/// <summary>Water-only pathing is meaningless without water data — fall back to the species domain.</summary>
	private NavDomain SwimDomain
	{
		get
		{
			var grid = NavGrid.Instance;
			return grid != null && grid.IsBuilt && grid.HasWater ? NavDomain.Water : tuning.navDomain;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		concealment = GetComponent<Concealment>();
		patrolTarget = Anchor;
		submergedSince = Time.time;
	}

	private void EnterSubmerged()
	{
		state = State.Submerged;
		submergedSince = Time.time;

		// Pick an offshore destination straight away so it swims off the edge
		// instead of loitering where it entered.
		retreatTarget = PickWaterRetreat();
		nextRetreatPlanTime = Time.time + 1f;
	}

	protected override void Think()
	{
		switch (state)
		{
			case State.Submerged: Submerged_Think(); break;
			case State.Beaching: Beaching_Think(); break;
			case State.Resting: Resting_Think(); break;
			case State.ReturningToWater: Returning_Think(); break;
		}
	}

	// ---- Submerged ----

	private void Submerged_Think()
	{
		// Washed up somehow (spawned on land, pushed out): get back in before
		// pretending to be a submerged crocodile.
		if (!IsInWater)
		{
			BeginReturning();
			return;
		}

		Conceal(true);

		// Out of breath: it has to come ashore — but only after a real stint in the
		// water. Being chased back in with an empty bar would otherwise make it
		// beach again on the very next frame, and it would jitter on the waterline
		// forever. While the bar is empty Drain is a no-op, so it refills as it swims.
		bool exhausted = Stamina != null && !Stamina.Drain(tuning.staminaDrainPerSecond);

		if (exhausted && Time.time - submergedSince >= tuning.minSwimSeconds)
		{
			BeginBeaching();
			return;
		}

		if (!HasHunter)
		{
			PatrolWater();
			return;
		}

		// It does not flee in bursts — it simply maximises distance, and strictly
		// within the water. The target has to BE a water cell: aiming at a point
		// offshore made every path fail, and the straight-line fallback then walked
		// it up the bank, which is what caused the shore/water stutter.
		if (Time.time >= nextRetreatPlanTime || Vector2.Distance(Position, retreatTarget) < 1f)
		{
			retreatTarget = PickWaterRetreat();
			nextRetreatPlanTime = Time.time + 1f;
		}

		if (MoveTowards(retreatTarget, RunSpeed, SwimDomain) == MoveResult.NoRoute)
			Steer(retreatTarget - Position, RunSpeed);
	}

	/// <summary>
	/// Samples candidate spots across the river and keeps whichever is furthest
	/// from the snake. Every candidate is snapped onto a water cell first, so the
	/// crocodile can never pick a destination that would take it out of the river.
	/// </summary>
	private Vector2 PickWaterRetreat()
	{
		var grid = NavGrid.Instance;
		if (grid == null || !grid.IsBuilt || !grid.HasWater) return RetreatPoint(FleeDistance);

		Vector2 best = Position;
		float bestDistance = HasHunter ? Vector2.Distance(Position, HunterPosition) : -1f;

		for (int i = 0; i < 12; i++)
		{
			Vector2 candidate = Position + Random.insideUnitCircle.normalized * Random.Range(FleeDistance * 0.4f, FleeDistance);
			if (!grid.TryFindNearestPassable(candidate, NavDomain.Water, out var cell, maxRadius: 8)) continue;

			Vector2 world = grid.CellToWorld(cell);
			float distance = HasHunter ? Vector2.Distance(world, HunterPosition) : Vector2.Distance(world, Position);

			if (distance <= bestDistance) continue;

			bestDistance = distance;
			best = world;
		}

		return best;
	}

	/// <summary>Undisturbed drifting around the spawn point, never leaving the river.</summary>
	private void PatrolWater()
	{
		if (MoveTowards(patrolTarget, WalkSpeed, SwimDomain) == MoveResult.Moving) return;

		var grid = NavGrid.Instance;
		Vector2 candidate = Anchor + Random.insideUnitCircle * tuning.wanderRadius;

		patrolTarget = grid != null && grid.TryFindNearestPassable(candidate, SwimDomain, out var cell)
			? grid.CellToWorld(cell)
			: Anchor;
	}

	// ---- Beaching ----

	private void BeginBeaching()
	{
		state = State.Beaching;
		Conceal(false);

		// Searched wide: a river is easily more than a dozen cells across, and
		// failing to find a bank would strand it mid-water.
		var grid = NavGrid.Instance;

		if (grid != null && grid.TryFindNearestPassable(Position, NavDomain.Land, out var cell, maxRadius: 40))
		{
			Vector2 waterline = grid.CellToWorld(cell);

			// Keep walking a little past the waterline. Stopping on the first dry
			// cell leaves it straddling the edge, where one nudge flips it back
			// into the water and restarts the whole cycle.
			Vector2 inland = waterline + (waterline - Position).normalized * tuning.beachInsetCells;

			shoreTarget = grid.TryFindNearestPassable(inland, NavDomain.Land, out var deeper)
				? grid.CellToWorld(deeper)
				: waterline;
		}
		else
		{
			Debug.LogWarning($"[CrocodileBrain] '{name}' found no land within reach — check that the ground tilemap covers the riverbank.", this);
			shoreTarget = Position;
		}

		Nav?.SetDestination(shoreTarget, tuning.navDomain, force: true);
	}

	private void Beaching_Think()
	{
		Conceal(false);

		switch (MoveTowards(shoreTarget, WalkSpeed))
		{
			case MoveResult.Moving:
				return;

			case MoveResult.NoRoute:
				// It is amphibious, so heading straight at the bank is a sane
				// fallback — anything rather than standing still.
				if (Vector2.Distance(Position, shoreTarget) > 0.5f)
				{
					Steer(shoreTarget - Position, WalkSpeed);
					return;
				}

				break;
		}

		state = State.Resting;
		restEndTime = Time.time + tuning.shoreRestSeconds;
	}

	// ---- Resting ----

	private void Resting_Think()
	{
		Conceal(false);
		Halt();

		// Spooked on the bank: straight back into the water, stamina or not.
		// Uses the flee radius (two dashes), not the calm radius — the doc says
		// "dash radius x 2", and the wider value left the snake permanently inside
		// it, so the crocodile would never finish a rest and never be killable.
		if (HasHunter && DistanceToHunter <= FleeRadius)
		{
			BeginReturning();
			return;
		}

		// The minimum rest is what gives the player a reliable window to strike.
		if (Time.time < restEndTime) return;
		if (Stamina != null && !Stamina.IsFull) return;

		BeginReturning();
	}

	// ---- Returning ----

	private void BeginReturning()
	{
		state = State.ReturningToWater;
		Conceal(false);

		// If the water is genuinely unreachable this gets retried; throttle it so a
		// stranded crocodile does not run A* on every physics step.
		if (Time.time < nextReturnPlanTime) return;
		nextReturnPlanTime = Time.time + 0.5f;

		var grid = NavGrid.Instance;

		if (grid != null && grid.TryFindNearestPassable(Position, NavDomain.Water, out var cell, maxRadius: 40))
		{
			Vector2 waterline = grid.CellToWorld(cell);

			// Push the target well past the edge. Aiming at the nearest water cell
			// parks it on the waterline, where it is one nudge from being ashore
			// again — it has to actually swim out.
			Vector2 deep = waterline + (waterline - Position).normalized * tuning.waterInsetCells;

			waterTarget = grid.TryFindNearestPassable(deep, NavDomain.Water, out var offshore)
				? grid.CellToWorld(offshore)
				: waterline;
		}
		else
		{
			Debug.LogWarning($"[CrocodileBrain] '{name}' found no water within reach — check the water tilemap is assigned to the NavGrid.", this);
			waterTarget = Anchor;
		}

		Nav?.SetDestination(waterTarget, tuning.navDomain, force: true);
	}

	private void Returning_Think()
	{
		Conceal(false);

		// Nothing to return to: resume normal life rather than loop on a goal
		// that can never be satisfied.
		var grid = NavGrid.Instance;
		if (grid == null || !grid.IsBuilt || !grid.HasWater)
		{
			EnterSubmerged();
			return;
		}

		// Actually being in the water is the only thing that ends this state —
		// not "arrived at a waypoint", which is what let it stall on the bank.
		if (IsInWater)
		{
			EnterSubmerged();
			return;
		}

		switch (MoveTowards(waterTarget, WalkSpeed))
		{
			case MoveResult.Moving:
				return;

			case MoveResult.NoRoute:
				if (Vector2.Distance(Position, waterTarget) > 0.5f)
				{
					Steer(waterTarget - Position, WalkSpeed);
					return;
				}

				break;
		}

		// Reached the target but the grid says this is not water — pick again.
		BeginReturning();
	}

	private void Conceal(bool value)
	{
		if (concealment != null) concealment.SetConcealed(value);
	}
}
