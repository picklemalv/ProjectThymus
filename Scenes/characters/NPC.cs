using Godot;

public partial class NPC : CharacterBody2D
{
	[Export]
	public string NpcID = "";

	[Export]
	public EnemyData EnemyData;

	[ExportGroup("Idle Wander")]
	[Export] public bool Wander { get; set; } = true;
	[Export] public float WanderRadius { get; set; } = 28f;
	[Export] public float WanderSpeed { get; set; } = 16f;
	[Export] public float PauseMin { get; set; } = 1.5f;
	[Export] public float PauseMax { get; set; } = 3.5f;

	private Vector2 home;
	private Vector2 target;
	private bool moving = false;
	private float pauseLeft = 0f;
	private AnimatedSprite2D sprite;

	public override void _Ready()
	{
		if (GameState.Instance.IsNpcDefeated(NpcID))
		{
			QueueFree();
			return;
		}

		MotionMode = MotionModeEnum.Floating;

		sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		home = GlobalPosition;
		pauseLeft = (float)GD.RandRange(0.0, PauseMax);

		if (sprite != null)
			sprite.Play("default");

		SetPhysicsProcess(Wander);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		if (GameState.Instance != null && GameState.Instance.Fading)
		{
			Velocity = Vector2.Zero;

			if (sprite != null)
				sprite.Play("default");

			return;
		}

		if (!moving)
		{
			Velocity = Vector2.Zero;
			pauseLeft -= dt;

			if (pauseLeft <= 0f)
				PickNewTarget();

			return;
		}

		Vector2 toTarget = target - GlobalPosition;

		if (toTarget.Length() <= WanderSpeed * dt + 0.5f)
		{
			GlobalPosition = target;
			StopMoving();
			return;
		}

		Velocity = toTarget.Normalized() * WanderSpeed;
		MoveAndSlide();

		// Keep the NPC inside its roaming radius.
		Vector2 offset = GlobalPosition - home;

		if (offset.Length() > WanderRadius)
			GlobalPosition = home + offset.Normalized() * WanderRadius;

		// Stop if the NPC collides with an obstacle or another body.
		if (GetSlideCollisionCount() > 0)
		{
			StopMoving();
			return;
		}

		// Play the walk animation while roaming.
		if (sprite != null)
		{
			sprite.Play("walk");

			// Flip the sprite according to horizontal movement.
			if (Velocity.X != 0)
				sprite.FlipH = Velocity.X < 0;
		}
	}

	private void PickNewTarget()
	{
		float angle = (float)GD.RandRange(0.0, Mathf.Tau);
		float distance = Mathf.Sqrt((float)GD.RandRange(0.0, 1.0))
						 * WanderRadius;

		target = home + Vector2.Right.Rotated(angle) * distance;
		moving = true;
	}

	private void StopMoving()
	{
		moving = false;
		Velocity = Vector2.Zero;

		pauseLeft = (float)GD.RandRange(
			PauseMin,
			Mathf.Max(PauseMin, PauseMax)
		);

		if (sprite != null)
			sprite.Play("default");
	}
}
