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

		if (GameState.Instance.IsEscapeGraceActive())
			return;

		if (body is Player)
		{
			NPC npc = GetParent<NPC>();

			if (npc == null)
			{
				GD.PushError("DetectionArea2d could not find parent NPC!");
				return;
			}

			if (GameState.Instance.IsNpcDefeated(npc.NpcID))
				return;

			if (npc.EnemyData == null)
			{
				GD.PushError($"NPC {npc.NpcID} has NO EnemyData assigned!");
				return;
			}

			AnimatedSprite2D npcSprite = npc.GetNode<AnimatedSprite2D>("AnimatedSprite2D");

			if (npcSprite == null)
			{
				GD.PushError($"NPC {npc.NpcID} has no AnimatedSprite2D!");
				return;
			}

			battleStarted = true;

			GameState.Instance.HasReturnPosition = true;
			GameState.Instance.ReturnPosition = body.GlobalPosition;

			GameState.Instance.CurrentNpcID = npc.NpcID;
			GameState.Instance.CurrentEnemyData = npc.EnemyData;
			GameState.Instance.CurrentEnemySpriteFrames = npcSprite.SpriteFrames;

			GD.Print("==============================");
			GD.Print($"Starting battle against NPC {npc.NpcID}");
			GD.Print($"Enemy Name: {npc.EnemyData.EnemyName}");
			GD.Print($"Enemy HP: {npc.EnemyData.BaseHP}");
			GD.Print($"Enemy Attack: {npc.EnemyData.BaseAttack}");
			GD.Print($"Enemy Defense: {npc.EnemyData.BaseDefense}");
			GD.Print($"Enemy Speed: {npc.EnemyData.BaseSpeed}");
			GD.Print($"SpriteFrames: {npcSprite.SpriteFrames}");
			GD.Print("==============================");

			StartBattle();
		}
	}

	private void StartBattle()
	{
		GetTree().CallDeferred(
			SceneTree.MethodName.ChangeSceneToFile,
			"res://Scenes/BattleScene/battle_scene.tscn"
		);
	}
}
