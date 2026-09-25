using Godot;

public partial class NPC : CharacterBody2D
{
	[Export]
	public string NpcID = "";

	private bool battleStarted = false;

	public override void _Ready()
	{
		if (GameState.Instance.IsNpcDefeated(NpcID))
		{
			QueueFree();
			return;
		}
	}

	public void StartBattle()
	{
		if (battleStarted)
			return;

		if (GameState.Instance.IsNpcDefeated(NpcID))
			return;

		battleStarted = true;

		GameState.Instance.CurrentNpcID = NpcID;

		GetTree().ChangeSceneToFile(
			"res://Scenes/battle_scene.tscn"
		);
	}
}
