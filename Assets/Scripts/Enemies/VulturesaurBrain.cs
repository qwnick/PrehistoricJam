using UnityEngine;

/// <summary>
/// The first prey. Runs slightly slower than the snake but escapes with bursts of
/// dashes, and carries only enough stamina for a few of them.
///
/// That is the whole lesson the game teaches with this enemy: you cannot catch it
/// head-on, you have to spend its stamina first. Once the bursts stop, your base
/// speed is enough. Nothing here enforces that — it falls out of runSpeedFactor
/// being just under 1 and maxStamina being small.
/// </summary>
public class VulturesaurBrain : EnemyBrain
{
	private bool isFleeing;

	private Vector2 dashDirection;
	private float dashEndTime;
	private float dashReadyTime;

	private bool IsDashing => Time.time < dashEndTime;

	protected override void Think()
	{
		if (!HasHunter)
		{
			Wander();
			return;
		}

		// A dash is committed: it runs to completion regardless of what the snake does.
		if (IsDashing)
		{
			Body.linearVelocity = dashDirection * tuning.DashSpeed;
			FaceDirection(dashDirection);
			return;
		}

		float distance = DistanceToHunter;

		if (!isFleeing)
		{
			if (distance <= FleeRadius) isFleeing = true;
			else
			{
				Wander();
				return;
			}
		}

		// Hysteresis: it only calms down well outside the radius that spooked it,
		// otherwise it stutters between wandering and fleeing on the boundary.
		if (distance >= CalmRadius)
		{
			isFleeing = false;
			Wander();
			return;
		}

		if (CanDash(distance))
		{
			StartDash();
			return;
		}

		Steer(DirectionAwayFromHunter, RunSpeed);
	}

	private bool CanDash(float distance)
	{
		if (!tuning.canDash) return false;
		if (Time.time < dashReadyTime) return false;
		if (distance > DashTriggerRadius) return false;

		// Out of stamina means no more bursts — this is the opening the player hunts for.
		return Stamina != null && Stamina.CanAfford(tuning.dashStaminaCost);
	}

	private void StartDash()
	{
		if (!Stamina.TryConsume(tuning.dashStaminaCost)) return;

		dashDirection = DirectionAwayFromHunter;
		dashEndTime = Time.time + tuning.dashDuration;
		dashReadyTime = dashEndTime + tuning.dashCooldown;
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();

		if (tuning == null || !tuning.canDash || !Application.isPlaying || !HasHunter) return;

		Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
		Gizmos.DrawWireSphere(transform.position, DashTriggerRadius);
	}
}
