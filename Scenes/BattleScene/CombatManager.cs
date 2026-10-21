using Godot;
using System;

public partial class CombatManager : Node2D
{
	private const float InfectionGainPerTurn = 2f;

	private Stats playerStats;
	private Stats enemyStats;

	private bool battleOver = false;

	private Label playerHPLabel;
	private Label enemyHPLabel;
	private Label infectionLabel;
	private Label statusLabel;
	private Button attackButton;
	private ColorRect fadeRect;

	private AnimatedSprite2D playerSprite;
	private AnimatedSprite2D enemySprite;

	public override void _Ready()
	{
		playerHPLabel = GetNode<Label>("UI/VBoxContainer/PlayerHPLabel");
		enemyHPLabel = GetNode<Label>("UI/VBoxContainer/EnemyHPLabel");
		infectionLabel = GetNode<Label>("UI/VBoxContainer/InfectionLabel");
		statusLabel = GetNode<Label>("UI/VBoxContainer/StatusLabel");
		attackButton = GetNode<Button>("UI/VBoxContainer/AttackButton");
		fadeRect = GetNode<ColorRect>("FadeLayer/FadeRect");
		playerSprite = GetNode<AnimatedSprite2D>("Player/AnimatedSprite2D");
		playerSprite.AnimationFinished += OnAnimationFinished;

		enemySprite = GetNode<AnimatedSprite2D>("NPC/AnimatedSprite2D");
		LoadEncounteredEnemy();

		attackButton.Pressed += OnAttackPressed;

		playerStats = GameState.Instance.PlayerStats;

		EnemyData data = GameState.Instance.CurrentEnemyData;

		enemyStats = new Stats();
		if (data != null)
		{
			enemyStats.BaseHP = data.BaseHP;
			enemyStats.BaseAttack = data.BaseAttack;
			enemyStats.BaseDefense = data.BaseDefense;
			enemyStats.BaseSpeed = data.BaseSpeed;
		}
		else
		{
			GD.PushWarning("No EnemyData set, using fallback stats.");
			enemyStats.BaseHP = 60;
			enemyStats.BaseAttack = 10;
			enemyStats.BaseDefense = 3;
			enemyStats.BaseSpeed = 6;
		}
		AddChild(enemyStats);

		UpdateLabels();
		statusLabel.Text = data != null ? $"A wild {data.EnemyName} appears!" : "A wild enemy appears!";

		FadeIn();
		GD.Print("==============================");
		GD.Print("BATTLE SCENE LOADED");
		GD.Print($"CurrentNpcID: {GameState.Instance.CurrentNpcID}");
		GD.Print($"CurrentEnemyData: {GameState.Instance.CurrentEnemyData}");
		GD.Print($"CurrentEnemySpriteFrames: {GameState.Instance.CurrentEnemySpriteFrames}");
		GD.Print("==============================");

		if (enemyStats.Speed > playerStats.Speed)
		{
			attackButton.Disabled = true;
			CallDeferred(nameof(EnemyOpeningAttack));
		}
	}

	private async void EnemyOpeningAttack()
	{
		await ToSignal(GetTree().CreateTimer(1.0), Timer.SignalName.Timeout);

		if (battleOver)
			return;

		statusLabel.Text = "Enemy is faster! It strikes first!";

		int dmg = CalculateDamage(enemyStats.Attack, playerStats.Defense);
		playerStats.CurrentHP -= dmg;
		playerStats.CurrentHP = Mathf.Max(playerStats.CurrentHP, 0);
		UpdateLabels();

		if (playerStats.CurrentHP <= 0)
		{
			TriggerTimeLoop("You were overwhelmed...");
			return;
		}

		attackButton.Disabled = false;
	}

	private int CalculateDamage(int attack, int defense)
	{
		return Mathf.Max(attack - defense, 1);
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

		playerSprite.Play("attack");

		playerStats.AddInfection(InfectionGainPerTurn);

		int dmgToEnemy = CalculateDamage(playerStats.Attack, enemyStats.Defense);
		enemyStats.CurrentHP -= dmgToEnemy;
		enemyStats.CurrentHP = Mathf.Max(enemyStats.CurrentHP, 0);

		if (enemyStats.CurrentHP <= 0)
		{
			EndBattle();
			return;
		}

		int dmgToPlayer = CalculateDamage(enemyStats.Attack, playerStats.Defense);
		playerStats.CurrentHP -= dmgToPlayer;
		playerStats.CurrentHP = Mathf.Max(playerStats.CurrentHP, 0);

		UpdateLabels();

		if (playerStats.CurrentHP <= 0)
		{
			TriggerTimeLoop("You were overwhelmed...");
			return;
		}

		if (playerStats.IsInfectionMaxed())
		{
			TriggerTimeLoop("Infection reached 100%!");
			return;
		}

		statusLabel.Text = "You attacked! Enemy attacked back!";
	}

	private void OnAnimationFinished()
	{
		if (playerSprite.Animation == "attack")
		{
			playerSprite.Play("default");
		}
	}

	private void UpdateLabels()
	{
		playerHPLabel.Text = "Player HP: " + playerStats.CurrentHP;
		enemyHPLabel.Text = "Enemy HP: " + enemyStats.CurrentHP;
		infectionLabel.Text = $"Infection: {playerStats.CurrentInfection:0}%";
	}

	private async void TriggerTimeLoop(string reason)
	{
		battleOver = true;
		attackButton.Disabled = true;
		statusLabel.Text = $"{reason} Time Loop activated...";
		UpdateLabels();

		await ToSignal(GetTree().CreateTimer(1.5), Timer.SignalName.Timeout);

		Tween tween = CreateTween();
		tween.TweenProperty(fadeRect, "modulate:a", 1.0f, 0.6f);
		await ToSignal(tween, Tween.SignalName.Finished);

		GameState.Instance.EndExpedition(false);

		GetTree().ChangeSceneToFile("res://Scenes/Shelter/Shelter.tscn");
	}

	private void LoadEncounteredEnemy()
	{
		SpriteFrames enemyFrames = GameState.Instance.CurrentEnemySpriteFrames;

		if (enemyFrames == null)
		{
			GD.PushError("CurrentEnemySpriteFrames is NULL!");
			return;
		}

		GD.Print("==============================");
		GD.Print("LOADING ENEMY SPRITE");
		GD.Print($"Enemy SpriteFrames: {enemyFrames}");
		GD.Print($"Animation count: {enemyFrames.GetAnimationNames().Length}");

		string[] animations = enemyFrames.GetAnimationNames();

		foreach (string animationName in animations)
		{
			GD.Print(
				$"Animation: {animationName} | " +
				$"Frames: {enemyFrames.GetFrameCount(animationName)}"
			);
		}

		if (animations.Length == 0)
		{
			GD.PushError("Enemy SpriteFrames contains NO animations!");
			return;
		}

		enemySprite.SpriteFrames = enemyFrames;

		string selectedAnimation = animations[0];

		enemySprite.Animation = selectedAnimation;
		enemySprite.Frame = 0;
		enemySprite.Visible = true;
		enemySprite.Play();

		GD.Print($"Playing animation: {selectedAnimation}");
		GD.Print($"Enemy visible: {enemySprite.Visible}");
		GD.Print($"Enemy position: {enemySprite.Position}");
		GD.Print($"Enemy scale: {enemySprite.Scale}");
		GD.Print("==============================");
	}

	private async void EndBattle()
	{
		battleOver = true;
		attackButton.Disabled = true;

		EnemyData data = GameState.Instance.CurrentEnemyData;
		int scrapDropped = data != null ? data.ScrapDrop : 10;
		int scrapAdded = GameState.Instance.AddRunScrap(scrapDropped);
		string fullNote = scrapAdded < scrapDropped ? " (inventory full)" : "";

		float healPercent = (float)GD.RandRange(10, 15) / 100f;
		int healAmount = Mathf.CeilToInt(playerStats.MaxHP * healPercent);

		playerStats.CurrentHP += healAmount;
		playerStats.CurrentHP = Mathf.Min(playerStats.CurrentHP, playerStats.MaxHP);

		statusLabel.Text = $"You won! +{scrapAdded} Scrap{fullNote}. Recovered {healAmount} HP.";
		UpdateLabels();
		GameState.Instance.PrintData();

		await ToSignal(GetTree().CreateTimer(1.5), Timer.SignalName.Timeout);

		Tween tween = CreateTween();
		tween.TweenProperty(fadeRect, "modulate:a", 1.0f, 0.6f);
		await ToSignal(tween, Tween.SignalName.Finished);

		GameState.Instance.DefeatNpc(GameState.Instance.CurrentNpcID);

		GetTree().ChangeSceneToFile("res://Scenes/main.tscn");
	}
}
