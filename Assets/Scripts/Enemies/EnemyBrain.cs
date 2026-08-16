using UnityEngine;

/// <summary>
/// Shared base for every prey species: config wiring, perception, steering,
/// wandering, and leaving a corpse behind. Subclasses only implement Think().
///
/// Perception values are never read raw off the tuning asset — they always go
/// through the resolvers below, which scale them against the hunter's own dash
/// distance and move speed. That is why retuning the snake retunes the roster.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public abstract class EnemyBrain : MonoBehaviour
{
	[Header("Config")]
	[SerializeField] protected EnemyTuning tuning;

	[Tooltip("Spawned on death. Eating it is what advances the hunter's progression.")]
	[SerializeField] private Corpse corpsePrefab;

	[SerializeField] private bool drawDebugGizmos = true;

	protected Rigidbody2D Body { get; private set; }
	protected Health Health { get; private set; }
	protected Stamina Stamina { get; private set; }

	/// <summary>Optional. Without one, movement falls back to steering in a straight line.</summary>
	protected PathFollower Nav { get; private set; }

	/// <summary>Home position for wandering — where this enemy was placed or spawned.</summary>
	protected Vector2 Anchor { get; private set; }

	private Vector2 wanderTarget;
	private float wanderResumeTime;

	// ---- Perception ----

	// Named Target rather than Hunter: a member sharing its type's name makes
	// every "Hunter.Instance" inside this class ambiguous to the compiler.
	protected static Hunter Target => Hunter.Instance;
	protected static bool HasHunter => Hunter.Instance != null;

	protected PlayerTuning HunterTuning => HasHunter ? Target.Tuning : null;
	protected Vector2 HunterPosition => (Vector2)Target.transform.position;
	protected Vector2 Position => transform.position;

	protected float DistanceToHunter => Vector2.Distance(Position, HunterPosition);
	protected Vector2 DirectionAwayFromHunter => (Position - HunterPosition).normalized;
	protected Vector2 DirectionToHunter => (HunterPosition - Position).normalized;

	protected float FleeRadius => tuning.FleeRadius(HunterTuning);
	protected float CalmRadius => tuning.CalmRadius(HunterTuning);
	protected float WalkSpeed => tuning.WalkSpeed(HunterTuning);
	protected float RunSpeed => tuning.RunSpeed(HunterTuning);
	protected float DashTriggerRadius => tuning.DashTriggerRadius(HunterTuning);
	protected float FleeDistance => tuning.FleeDistance(HunterTuning);

	protected virtual void Awake()
	{
		Body = GetComponent<Rigidbody2D>();
		Health = GetComponent<Health>();
		Stamina = GetComponent<Stamina>();
		Nav = GetComponent<PathFollower>();
		Anchor = transform.position;
		wanderTarget = Anchor;

		if (tuning == null)
		{
			Debug.LogError($"[{GetType().Name}] No EnemyTuning assigned on '{name}'.", this);
			enabled = false;
			return;
		}

		Health.Configure(tuning.health);
		Health.Died += HandleDeath;

		if (Stamina != null && tuning.usesStamina)
			Stamina.Configure(tuning.maxStamina, tuning.staminaRegenPerSecond, tuning.staminaRegenDelay);
	}

	protected virtual void OnDestroy()
	{
		if (Health != null) Health.Died -= HandleDeath;
	}

	private void FixedUpdate()
	{
		if (Health.IsDead) return;
		Think();
	}

	/// <summary>Per-species behaviour. Runs on the physics step.</summary>
	protected abstract void Think();

	// ---- Steering ----

	protected void Steer(Vector2 direction, float speed)
	{
		if (direction.sqrMagnitude < 0.0001f)
		{
			Halt();
			return;
		}

		direction.Normalize();
		Body.linearVelocity = direction * speed;
		FaceDirection(direction);
	}

	protected void Halt() => Body.linearVelocity = Vector2.zero;

	/// <summary>
	/// Walks towards a world point, routing around terrain when a PathFollower is
	/// present. Returns false once there is nowhere left to go — arrived, or no
	/// route exists — which is what the state machines use as their "done" signal.
	/// </summary>
	protected bool MoveTowards(Vector2 target, float speed)
	{
		if (Nav == null)
		{
			// No pathfinder: straight-line steering, fine for open ground.
			Vector2 offset = target - Position;
			if (offset.sqrMagnitude < 0.04f) { Halt(); return false; }

			Steer(offset, speed);
			return true;
		}

		if (!Nav.SetDestination(target, tuning.navDomain))
		{
			Halt();
			return false;
		}

		Vector2 direction = Nav.Steering();
		if (direction == Vector2.zero) { Halt(); return false; }

		Steer(direction, speed);
		return true;
	}

	/// <summary>A point roughly <paramref name="distance"/> away, directly opposite the hunter.</summary>
	protected Vector2 RetreatPoint(float distance) => Position + DirectionAwayFromHunter * distance;

	/// <summary>Sprites in this project point up, hence the -90 degree offset.</summary>
	protected void FaceDirection(Vector2 direction)
	{
		// if (direction.sqrMagnitude < 0.0001f) return;

		// float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
		// Body.MoveRotation(Mathf.LerpAngle(Body.rotation, angle, 0.25f));
	}

	/// <summary>
	/// Idle behaviour: amble to a random nearby point, pause, repeat.
	/// Shared because four of the five species do exactly this when undisturbed.
	/// </summary>
	protected void Wander()
	{
		if (Time.time < wanderResumeTime)
		{
			Halt();
			return;
		}

		if (Vector2.Distance(Position, wanderTarget) < 0.15f)
		{
			wanderTarget = Anchor + Random.insideUnitCircle * tuning.wanderRadius;
			wanderResumeTime = Time.time + Random.Range(tuning.wanderPauseMin, tuning.wanderPauseMax);
			Halt();
			return;
		}

		Steer(wanderTarget - Position, WalkSpeed);
	}

	// ---- Death ----

	private void HandleDeath(Health _)
	{
		SpawnCorpse();
		Destroy(gameObject);
	}

	private void SpawnCorpse()
	{
		if (corpsePrefab == null)
		{
			Debug.LogWarning($"[{GetType().Name}] '{name}' died with no corpse prefab — progression cannot advance from it.", this);
			return;
		}

		var corpse = Instantiate(corpsePrefab, transform.position, transform.rotation);
		corpse.Initialize(tuning.species, tuning.corpseLifetime, tuning.corpseNutrition);
	}

	// ---- Debug ----

	protected virtual void OnDrawGizmosSelected()
	{
		// Every radius in this game is defined relative to the snake, which makes
		// them impossible to tune by eye without drawing them.
		if (!drawDebugGizmos || tuning == null || !Application.isPlaying || !HasHunter) return;

		Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f);
		Gizmos.DrawWireSphere(transform.position, FleeRadius);

		Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.5f);
		Gizmos.DrawWireSphere(transform.position, CalmRadius);

		Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
		Gizmos.DrawWireSphere(Anchor, tuning.wanderRadius);
	}
}
