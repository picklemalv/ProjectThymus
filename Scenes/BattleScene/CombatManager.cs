using Godot;
using System;
using System.Collections.Generic;

public partial class CombatManager : Node2D
{
	private const float InfectionGainPerTurn = 2f;
	private const float EscapeChance = 0.5f;
	private const float EscapeGraceSeconds = 4f;

	private Stats playerStats;
	private Stats enemyStats;

	private bool battleOver = false;
	private bool actionsEnabled = true;
	private bool defending = false;

	private int attackBuffBonus = 0;
	private int attackBuffTurns = 0;

	private Label playerHPLabel;
	private Label enemyHPLabel;
	private Label infectionLabel;
	private Label statusLabel;
	private Button attackButton;
	private Button defendButton;
	private Button escapeButton;
	private ColorRect fadeRect;

	private AnimatedSprite2D playerSprite;
	private AnimatedSprite2D enemySprite;

	private readonly Dictionary<string, Button> itemButtons = new Dictionary<string, Button>();

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

		BuildActionButtons();

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
		playerStats.PrintStats();
		GD.Print("==============================");

		if (enemyStats.Speed > playerStats.Speed)
		{
			SetActionsEnabled(false);
			CallDeferred(nameof(EnemyOpeningAttack));
		}
		else
		{
			SetActionsEnabled(true);
		}
	}

	private void BuildActionButtons()
	{
		VBoxContainer box = GetNode<VBoxContainer>("UI/VBoxContainer");

		HBoxContainer actionRow = new HBoxContainer();
		actionRow.AddThemeConstantOverride("separation", 8);
		box.AddChild(actionRow);

		defendButton = new Button();
		defendButton.Text = "Defend";
		defendButton.TooltipText = "Take half damage from the next enemy attack.";
		defendButton.Pressed += OnDefendPressed;
		actionRow.AddChild(defendButton);

		escapeButton = new Button();
		escapeButton.Text = "Escape (50%)";
		escapeButton.TooltipText = "50% chance to flee the battle.";
		escapeButton.Pressed += OnEscapePressed;
		actionRow.AddChild(escapeButton);

		HBoxContainer itemRow = new HBoxContainer();
		itemRow.AddThemeConstantOverride("separation", 8);
		box.AddChild(itemRow);

		foreach (ConsumableDef def in GameDatabase.Consumables)
		{
			string id = def.Id;

			Button button = new Button();
			button.Pressed += () => OnItemPressed(id);
			itemRow.AddChild(button);

			itemButtons[id] = button;
		}

		RefreshActionButtons();
	}

	private void RefreshActionButtons()
	{
		bool canAct = actionsEnabled && !battleOver;

		attackButton.Disabled = !canAct;
		defendButton.Disabled = !canAct;
		escapeButton.Disabled = !canAct;

		foreach (ConsumableDef def in GameDatabase.Consumables)
		{
			int count = GameState.Instance.GetRunItemCount(def.Id);
			Button button = itemButtons[def.Id];

			button.Text = $"{def.Name} x{count}";
			button.TooltipText = def.Description;
			button.Disabled = !canAct || count <= 0;
		}
	}

	private void SetActionsEnabled(bool enabled)
	{
		actionsEnabled = enabled;
		RefreshActionButtons();
	}

	private async void EnemyOpeningAttack()
	{
		await ToSignal(GetTree().CreateTimer(1.0), Timer.SignalName.Timeout);

		if (battleOver)
			return;

		RunEnemyTurn("Enemy is faster! It strikes first!", 0.4f);
	}

	private async void RunEnemyTurn(string playerText, float delay)
	{
		SetActionsEnabled(false);
		statusLabel.Text = playerText;
		UpdateLabels();

		await ToSignal(GetTree().CreateTimer(delay), Timer.SignalName.Timeout);

		if (battleOver)
			return;

		int dmg = CalculateDamage(enemyStats.Attack, playerStats.Defense);
		string guardText = "";

		if (defending)
		{
			dmg = Mathf.Max(dmg / 2, 1);
			defending = false;
			guardText = " (guarded)";
		}

		playerStats.CurrentHP = Mathf.Max(playerStats.CurrentHP - dmg, 0);
		UpdateLabels();

		if (playerStats.CurrentHP <= 0)
		{
			TriggerTimeLoop("You were overwhelmed...");
			return;
		}

		statusLabel.Text = $"{playerText} Enemy hit you for {dmg}{guardText}!";
		SetActionsEnabled(true);
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

	private bool CheckInfectionLoop()
	{
		if (!playerStats.IsInfectionMaxed())
			return false;

		UpdateLabels();
		TriggerTimeLoop("Infection reached 100%!");
		return true;
	}

	private void OnAttackPressed()
	{
		if (battleOver || !actionsEnabled)
			return;

		PerformAttack();
	}

	private void PerformAttack()
	{
		playerSprite.Play("attack");

		playerStats.AddInfection(InfectionGainPerTurn);

		int attackValue = playerStats.Attack + playerStats.WeaponDamage + attackBuffBonus;
		int dmgToEnemy = CalculateDamage(attackValue, enemyStats.Defense);
		enemyStats.CurrentHP = Mathf.Max(enemyStats.CurrentHP - dmgToEnemy, 0);

		TickBuff();
		UpdateLabels();

		if (CheckInfectionLoop())
			return;

		if (enemyStats.CurrentHP <= 0)
		{
			EndBattle();
			return;
		}

		RunEnemyTurn($"You hit for {dmgToEnemy}!", 0.7f);
	}

	private void OnDefendPressed()
	{
		if (battleOver || !actionsEnabled)
			return;

		defending = true;
		playerStats.AddInfection(InfectionGainPerTurn);

		if (CheckInfectionLoop())
			return;

		RunEnemyTurn("You brace yourself.", 0.7f);
	}

	private async void OnEscapePressed()
	{
		if (battleOver || !actionsEnabled)
			return;

		playerStats.AddInfection(InfectionGainPerTurn);

		if (CheckInfectionLoop())
			return;

		if (GD.Randf() >= EscapeChance)
		{
			RunEnemyTurn("You failed to escape!", 0.7f);
			return;
		}

		battleOver = true;
		SetActionsEnabled(false);
		statusLabel.Text = "You escaped!";
		UpdateLabels();

		GameState.Instance.StartEscapeGrace(EscapeGraceSeconds);

		await ToSignal(GetTree().CreateTimer(1.0), Timer.SignalName.Timeout);

		Tween tween = CreateTween();
		tween.TweenProperty(fadeRect, "modulate:a", 1.0f, 0.6f);
		await ToSignal(tween, Tween.SignalName.Finished);

		GetTree().ChangeSceneToFile("res://Scenes/main.tscn");
	}

	private void OnItemPressed(string id)
	{
		if (battleOver || !actionsEnabled)
			return;

		ConsumableDef def = GameDatabase.GetConsumable(id);

		if (def == null || !GameState.Instance.UseRunItem(id))
			return;

		List<string> effects = new List<string>();

		if (def.Heal > 0)
		{
			int hpBefore = playerStats.CurrentHP;
			playerStats.CurrentHP = Mathf.Min(playerStats.CurrentHP + def.Heal, playerStats.MaxHP);
			effects.Add($"+{playerStats.CurrentHP - hpBefore} HP");
		}

		if (def.InfectionReduction > 0)
		{
			float infectionBefore = playerStats.CurrentInfection;
			playerStats.AddInfection(-def.InfectionReduction);
			effects.Add($"-{infectionBefore - playerStats.CurrentInfection:0}% infection");
		}

		if (def.AttackBuff > 0)
		{
			attackBuffBonus = def.AttackBuff;
			attackBuffTurns = def.BuffTurns;
			effects.Add($"+{def.AttackBuff} ATK for {def.BuffTurns} attacks");
		}

		playerStats.AddInfection(InfectionGainPerTurn);

		if (CheckInfectionLoop())
			return;

		RunEnemyTurn($"Used {def.Name} ({string.Join(", ", effects)}).", 0.7f);
	}

	private void TickBuff()
	{
		if (attackBuffTurns <= 0)
			return;

		attackBuffTurns--;

		if (attackBuffTurns == 0)
			attackBuffBonus = 0;
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
		string buffText = attackBuffTurns > 0 ? $"  (ATK +{attackBuffBonus}, {attackBuffTurns} left)" : "";

		playerHPLabel.Text = $"Player HP: {playerStats.CurrentHP}/{playerStats.MaxHP}{buffText}";
		enemyHPLabel.Text = "Enemy HP: " + enemyStats.CurrentHP;
		infectionLabel.Text = $"Infection: {playerStats.CurrentInfection:0}%";
	}

	private async void TriggerTimeLoop(string reason)
	{
		if (battleOver)
			return;

		battleOver = true;
		SetActionsEnabled(false);
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

	private string TryLoot(ResourceType type, int amount, ref bool bagFull)
	{
		if (amount <= 0)
			return "";

		int added = GameState.Instance.AddRun(type, amount);

		if (added < amount)
			bagFull = true;

		return added > 0 ? $" +{added} {type}" : "";
	}

	private async void EndBattle()
	{
		battleOver = true;
		SetActionsEnabled(false);

		EnemyData data = GameState.Instance.CurrentEnemyData;

		int scrapDrop = data != null ? data.ScrapDrop : 10;
		int materialDrop = data != null ? data.MaterialDrop : 2;
		int energyDrop = data != null ? data.EnergyDrop : 1;
		int essenceDrop = 0;

		if (data != null && GD.Randf() < data.EssenceDropChance)
			essenceDrop = data.EssenceDrop;

		bool bagFull = false;
		string loot =
			TryLoot(ResourceType.Scrap, scrapDrop, ref bagFull) +
			TryLoot(ResourceType.Material, materialDrop, ref bagFull) +
			TryLoot(ResourceType.Energy, energyDrop, ref bagFull) +
			TryLoot(ResourceType.Essence, essenceDrop, ref bagFull);

		if (loot == "")
			loot = " nothing";

		string fullNote = bagFull ? " (bag full)" : "";

		float healPercent = (float)GD.RandRange(10, 15) / 100f;
		int healAmount = Mathf.CeilToInt(playerStats.MaxHP * healPercent);

		playerStats.CurrentHP += healAmount;
		playerStats.CurrentHP = Mathf.Min(playerStats.CurrentHP, playerStats.MaxHP);

		statusLabel.Text = $"You won!{loot}{fullNote}. Recovered {healAmount} HP.";
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
