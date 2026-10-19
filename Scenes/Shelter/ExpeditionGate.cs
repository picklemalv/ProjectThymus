using Godot;

public partial class ExpeditionGate : Area2D
{
	[Export(PropertyHint.File, "*.tscn")]
	public string ExpeditionScene = "res://Scenes/main.tscn";

	private bool leaving = false;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (leaving || body is not Player)
			return;

		leaving = true;
		GameState.Instance.StartExpedition();
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, ExpeditionScene);
	}
}
