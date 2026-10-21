
using Godot;

public partial class Teleport : Area2D
{
	[Export]
	public Marker2D Destination;

	[Export]
	public string PromptText = "E - Go";

	private Label prompt;
	private Player player;
	private bool playerInRange = false;

	public override void _Ready()
	{
		prompt = GetNode<Label>("Prompt");
		prompt.Text = PromptText;
		prompt.Visible = false;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		if (playerInRange && Input.IsKeyPressed(Key.E))
		{
			TeleportPlayer();
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player p)
		{
			player = p;
			playerInRange = true;
			prompt.Visible = true;
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body == player)
		{
			playerInRange = false;
			prompt.Visible = false;
			player = null;
		}
	}

	private void TeleportPlayer()
	{
		if (Destination == null || player == null)
			return;

		player.GlobalPosition = Destination.GlobalPosition;

		playerInRange = false;
		prompt.Visible = false;
		player = null;
	}
}
