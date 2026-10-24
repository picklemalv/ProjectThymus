using Godot;

public partial class ExpeditionGate : Area2D
{
	[Export(PropertyHint.File, "*.tscn")]
	public string ExpeditionScene = "res://Scenes/main.tscn";

	// Where the player appears (relative to the gate) after coming back with the final core.
	// Keep it outside the gate's collision area, or the gate will start a new expedition.
	[Export]
	public Vector2 ReturnOffset = new Vector2(0f, 90f);

	private bool leaving = false;
	private ulong lastWarningMs = 0;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;


		if (GameState.Instance.SpawnAtGate)
		{
			GameState.Instance.SpawnAtGate = false;
			CallDeferred(nameof(MovePlayerToGate));
		}
	}

	private void MovePlayerToGate()
	{
		Node2D player = GetParent().GetNodeOrNull<Node2D>("Player");

		if (player == null)
		{
			GD.PushWarning("ExpeditionGate: no Player node found next to the gate.");
			return;
		}

		CollisionShape2D shape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		Vector2 gatePosition = shape != null ? shape.GlobalPosition : GlobalPosition;

		player.GlobalPosition = gatePosition + ReturnOffset;

		Camera2D camera = player.GetNodeOrNull<Camera2D>("Camera2D");

		if (camera != null)
			camera.ResetSmoothing();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (leaving || body is not Player)
			return;

		if (!GameState.Instance.GearUpQuestDone)
		{
			ShowBlockedMessage();
			return;
		}

		leaving = true;
		GameState.Instance.StartExpedition();
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, ExpeditionScene);
	}

	private void ShowBlockedMessage()
	{
		ulong now = Time.GetTicksMsec();

		if (now - lastWarningMs < 1500)
			return;

		lastWarningMs = now;

		Node scene = GetTree().CurrentScene;

		if (scene == null)
			return;

		CanvasLayer layer = new CanvasLayer();
		layer.Layer = 60;

		Label label = new Label();
		label.Text = "You can't go out unarmed.\nCraft and equip gear at the Armory first.";
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.AddThemeFontSizeOverride("font_size", 26);
		layer.AddChild(label);

		scene.AddChild(layer);

		label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
		label.GrowHorizontal = Control.GrowDirection.Both;
		label.OffsetTop += 80;
		label.OffsetBottom += 80;

		Tween tween = layer.CreateTween();
		tween.TweenInterval(2.0);
		tween.TweenProperty(label, "modulate:a", 0.0f, 1.0f);
		tween.TweenCallback(Callable.From(layer.QueueFree));
	}
}
