
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
		prompt.Text = BuildPromptText();
		prompt.Visible = false;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		if (playerInRange && Input.IsActionPressed("interact"))
		{
			TeleportPlayer();
		}
	}

	private string BuildPromptText()
	{
		if (PromptText.StartsWith("E - "))
			return GameSettings.GetKeyName("interact") + PromptText.Substring(1);

		return PromptText;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player p)
		{
			player = p;
			playerInRange = true;
			prompt.Text = BuildPromptText();
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

		int zoneNumber = GetZoneNumber(Destination);

		if (zoneNumber > 0)
		{
			GameState.Instance.CurrentZone = zoneNumber;
			ShowZoneBanner(zoneNumber);
		}

		playerInRange = false;
		prompt.Visible = false;
		player = null;
	}

	private int GetZoneNumber(Marker2D marker)
	{
		Node parent = marker.GetParent();

		if (parent == null)
			return 0;

		string parentName = parent.Name.ToString();

		if (parentName.StartsWith("Map") && int.TryParse(parentName.Substring(3), out int number))
			return number;

		return 0;
	}

	private void ShowZoneBanner(int zoneNumber)
	{
		ZoneRule rule = GameDatabase.GetZone(zoneNumber);

		CanvasLayer layer = new CanvasLayer();
		layer.Layer = 50;

		Label label = new Label();
		label.Text = $"{rule.Name}\nInfection +{rule.InfectionPerTurn:0}% per turn";
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.AddThemeFontSizeOverride("font_size", 28);
		layer.AddChild(label);

		GetTree().CurrentScene.AddChild(layer);

		label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
		label.GrowHorizontal = Control.GrowDirection.Both;
		label.OffsetTop += 70;
		label.OffsetBottom += 70;

		Tween tween = layer.CreateTween();
		tween.TweenInterval(1.5);
		tween.TweenProperty(label, "modulate:a", 0.0f, 1.0f);
		tween.TweenCallback(Callable.From(layer.QueueFree));
	}
}
