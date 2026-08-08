using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public float Speed = 200f;

	private AnimatedSprite2D sprite;

	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
            "move_down"
		);

		Velocity = direction * Speed;

		MoveAndSlide();

		UpdateAnimation(direction);
	}


	private void UpdateAnimation(Vector2 direction)
	{
		if (direction != Vector2.Zero)
		{
			sprite.Play("running");
		}
		else
		{
			sprite.Play("default");
		}


		// Mirror sprite when moving left/right
		if (direction.X != 0)
		{
			sprite.FlipH = direction.X < 0;
		}
	}
}
