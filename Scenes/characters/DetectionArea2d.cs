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

		if (GameState.Instance.IsEscapeGraceActive() || GameState.Instance.CarryingFinalCore || GameState.Instance.Fading)
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

			EnemyData enemyData = npc.EnemyData;

			if (enemyData == null)
			{
				enemyData = BuildFallbackEnemyData(npc.NpcID);

				if (enemyData != null)
					GD.PushWarning($"NPC {npc.NpcID} has no EnemyData assigned, using built-in fallback stats.");
			}

			if (enemyData == null)
			{
				GD.PushError($"NPC {npc.NpcID} has NO EnemyData assigned!");
				return;
			}

			enemyData = EnemyBalance.Apply(npc.NpcID, enemyData);

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
			GameState.Instance.CurrentEnemyData = enemyData;
			GameState.Instance.CurrentEnemySpriteFrames = npcSprite.SpriteFrames;

			GD.Print("==============================");
			GD.Print($"Starting battle against NPC {npc.NpcID}");
			GD.Print($"Enemy Name: {enemyData.EnemyName}");
			GD.Print($"Enemy HP: {enemyData.BaseHP}");
			GD.Print($"Enemy Attack: {enemyData.BaseAttack}");
			GD.Print($"Enemy Defense: {enemyData.BaseDefense}");
			GD.Print($"Enemy Speed: {enemyData.BaseSpeed}");
			GD.Print($"SpriteFrames: {npcSprite.SpriteFrames}");
			GD.Print("==============================");

			StartBattle();
		}
	}

	private EnemyData BuildFallbackEnemyData(string npcID)
	{
		if (npcID != "Boss3")
			return null;

		EnemyData data = new EnemyData();
		data.EnemyName = "Final Boss";
		data.BaseHP = 450;
		data.BaseAttack = 38;
		data.BaseDefense = 10;
		data.BaseSpeed = 8;
		data.ScrapDrop = 60;
		data.MaterialDrop = 15;
		data.EnergyDrop = 8;
		data.EssenceDrop = 3;
		data.EssenceDropChance = 1f;
		return data;
	}

	private void StartBattle()
	{
		GetTree().CallDeferred(
			SceneTree.MethodName.ChangeSceneToFile,
			"res://Scenes/BattleScene/battle_scene.tscn"
		);
	}
}
