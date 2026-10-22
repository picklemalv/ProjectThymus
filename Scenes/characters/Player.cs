using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public float Speed = 200f;

	private AnimatedSprite2D sprite;
	private Camera2D camera;
	private Vector2 baseZoom = Vector2.One;
	private Vector2 baseOffset = Vector2.Zero;
	private float swayTime = 0f;

	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		foreach (Node child in GetChildren())
		{
			if (child is Camera2D foundCamera)
			{
				camera = foundCamera;
				baseZoom = foundCamera.Zoom;
				baseOffset = foundCamera.Offset;
				break;
			}
		}

		if (GameState.Instance.HasReturnPosition)
		{
			GlobalPosition = GameState.Instance.ReturnPosition;
			GameState.Instance.HasReturnPosition = false;
		}
		else if (InExplorationScene())
		{
			GameState.Instance.SetExpeditionStart(GlobalPosition);
		}
	}

	private bool InExplorationScene()
	{
		Node node = this;

		while (node.GetParent() != null && node.GetParent() != GetTree().Root)
			node = node.GetParent();

		return node.SceneFilePath == "res://Scenes/main.tscn";
	}

	public override void _PhysicsProcess(double delta)
	{
		float multiplier = GameState.Instance.MoveSpeedMultiplier;

		Vector2 direction = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
			"move_down"
		);

		Velocity = direction * Speed * multiplier;

		MoveAndSlide();

		UpdateAnimation(direction, multiplier);
		UpdateCamera(delta);
	}

	private void UpdateCamera(double delta)
	{
		if (camera == null)
			return;

		float step = (float)delta;

		if (GameState.Instance.CarryingFinalCore)
		{
			swayTime += step;

			camera.Zoom = camera.Zoom.Lerp(baseZoom * 1.15f, step * 0.6f);
			camera.Offset = baseOffset + new Vector2(
				Mathf.Sin(swayTime * 1.3f) * 2.5f,
				Mathf.Sin(swayTime * 2.1f) * 3.5f
			);
		}
		else
		{
			swayTime = 0f;

			camera.Zoom = camera.Zoom.Lerp(baseZoom, step * 2.0f);
			camera.Offset = camera.Offset.Lerp(baseOffset, step * 3.0f);
		}
	}

	private void UpdateAnimation(Vector2 direction, float multiplier)
	{
		sprite.SpeedScale = multiplier;

		if (direction != Vector2.Zero)
		{
			sprite.Play("running");
		}
		else
		{
			sprite.Play("default");
		}

		if (direction.X != 0)
		{
			sprite.FlipH = direction.X < 0;
		}
	}
}
