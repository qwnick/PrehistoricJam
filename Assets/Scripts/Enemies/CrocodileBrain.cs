using UnityEngine;

/// <summary>
/// Lives in the river, invisible while submerged, and keeps as much water as it
/// can between itself and the snake. Swimming burns its stamina; when it runs out
/// it has to haul out onto the bank and rest, and that is the only moment it can
/// be killed.
///
/// Because it is unseeable underwater, this hunt is impossible without
/// echolocation rather than merely difficult — which is what makes the Pterosaur
/// a required step before the river.
/// </summary>
public class CrocodileBrain : EnemyBrain
{
	private enum State
	{
		Submerged,
		Beaching,
		Resting
	}

	private State state = State.Submerged;
	private Concealment concealment;
	private Vector2 shoreTarget;
	private float restEndTime;

	protected override void Awake()
	{
		base.Awake();
		concealment = GetComponent<Concealment>();
	}

	protected override void Think()
	{
		switch (state)
		{
			case State.Submerged: Submerged_Think(); break;
			case State.Beaching: Beaching_Think(); break;
			case State.Resting: Resting_Think(); break;
		}
	}

	private void Submerged_Think()
	{
		Conceal(true);

		// Out of breath: it has to come ashore, whatever the snake is doing.
		if (Stamina != null && !Stamina.Drain(tuning.staminaDrainPerSecond))
		{
			BeginBeaching();
			return;
		}

		if (!HasHunter)
		{
			Wander();
			return;
		}

		// Underwater it does not flee in bursts — it simply maximises distance.
		MoveTowards(RetreatPoint(FleeDistance), RunSpeed);
	}

	private void BeginBeaching()
	{
		state = State.Beaching;
		Conceal(false);

		var grid = NavGrid.Instance;

		if (grid != null && grid.TryFindNearestPassable(Position, NavDomain.Land, out var cell))
			shoreTarget = grid.CellToWorld(cell);
		else
			shoreTarget = Position;

		Nav?.SetDestination(shoreTarget, tuning.navDomain, force: true);
	}

	private void Beaching_Think()
	{
		Conceal(false);

		if (MoveTowards(shoreTarget, WalkSpeed)) return;

		state = State.Resting;
		restEndTime = Time.time + tuning.shoreRestSeconds;
	}

	private void Resting_Think()
	{
		Conceal(false);
		Halt();

		// Spooked on the bank: straight back into the water regardless of stamina.
		if (HasHunter && DistanceToHunter <= CalmRadius)
		{
			state = State.Submerged;
			return;
		}

		// The minimum rest is what gives the player a reliable window to strike.
		if (Time.time < restEndTime) return;
		if (Stamina != null && !Stamina.IsFull) return;

		state = State.Submerged;
	}

	private void Conceal(bool value)
	{
		if (concealment != null) concealment.SetConcealed(value);
	}
}
