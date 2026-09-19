using Godot;
using System;

public partial class CombatManager : Node2D
{
	private int playerHP = 100;
	private int playerAttack = 15;

	private int enemyHP = 60;
	private int enemyAttack = 10;

	private bool battleOver = false;

	private Label playerHPLabel;
	private Label enemyHPLabel;
	private Label statusLabel;
	private Button attackButton;
	private ColorRect fadeRect;

	public override void _Ready()
	{
		playerHPLabel = GetNode<Label>("UI/VBoxContainer/PlayerHPLabel");
		enemyHPLabel = GetNode<Label>("UI/VBoxContainer/EnemyHPLabel");
		statusLabel = GetNode<Label>("UI/VBoxContainer/StatusLabel");
		attackButton = GetNode<Button>("UI/VBoxContainer/AttackButton");
		fadeRect = GetNode<ColorRect>("FadeLayer/FadeRect");

		attackButton.Pressed += OnAttackPressed;

		UpdateLabels();
		statusLabel.Text = "A wild enemy appears!";

		FadeIn();
	}

	private void FadeIn()
	{
		fadeRect.Modulate = new Color(1, 1, 1, 1);

		Tween tween = CreateTween();
		tween.TweenProperty(fadeRect, "modulate:a", 0.0f, 1.5f);
	}

	private void OnAttackPressed()
	{
		if (battleOver)
			return;

		enemyHP -= playerAttack;
		enemyHP = Mathf.Max(enemyHP, 0);

		if (enemyHP <= 0)
		{
			EndBattle(true);
			return;
		}

		playerHP -= enemyAttack;
		playerHP = Mathf.Max(playerHP, 0);

		UpdateLabels();

		if (playerHP <= 0)
		{
			EndBattle(false);
			return;
		}

		statusLabel.Text = "You attacked! Enemy attacked back!";
	}

	private void UpdateLabels()
	{
		playerHPLabel.Text = "Player HP: " + playerHP;
		enemyHPLabel.Text = "Enemy HP: " + enemyHP;
	}

	private async void EndBattle(bool playerWon)
{
	battleOver = true;
	attackButton.Disabled = true;
	UpdateLabels();
	statusLabel.Text = playerWon ? "You won the battle!" : "You were defeated...";

	if (playerWon)
	{
		await ToSignal(GetTree().CreateTimer(1.5), Timer.SignalName.Timeout);

		Tween tween = CreateTween();
		tween.TweenProperty(fadeRect, "modulate:a", 1.0f, 0.6f);
		await ToSignal(tween, Tween.SignalName.Finished);

		GameState.Instance.NpcDefeated = true;
		GetTree().ChangeSceneToFile("res://Scenes/main.tscn");
	}
}
}
