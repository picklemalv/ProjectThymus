using Godot;
using System;

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
			battleStarted = true;

			GameState.Instance.HasReturnPosition = true;
			GameState.Instance.ReturnPosition = body.GlobalPosition;

			StartBattle();
		}
	}

	private void StartBattle()
	{
		GD.Print("Battle started!");
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://Scenes/BattleScene/battle_scene.tscn");
	}
}
