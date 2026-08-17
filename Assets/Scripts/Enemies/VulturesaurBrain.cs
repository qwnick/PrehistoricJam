using UnityEngine;

/// <summary>
/// A scavenger that pays for every take-off out of a small stamina pool, and
/// refills it either slowly on the ground or quickly off a corpse.
///
/// That is the hook the player exploits: kill something, leave the body, and the
/// Vulturesaur will eventually have to commit to eating it. Chase it until it is
/// low, then take it while it is head-down on the carcass. It is the only prey
/// whose weakness the player creates rather than waits for.
/// </summary>
public class VulturesaurBrain : EnemyBrain
{
	private enum State
	{
		Idle,
		Flying,
		Eating
	}

	private State state = State.Idle;
	private Vector2 flightTarget;
	private Corpse targetCorpse;
	private float eatEndTime;

	private bool StaminaLow => Stamina != null && Stamina.Normalized <= tuning.lowStaminaThreshold;

	protected override void Think()
	{
		switch (state)
		{
			case State.Idle: Idle_Think(); break;
			case State.Flying: Flying_Think(); break;
			case State.Eating: Eating_Think(); break;
		}
	}

	private void Idle_Think()
	{
		if (HasHunter && DistanceToHunter <= FleeRadius)
		{
			TakeOff();
			return;
		}

		// Undisturbed and hungry: walk over to the nearest body and feed.
		if (StaminaLow && TryFindCorpse(out var corpse))
		{
			targetCorpse = corpse;

			if (Vector2.Distance(Position, corpse.transform.position) <= tuning.nearWaterDistance)
			{
				BeginEating();
				return;
			}

			// Unreachable carcass: fall through to wandering rather than stand and stare.
			if (MoveTowards(corpse.transform.position, WalkSpeed) != MoveResult.NoRoute) return;
		}

		Wander();
	}

	private void TakeOff()
	{
		// Too tired to fly is not an option — it just runs instead.
		if (Stamina != null && !Stamina.TryConsume(tuning.flightStaminaCost))
		{
			if (MoveTowards(RetreatPoint(FleeDistance), WalkSpeed) == MoveResult.NoRoute)
				Steer(DirectionAwayFromHunter, WalkSpeed);

			return;
		}

		// Low on fuel it heads for a meal; otherwise it just puts distance between
		// itself and the snake.
		if (StaminaLow && TryFindCorpse(out var corpse))
		{
			targetCorpse = corpse;
			flightTarget = corpse.transform.position;
		}
		else
		{
			targetCorpse = null;
			flightTarget = RetreatPoint(FleeDistance);
		}

		state = State.Flying;
		Nav?.SetDestination(flightTarget, tuning.navDomain, force: true);
	}

	private void Flying_Think()
	{
		switch (MoveTowards(flightTarget, RunSpeed))
		{
			case MoveResult.Moving:
				return;

			case MoveResult.NoRoute:
				// Cannot route there — abandon the plan rather than hover in place.
				targetCorpse = null;
				state = State.Idle;
				return;
		}

		if (targetCorpse != null)
		{
			BeginEating();
			return;
		}

		state = State.Idle;
	}

	private void BeginEating()
	{
		state = State.Eating;
		eatEndTime = Time.time + tuning.corpseEatSeconds;
	}

	private void Eating_Think()
	{
		// Someone else got there first, or it rotted away.
		if (targetCorpse == null)
		{
			state = State.Idle;
			return;
		}

		Halt();

		// Head down and committed. Interrupting it costs the meal but not the
		// stamina it has already gained — this is the player's window.
		if (HasHunter && DistanceToHunter <= FleeRadius)
		{
			TakeOff();
			return;
		}

		if (Time.time < eatEndTime) return;

		Stamina?.RefillFully();
		targetCorpse.Consume();
		targetCorpse = null;
		state = State.Idle;
	}

	private bool TryFindCorpse(out Corpse corpse)
	{
		corpse = Corpse.FindNearest(Position, FleeDistance);
		return corpse != null;
	}
}
