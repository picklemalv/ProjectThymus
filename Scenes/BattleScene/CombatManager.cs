using Godot;
using System;
using System.Collections.Generic;

public partial class CombatManager : Node2D
{
	private const float EscapeChance = 0.5f;
	private const float EscapeGraceSeconds = 4f;

	private Stats playerStats;
	private Stats enemyStats;
	private ZoneRule zone;
	private float zoneInfection = 2f;
	private BossSpecial bossSpecial;
	private int enemyAttackCount = 0;

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
		playerHPLabel = GetNode<Label>("UI/PlayerHPLabel");
		enemyHPLabel = GetNode<Label>("UI/EnemyHPLabel");
		infectionLabel = GetNode<Label>("UI/VBoxContainer/InfectionLabel");
		statusLabel = GetNode<Label>("UI/VBoxContainer/StatusLabel");
		attackButton = GetNode<Button>("UI/AttackButton");
		defendButton = GetNode<Button>("UI/DefendButton");
		escapeButton = GetNode<Button>("UI/EscapeButton");
		fadeRect = GetNode<ColorRect>("FadeLayer/FadeRect");
		playerSprite = GetNode<AnimatedSprite2D>("Player/AnimatedSprite2D");
		playerSprite.AnimationFinished += OnAnimationFinished;

		enemySprite = GetNode<AnimatedSprite2D>("NPC/AnimatedSprite2D");
		enemySprite.AnimationFinished += OnEnemyAnimationFinished;

		LoadEncounteredEnemy();

		playerStats = GameState.Instance.PlayerStats;
		zone = GameState.Instance.CurrentZoneRule;
		zoneInfection = zone.InfectionPerTurn;
		bossSpecial = GameDatabase.GetBossSpecial(GameDatabase.BossCoreIndex(GameState.Instance.CurrentNpcID));

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

		if (bossSpecial != null)
			statusLabel.Text += $"\nBOSS: {bossSpecial.Name} every {bossSpecial.EveryNAttacks} attacks!";

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
	
	private string GetPlayerAnimation(string baseName)
	{
		float infection = playerStats.CurrentInfection;

		string suffix = "";

		if (infection >= 75f)
			suffix = "75";
		else if (infection >= 50f)
			suffix = "50";

		string animationName = baseName + suffix;

		// Fall back to the original animation if a variant is missing.
		if (playerSprite.SpriteFrames.HasAnimation(animationName))
			return animationName;

		return baseName;
	}

	private void PlayPlayerAnimation(string baseName)
	{
		string animationName = GetPlayerAnimation(baseName);

		if (playerSprite.SpriteFrames.HasAnimation(animationName))
			playerSprite.Play(animationName);
	}

	private void BuildActionButtons()
	{
		// These buttons are placed in the BattleScene UI in the Godot editor.
		attackButton.Pressed += OnAttackPressed;
		defendButton.Pressed += OnDefendPressed;
		escapeButton.Pressed += OnEscapePressed;

		BindItemButton("UI/AntidoteButton", "antidote", "cure");
		BindItemButton("UI/HealthButton", "health", "heal", "medkit", "medical");
		BindItemButton(
			"UI/InfectionReductButton",
			"infectionreduct",
			"infectionreduction",
			"reduceinfection",
			"antivirus",
			"purge"
		);

		RefreshActionButtons();
	}

	private void BindItemButton(string nodePath, params string[] keywords)
	{
		Button button = GetNode<Button>(nodePath);
		ConsumableDef def = FindConsumable(keywords);

		if (def == null)
		{
			GD.PushWarning(
				$"No consumable matched button '{nodePath}'. " +
				$"Check the consumable IDs/names in GameDatabase."
			);
			button.Disabled = true;
			return;
		}

		if (itemButtons.ContainsKey(def.Id))
		{
			GD.PushWarning($"Consumable '{def.Id}' was matched to more than one UI button.");
			button.Disabled = true;
			return;
		}

		itemButtons[def.Id] = button;
		string id = def.Id;
		button.Pressed += () => OnItemPressed(id);
	}

	private ConsumableDef FindConsumable(params string[] keywords)
	{
		foreach (ConsumableDef def in GameDatabase.Consumables)
		{
			if (itemButtons.ContainsKey(def.Id))
				continue;

			string searchable = Normalize(def.Id + def.Name);

			foreach (string keyword in keywords)
			{
				if (searchable.Contains(Normalize(keyword)))
					return def;
			}
		}

		return null;
	}

	private string Normalize(string value)
	{
		return System.Text.RegularExpressions.Regex.Replace(
			value.ToLowerInvariant(),
			"[^a-z0-9]",
			""
		);
	}

	private void RefreshActionButtons()
	{
		bool canAct = actionsEnabled && !battleOver;

		attackButton.Disabled = !canAct;
		defendButton.Disabled = !canAct;
		escapeButton.Disabled = !canAct;

		foreach (ConsumableDef def in GameDatabase.Consumables)
		{
			if (!itemButtons.TryGetValue(def.Id, out Button button))
				continue;

			int count = GameState.Instance.GetRunItemCount(def.Id);
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

		enemyAttackCount++;
		
		enemySprite.Play("attack");

		bool special = bossSpecial != null && enemyAttackCount % bossSpecial.EveryNAttacks == 0;
		int attackValue = special
			? Mathf.RoundToInt(enemyStats.Attack * bossSpecial.DamageMultiplier)
			: enemyStats.Attack;

		int dmg = CalculateDamage(attackValue, playerStats.Defense);
		string guardText = "";

		if (defending)
		{
			dmg = Mathf.Max(dmg / 2, 1);
			defending = false;
			guardText = " (guarded)";
		}

		// After the final boss falls, the Runner cannot die on the way back.
		int hpFloor = GameState.Instance.CarryingFinalCore ? 1 : 0;
		playerStats.CurrentHP = Mathf.Max(playerStats.CurrentHP - dmg, hpFloor);
		// Play player getting-hit animation
		if (playerSprite.SpriteFrames.HasAnimation("hit"))
		{
			PlayPlayerAnimation("hit");
		}

		if (special)
		{
			playerStats.AddInfection(bossSpecial.InfectionGain);
			FlashPlayer();
		}

		UpdateLabels();

		if (playerStats.CurrentHP <= 0)
		{
			if (playerSprite.SpriteFrames.HasAnimation("die"))
			{
				PlayPlayerAnimation("die");
			}

			TriggerTimeLoop("You were overwhelmed...");
			return;
		}

		if (special && CheckInfectionLoop())
			return;

		string hitText;

		if (special)
		{
			string infectionText = bossSpecial.InfectionGain > 0f ? $" and +{bossSpecial.InfectionGain:0}% infection" : "";
			hitText = $"{bossSpecial.Name}! It hits you for {dmg}{guardText}{infectionText}!";
		}
		else
		{
			hitText = $"Enemy hit you for {dmg}{guardText}!";
		}

		string warning = "";

		if (bossSpecial != null && (enemyAttackCount + 1) % bossSpecial.EveryNAttacks == 0)
			warning = $"\n{bossSpecial.Name} is coming next turn. Defend to halve it!";

		statusLabel.Text = $"{playerText} {hitText}{warning}";
		SetActionsEnabled(true);
	}

	private void FlashPlayer()
	{
		playerSprite.Modulate = new Color(1f, 0.3f, 0.3f);

		Tween tween = CreateTween();
		tween.TweenProperty(playerSprite, "modulate", new Color(1f, 1f, 1f), 0.5f);
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
		PlayPlayerAnimation("attack");

		// Play NPC hit animation
		if (enemySprite.SpriteFrames.HasAnimation("hit"))
		{
			enemySprite.Play("hit");
		}

		playerStats.AddInfection(zoneInfection);

		int attackValue = playerStats.Attack + playerStats.WeaponDamage + attackBuffBonus;
		int dmgToEnemy = CalculateDamage(attackValue, enemyStats.Defense);
		enemyStats.CurrentHP = Mathf.Max(enemyStats.CurrentHP - dmgToEnemy, 0);

		TickBuff();
		UpdateLabels();

		// Killing the final boss always counts as a win, even if infection hit 100% on the same attack.
		bool killedFinalBoss = enemyStats.CurrentHP <= 0
			&& GameDatabase.BossCoreIndex(GameState.Instance.CurrentNpcID) == GameDatabase.CoreCount;

		if (!killedFinalBoss && CheckInfectionLoop())
			return;

		if (enemyStats.CurrentHP <= 0)
		{
			if (enemySprite.SpriteFrames.HasAnimation("die"))
			{
				enemySprite.Play("die");
			}

			EndBattle();
			return;
		}

		RunEnemyTurn($"You hit for {dmgToEnemy}!", 0.7f);
	}
	
	private void OnEnemyAnimationFinished()
	{
		if (enemySprite.Animation == "hit" ||
			enemySprite.Animation == "attack")
		{
			if (enemySprite.SpriteFrames.HasAnimation("default"))
				enemySprite.Play("default");
		}
	}

	private void OnDefendPressed()
	{
		if (battleOver || !actionsEnabled)
			return;

		defending = true;
		playerStats.AddInfection(zoneInfection);

		if (CheckInfectionLoop())
			return;

		RunEnemyTurn("You brace yourself.", 0.7f);
	}

	private async void OnEscapePressed()
	{
		if (battleOver || !actionsEnabled)
			return;

		playerStats.AddInfection(zoneInfection);

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

		if (def.PermanentInfectionReduction > 0)
		{
			GameState.Instance.ReduceBaseInfection(def.PermanentInfectionReduction);
			playerStats.AddInfection(-def.PermanentInfectionReduction);
			effects.Add($"starting infection -{def.PermanentInfectionReduction}%");
		}

		if (def.AttackBuff > 0)
		{
			attackBuffBonus = def.AttackBuff;
			attackBuffTurns = def.BuffTurns;
			effects.Add($"+{def.AttackBuff} ATK for {def.BuffTurns} attacks");
		}

		playerStats.AddInfection(zoneInfection);

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
		string currentAnimation = playerSprite.Animation;

		if (currentAnimation.StartsWith("attack", StringComparison.Ordinal) ||
			currentAnimation.StartsWith("hit", StringComparison.Ordinal))
		{
			PlayPlayerAnimation("default");
		}
	}

	private void UpdateLabels()
	{
		string buffText = attackBuffTurns > 0 ? $"  (ATK +{attackBuffBonus}, {attackBuffTurns} left)" : "";

		playerHPLabel.Text = $"Player HP: {playerStats.CurrentHP}/{playerStats.MaxHP}{buffText}";
		enemyHPLabel.Text = "Enemy HP: " + enemyStats.CurrentHP;
		infectionLabel.Text = $"Infection: {playerStats.CurrentInfection:0}%   ({zone.Name}: +{zone.InfectionPerTurn:0}% per turn)";
	}

	private async void TriggerTimeLoop(string reason)
	{
		if (battleOver)
			return;

		battleOver = true;
		SetActionsEnabled(false);
		statusLabel.Text = $"{reason} Time Loop activated...";
		UpdateLabels();

		if (enemySprite.SpriteFrames.HasAnimation("die"))
		{
			// Wait until the non-looping death animation finishes.
			await ToSignal(enemySprite, AnimatedSprite2D.SignalName.AnimationFinished);
		}
		else
		{
			await ToSignal(GetTree().CreateTimer(1.5), Timer.SignalName.Timeout);
		}

		Tween tween = CreateTween();
		tween.TweenProperty(fadeRect, "modulate:a", 1.0f, 0.6f);
		await ToSignal(tween, Tween.SignalName.Finished);

		GameState.Instance.FinishExpedition();
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

		// Always start with the idle animation
		string idleAnimation = enemyFrames.HasAnimation("default")
			? "default"
			: animations[0];

		// Position the NPC using the ground marker
		Marker2D marker = GetNode<Marker2D>("NPCMarker");

		enemySprite.Centered = true;

		Texture2D frameTexture = enemyFrames.GetFrameTexture(idleAnimation, 0);

		if (frameTexture != null)
		{
			enemySprite.Offset = new Vector2(
				0,
				-frameTexture.GetSize().Y / 2.0f
			);
		}

		enemySprite.GlobalPosition = marker.GlobalPosition;

		enemySprite.Stop();
		enemySprite.Animation = idleAnimation;
		enemySprite.Frame = 0;
		enemySprite.Visible = true;
		enemySprite.Play(idleAnimation);

		GD.Print($"Playing initial animation: {idleAnimation}");
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

		float rewardMultiplier = zone.RewardMultiplier;

		int scrapDrop = Mathf.RoundToInt((data != null ? data.ScrapDrop : 10) * rewardMultiplier);
		int materialDrop = Mathf.RoundToInt((data != null ? data.MaterialDrop : 2) * rewardMultiplier);
		int energyDrop = Mathf.RoundToInt((data != null ? data.EnergyDrop : 1) * rewardMultiplier);
		int essenceDrop = 0;

		if (data != null && GD.Randf() < data.EssenceDropChance)
			essenceDrop = Mathf.RoundToInt(data.EssenceDrop * rewardMultiplier);

		string coreText = "";
		int coreIndex = GameDatabase.BossCoreIndex(GameState.Instance.CurrentNpcID);

		if (coreIndex > 0 && GameState.Instance.TryAddRunCore(coreIndex))
			coreText = $" +Core Fragment {coreIndex}!";

		if (coreIndex == GameDatabase.CoreCount && GameState.Instance.CarryingFinalCore)
			coreText = " +FINAL CORE received! The source is destroyed!";

		bool bagFull = false;
		string loot =
			TryLoot(ResourceType.Scrap, scrapDrop, ref bagFull) +
			TryLoot(ResourceType.Material, materialDrop, ref bagFull) +
			TryLoot(ResourceType.Energy, energyDrop, ref bagFull) +
			TryLoot(ResourceType.Essence, essenceDrop, ref bagFull);

		if (loot == "" && coreText == "")
			loot = " nothing";

		string fullNote = bagFull ? " (bag full)" : "";

		float healPercent = (float)GD.RandRange(10, 15) / 100f;
		int healAmount = Mathf.CeilToInt(playerStats.MaxHP * healPercent);

		playerStats.CurrentHP += healAmount;
		playerStats.CurrentHP = Mathf.Min(playerStats.CurrentHP, playerStats.MaxHP);

		statusLabel.Text = $"You won!{loot}{coreText}{fullNote}. Recovered {healAmount} HP.";
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
