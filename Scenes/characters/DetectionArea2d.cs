using Godot;

public partial class DetectionArea2d : Area2D
{
	private bool battleStarted = false;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (battleStarted)
			return;

		if (body is Player)
		{
			NPC npc = GetParent<NPC>();

			if (npc == null)
				return;

			if (GameState.Instance.IsNpcDefeated(npc.NpcID))
				return;

			battleStarted = true;

			GameState.Instance.HasReturnPosition = true;
			GameState.Instance.ReturnPosition = body.GlobalPosition;

			GameState.Instance.CurrentNpcID = npc.NpcID;
			GameState.Instance.CurrentEnemyData = npc.EnemyData;

			StartBattle();
		}
	}

	private void StartBattle()
	{
		GD.Print("Battle started!");

		GetTree().CallDeferred(
			SceneTree.MethodName.ChangeSceneToFile,
			"res://Scenes/BattleScene/battle_scene.tscn"
		);
	}
}
