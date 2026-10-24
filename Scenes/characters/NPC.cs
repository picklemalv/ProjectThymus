using Godot;

public partial class NPC : CharacterBody2D
{
	[Export]
	public string NpcID = "";

	[Export]
	public EnemyData EnemyData;

	// Idle wandering: the enemy drifts around its spawn point but never leaves it by more than WanderRadius.
	[ExportGroup("Idle Wander")]
	[Export] public bool Wander { get; set; } = true;
	[Export] public float WanderRadius { get; set; } = 28f;   // max distance from the spawn point (pixels)
	[Export] public float WanderSpeed { get; set; } = 16f;    // pixels per second
	[Export] public float PauseMin { get; set; } = 1.5f;      // seconds standing still between moves
	[Export] public float PauseMax { get; set; } = 3.5f;

	private Vector2 home;
	private Vector2 target;
	private bool moving = false;
	private float pauseLeft = 0f;
	private float bobTime = 0f;
	private AnimatedSprite2D sprite;
	private Vector2 spriteBasePosition;

	public override void _Ready()
	{
		if (GameState.Instance.IsNpcDefeated(NpcID))
		{
			QueueFree();
			return;
		}

		// Top-down game: no floor / gravity logic for the wander movement.
		MotionMode = MotionModeEnum.Floating;

		sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		if (sprite != null)
			spriteBasePosition = sprite.Position;

		home = GlobalPosition;

		// Random start delay so enemies don't all move in sync.
		pauseLeft = (float)GD.RandRange(0.0, PauseMax);

		SetPhysicsProcess(Wander);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Stand still while the death fade is playing.
		if (GameState.Instance != null && GameState.Instance.Fading)
		{
			Velocity = Vector2.Zero;
			return;
		}

		if (!moving)
		{
			pauseLeft -= dt;

			if (pauseLeft <= 0f)
				PickNewTarget();

			return;
		}

		Vector2 toTarget = target - GlobalPosition;

		if (toTarget.Length() <= WanderSpeed * dt + 0.5f)
		{
			StopMoving();
			return;
		}

		Velocity = toTarget.Normalized() * WanderSpeed;
		MoveAndSlide();

		// Hard leash: never end a frame further than WanderRadius from the spawn point.
		Vector2 offset = GlobalPosition - home;

		if (offset.Length() > WanderRadius)
			GlobalPosition = home + offset.Normalized() * WanderRadius;

		// Bumped into something (wall, player, another enemy): stop and rest.
		if (GetSlideCollisionCount() > 0)
		{
			StopMoving();
			return;
		}

		// Little hop while walking, since the idle art is a single frame.
		if (sprite != null)
		{
			bobTime += dt;
			float hop = Mathf.Abs(Mathf.Sin(bobTime * 9f)) * 1.5f;
			sprite.Position = spriteBasePosition + new Vector2(0f, -hop);
		}
	}

	private void PickNewTarget()
	{
		float angle = (float)GD.RandRange(0.0, Mathf.Tau);
		// sqrt keeps the targets evenly spread over the whole circle.
		float distance = Mathf.Sqrt((float)GD.RandRange(0.0, 1.0)) * WanderRadius;

		target = home + Vector2.Right.Rotated(angle) * distance;
		moving = true;
		bobTime = 0f;
	}

	private void StopMoving()
	{
		moving = false;
		Velocity = Vector2.Zero;
		pauseLeft = (float)GD.RandRange(PauseMin, Mathf.Max(PauseMin, PauseMax));

		if (sprite != null)
			sprite.Position = spriteBasePosition;
	}
}
