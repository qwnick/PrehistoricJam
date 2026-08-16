using UnityEngine;

/// <summary>
/// Sits still, shuffles about occasionally, and bolts across the whole forest the
/// moment the snake gets close.
///
/// It has no stamina at all, so it never tires and never gives you a second
/// chance — and its flee radius sits between the snake's bite and its dash. That
/// gap is the entire design: you cannot walk into range, and you cannot reach it
/// without the dash you earned from the Velociraptor.
/// </summary>
public class PterosaurBrain : EnemyBrain
{
	private enum State
	{
		Idle,
		Flying
	}

	private State state = State.Idle;
	private Vector2 flightTarget;

	protected override void Think()
	{
		if (!HasHunter)
		{
			Wander();
			return;
		}

		if (state == State.Flying)
		{
			// It commits to the landing spot. With no stamina to manage there is
			// nothing to reconsider mid-flight.
			if (MoveTowards(flightTarget, RunSpeed)) return;

			state = State.Idle;
			return;
		}

		if (DistanceToHunter <= FleeRadius)
		{
			TakeOff();
			return;
		}

		Wander();
	}

	private void TakeOff()
	{
		flightTarget = RetreatPoint(FleeDistance);
		state = State.Flying;

		Nav?.SetDestination(flightTarget, tuning.navDomain, force: true);
	}
}
