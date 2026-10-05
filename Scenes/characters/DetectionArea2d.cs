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
			{
				GD.PushError("DetectionArea2d could not find parent NPC!");
				return;
			}

			if (GameState.Instance.IsNpcDefeated(npc.NpcID))
				return;

			// Make sure the NPC actually has EnemyData
			if (npc.EnemyData == null)
			{
				GD.PushError(
					$"NPC {npc.NpcID} has NO EnemyData assigned!"
				);

				return;
			}

			// Get the NPC's AnimatedSprite2D
			AnimatedSprite2D npcSprite =
				npc.GetNode<AnimatedSprite2D>("AnimatedSprite2D");

			if (npcSprite == null)
			{
				GD.PushError(
					$"NPC {npc.NpcID} has no AnimatedSprite2D!"
				);

				return;
			}

			battleStarted = true;

			// Save player position
			GameState.Instance.HasReturnPosition = true;
			GameState.Instance.ReturnPosition =
				body.GlobalPosition;

			// Save NPC identity
			GameState.Instance.CurrentNpcID =
				npc.NpcID;

			// Save enemy combat data
			GameState.Instance.CurrentEnemyData =
				npc.EnemyData;

			// Save enemy appearance
			GameState.Instance.CurrentEnemySpriteFrames =
				npcSprite.SpriteFrames;

			GD.Print(
				"=============================="
			);

			GD.Print(
				$"Starting battle against NPC {npc.NpcID}"
			);

			GD.Print(
				$"Enemy Name: {npc.EnemyData.EnemyName}"
			);

			GD.Print(
				$"Enemy HP: {npc.EnemyData.BaseHP}"
			);

			GD.Print(
				$"Enemy Attack: {npc.EnemyData.BaseAttack}"
			);

			GD.Print(
				$"Enemy Defense: {npc.EnemyData.BaseDefense}"
			);

			GD.Print(
				$"Enemy Speed: {npc.EnemyData.BaseSpeed}"
			);

			GD.Print(
				$"SpriteFrames: {npcSprite.SpriteFrames}"
			);

			GD.Print(
				"=============================="
			);

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
