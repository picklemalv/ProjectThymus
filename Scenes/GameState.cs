using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public enum ExpeditionStatus
{
	Active,
	Completed,
	Failed
}

public class SaveData
{
	public int Scrap { get; set; }
	public int Material { get; set; }
	public int Energy { get; set; }
	public int Essence { get; set; }
	public List<int> CoresOwned { get; set; } = new List<int>();
	public List<int> CoresInstalled { get; set; } = new List<int>();
	public bool GameWon { get; set; }
	public float PermanentInfection { get; set; }
	public int DeathCount { get; set; }
	public int StorageLevel { get; set; } = 1;
	public int ArmoryLevel { get; set; } = 1;
	public int InfirmaryLevel { get; set; } = 1;
	public int WorkshopLevel { get; set; } = 1;
	public List<string> OwnedEquipment { get; set; } = new List<string>();
	public Dictionary<string, string> Equipped { get; set; } = new Dictionary<string, string>();
	public bool GearUpQuestDone { get; set; }
	public bool IntroSeen { get; set; }
	public Dictionary<string, int> Items { get; set; } = new Dictionary<string, int>();
}

public partial class GameState : Node
{
	public static GameState Instance { get; private set; }

	public const int MaxRunScrap = GameDatabase.RunCapScrap;
	public const int MaxRunMaterial = GameDatabase.RunCapMaterial;
	public const int MaxRunEnergy = GameDatabase.RunCapEnergy;
	public const int MaxRunEssence = GameDatabase.RunCapEssence;
	public const int MaxRunCoreFragments = GameDatabase.CoreCount;
	public const int ItemCarryLimit = 3;

	private const string SavePath = "user://thymus_save.json";

	public bool SpawnAtInfirmary = false;
	public bool SpawnAtGate = false;

	public bool HasReturnPosition = false;
	public Vector2 ReturnPosition = Vector2.Zero;

	private HashSet<string> defeatedNPCs = new HashSet<string>();

	public string CurrentNpcID = "";
	public EnemyData CurrentEnemyData;
	public SpriteFrames CurrentEnemySpriteFrames;

	public Stats PlayerStats;

	private readonly Dictionary<ResourceType, int> permanent = new Dictionary<ResourceType, int>();
	private readonly Dictionary<ResourceType, int> run = new Dictionary<ResourceType, int>();
	private readonly Dictionary<string, int> permanentItems = new Dictionary<string, int>();
	private readonly Dictionary<string, int> runItems = new Dictionary<string, int>();
	private readonly HashSet<string> ownedEquipment = new HashSet<string>();

	private readonly bool[] coreOwned = new bool[GameDatabase.CoreCount + 1];
	private readonly bool[] coreInstalled = new bool[GameDatabase.CoreCount + 1];
	private readonly HashSet<int> runCores = new HashSet<int>();

	public int CurrentZone = 1;
	public bool GameWon = false;
	public float PermanentInfection = 0f;
	public int DeathCount = 0;
	public int LastGameLoops = 0;
	public bool GameLostPending = false;
	public Vector2 ExpeditionStartPosition = Vector2.Zero;

	private bool hasStartPosition = false;
	private bool retreatBannerShown = false;
	private Area2D shelterExit;
	private ColorRect retreatTint;
	private Label subtitleLabel;
	private Tween subtitleTween;
	private CpuParticles2D ashParticles;
	private Node2D playerNode;
	private float retreatTime = 0f;
	private int nextRetreatLine = 0;
	private bool nearExitShown = false;

	private static readonly (float Time, string Text)[] RetreatLines = new (float, string)[]
	{
		(7f, "The fog is parting."),
		(24f, "Every step feels heavier than the last."),
		(48f, "Keep going. The shelter is waiting."),
		(80f, "Almost there.")
	};

	private CanvasLayer hudLayer;
	private Label meterLabel;
	public int WorkshopLevel = 1;

	public ZoneRule CurrentZoneRule => GameDatabase.GetZone(CurrentZone);

	public bool CarryingFinalCore => runCores.Contains(GameDatabase.CoreCount);

	// True from the moment the final core drops until it is installed in the Core Chamber.
	public bool HoldingFinalCore =>
		CarryingFinalCore || (IsCoreOwned(GameDatabase.CoreCount) && !IsCoreInstalled(GameDatabase.CoreCount));

	public float MoveSpeedMultiplier => Fading ? 0f : (HoldingFinalCore ? 0.6f : 1f);

	// True while the death fade-out / fade-in is playing. Freezes the player and blocks new fights.
	public bool Fading { get; private set; } = false;

	private const float DeathFadeOutSeconds = 1.0f;
	private const float FlashRiseSeconds = 0.4f;
	private const float FlashHoldSeconds = 1.6f;
	private const float FlashFadeSeconds = 2.6f;
	private const float DeathFadeInSeconds = 1.2f;
	private CanvasLayer fadeLayer;
	private ColorRect fadeRect;

	// True when the save holds anything beyond a fresh start (used by the main menu's Continue button).
	public bool HasProgress =>
		DeathCount > 0 || GearUpQuestDone || StorageLevel > 1 || ArmoryLevel > 1 ||
		InfirmaryLevel > 1 || WorkshopLevel > 1 || PermanentCoreFragments > 0 || InstalledCoreCount > 0;

	public int TriesLeft
	{
		get
		{
			float penalty = GameDatabase.DeathInfectionPenalty(InfirmaryLevel);
			return Mathf.Max(1, Mathf.CeilToInt((Stats.MaxInfection - PermanentInfection) / penalty));
		}
	}

	public int RunCoreFragments => runCores.Count;

	public int PermanentCoreFragments
	{
		get
		{
			int count = 0;
			for (int i = 1; i <= GameDatabase.CoreCount; i++)
			{
				if (coreOwned[i] && !coreInstalled[i])
					count++;
			}
			return count;
		}
	}

	public int InstalledCoreCount
	{
		get
		{
			int count = 0;
			for (int i = 1; i <= GameDatabase.CoreCount; i++)
			{
				if (coreInstalled[i])
					count++;
			}
			return count;
		}
	}

	public int InstalledUpgradeCores
	{
		get
		{
			int count = 0;
			for (int i = 1; i < GameDatabase.CoreCount; i++)
			{
				if (coreInstalled[i])
					count++;
			}
			return count;
		}
	}

	public int StorageLevel = 1;
	public int ArmoryLevel = 1;
	public int InfirmaryLevel = 1;

	public ulong EscapeGraceUntilMs = 0;

	private readonly Dictionary<EquipSlot, string> equipped = new Dictionary<EquipSlot, string>();
	private float equipmentInfectionResist = 0f;

	public bool GearUpQuestDone = false;
	public bool IntroSeen = false;

	public ExpeditionStatus CurrentExpeditionStatus = ExpeditionStatus.Active;
	public string LastExpeditionMessage = "Ready for expedition.";

	public int PermanentScrap => GetPermanent(ResourceType.Scrap);
	public int PermanentMaterial => GetPermanent(ResourceType.Material);
	public int PermanentEnergy => GetPermanent(ResourceType.Energy);
	public int PermanentEssence => GetPermanent(ResourceType.Essence);

	public int RunScrap => GetRun(ResourceType.Scrap);
	public int RunMaterial => GetRun(ResourceType.Material);
	public int RunEnergy => GetRun(ResourceType.Energy);
	public int RunEssence => GetRun(ResourceType.Essence);

	public override void _Ready()
	{
		Instance = this;

		GameSettings.Load();

		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
		{
			permanent[type] = 0;
			run[type] = 0;
		}

		foreach (ConsumableDef def in GameDatabase.Consumables)
		{
			permanentItems[def.Id] = 0;
			runItems[def.Id] = 0;
		}

		PlayerStats = new Stats();
		AddChild(PlayerStats);

		BuildExplorationHud();
		BuildFadeOverlay();

		bool hasSave = FileAccess.FileExists(SavePath);

		LoadGame();

		if (!hasSave)
			GrantStarterKit();

		ApplyEquipment();
		UpdateQuestState();
		ApplyStationBonuses();
		ResetRunState();
	}

	public int GetPermanent(ResourceType type)
	{
		return permanent[type];
	}

	public int GetRun(ResourceType type)
	{
		return run[type];
	}

	public int AddPermanent(ResourceType type, int amount)
	{
		if (amount <= 0)
			return 0;

		int cap = GameDatabase.StorageCap(StorageLevel, type);
		int before = permanent[type];
		permanent[type] = Mathf.Min(before + amount, Mathf.Max(cap, before));
		return permanent[type] - before;
	}

	public int AddRun(ResourceType type, int amount)
	{
		if (amount <= 0)
			return 0;

		int cap = GameDatabase.RunCap(StorageLevel, type);
		int before = run[type];
		run[type] = Mathf.Min(before + amount, cap);
		return run[type] - before;
	}

	public int AddRunScrap(int amount)
	{
		return AddRun(ResourceType.Scrap, amount);
	}

	public int AddRunEnergy(int amount)
	{
		return AddRun(ResourceType.Energy, amount);
	}

	public bool TryAddRunCore(int index)
	{
		if (index < 1 || index > GameDatabase.CoreCount)
			return false;

		if (coreOwned[index] || runCores.Contains(index))
			return false;

		runCores.Add(index);
		return true;
	}

	public bool IsCoreOwned(int index)
	{
		return index >= 1 && index <= GameDatabase.CoreCount && coreOwned[index];
	}

	public bool IsCoreInstalled(int index)
	{
		return index >= 1 && index <= GameDatabase.CoreCount && coreInstalled[index];
	}

	public string InstallCore(int index)
	{
		if (index < 1 || index > GameDatabase.CoreCount)
			return "Unknown core.";
		if (coreInstalled[index])
			return "That core is already installed.";
		if (!coreOwned[index])
			return "You don't have that core.";

		coreInstalled[index] = true;

		if (index == GameDatabase.CoreCount)
			GameWon = true;

		SaveGame();

		if (index == GameDatabase.CoreCount)
			return "The final core hums to life.";

		return $"Core {index} installed. Facilities can be upgraded further at the Workshop.";
	}

	public int GetMaxFacilityLevel()
	{
		return Mathf.Min(GameDatabase.MaxStationLevel, 1 + InstalledUpgradeCores);
	}

	public int GetStationLevel(StationType station)
	{
		switch (station)
		{
			case StationType.Storage:
				return StorageLevel;
			case StationType.Armory:
				return ArmoryLevel;
			case StationType.Infirmary:
				return InfirmaryLevel;
			case StationType.Workshop:
				return WorkshopLevel;
			default:
				return 1;
		}
	}

	public ResourceCost GetCost(ResourceCost baseCost)
	{
		float multiplier = 1f - GameDatabase.WorkshopDiscount(WorkshopLevel);

		return new ResourceCost(
			baseCost.Scrap > 0 ? Mathf.Max(1, Mathf.CeilToInt(baseCost.Scrap * multiplier)) : 0,
			baseCost.Material > 0 ? Mathf.Max(1, Mathf.CeilToInt(baseCost.Material * multiplier)) : 0,
			baseCost.Energy > 0 ? Mathf.Max(1, Mathf.CeilToInt(baseCost.Energy * multiplier)) : 0,
			baseCost.Essence > 0 ? Mathf.Max(1, Mathf.CeilToInt(baseCost.Essence * multiplier)) : 0
		);
	}

	public ResourceCost GetUpgradeCost(StationType station)
	{
		ResourceCost baseCost = GameDatabase.UpgradeCost(station, GetStationLevel(station));

		if (baseCost == null)
			return null;

		return station == StationType.Workshop ? baseCost : GetCost(baseCost);
	}

	public string UpgradeBlockReason(StationType station)
	{
		if (station == StationType.CoreChamber)
			return "Nothing to upgrade.";

		int level = GetStationLevel(station);

		if (level >= GameDatabase.MaxStationLevel)
			return "Already at max level.";

		if (level + 1 > GetMaxFacilityLevel())
			return $"Needs {level} boss core(s) installed in the Core Chamber.";

		return "";
	}

	public void StartEscapeGrace(float seconds)
	{
		EscapeGraceUntilMs = Time.GetTicksMsec() + (ulong)(seconds * 1000f);
	}

	public bool IsEscapeGraceActive()
	{
		return Time.GetTicksMsec() < EscapeGraceUntilMs;
	}

	public void ApplyStationBonuses()
	{
		PlayerStats.InfectionGainMultiplier =
			(1f - GameDatabase.InfectionResistance(InfirmaryLevel)) * (1f - equipmentInfectionResist);
	}

	public bool CanAfford(ResourceCost cost)
	{
		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
		{
			if (permanent[type] < cost.Get(type))
				return false;
		}

		return true;
	}

	private void Spend(ResourceCost cost)
	{
		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
			permanent[type] -= cost.Get(type);
	}

	public int GetPermanentItemCount(string id)
	{
		return permanentItems.TryGetValue(id, out int count) ? count : 0;
	}

	public int GetRunItemCount(string id)
	{
		return runItems.TryGetValue(id, out int count) ? count : 0;
	}

	public bool UseRunItem(string id)
	{
		if (GetRunItemCount(id) <= 0)
			return false;

		runItems[id]--;
		return true;
	}

	public bool OwnsEquipment(string id)
	{
		return ownedEquipment.Contains(id);
	}

	public bool IsEquipped(string id)
	{
		foreach (KeyValuePair<EquipSlot, string> pair in equipped)
		{
			if (pair.Value == id)
				return true;
		}

		return false;
	}

	public string GetEquippedId(EquipSlot slot)
	{
		return equipped.TryGetValue(slot, out string id) ? id : null;
	}

	public int GetEquippedTier(EquipSlot slot)
	{
		EquipmentDef def = GameDatabase.GetEquipment(GetEquippedId(slot));
		return def != null ? def.Tier : 0;
	}

	public bool AllSlotsEquipped()
	{
		foreach (EquipSlot slot in GameDatabase.AllSlots)
		{
			if (!equipped.ContainsKey(slot))
				return false;
		}

		return true;
	}

	public void UpdateQuestState()
	{
		if (GearUpQuestDone || !AllSlotsEquipped())
			return;

		GearUpQuestDone = true;
		LastExpeditionMessage = "Gear up complete. You are ready to head out.";
	}

	public void ApplyEquipment()
	{
		int weaponDamage = 0;
		int defense = 0;
		int hp = 0;
		int speed = 0;
		float resist = 0f;

		foreach (KeyValuePair<EquipSlot, string> pair in equipped)
		{
			EquipmentDef def = GameDatabase.GetEquipment(pair.Value);

			if (def == null)
				continue;

			weaponDamage += def.WeaponDamage;
			defense += def.Defense;
			hp += def.HP;
			speed += def.Speed;
			resist += def.InfectionResist;
		}

		PlayerStats.WeaponDamage = weaponDamage;
		PlayerStats.EquipmentAttackBonus = 0;
		PlayerStats.EquipmentDefenseBonus = defense;
		PlayerStats.EquipmentHPBonus = hp;
		PlayerStats.EquipmentSpeedBonus = speed;

		equipmentInfectionResist = Mathf.Min(resist, 0.5f);
		ApplyStationBonuses();

		PlayerStats.CurrentHP = Mathf.Min(PlayerStats.CurrentHP, PlayerStats.MaxHP);
	}

	public string EquipItem(string id)
	{
		EquipmentDef def = GameDatabase.GetEquipment(id);

		if (def == null || !ownedEquipment.Contains(id))
			return "You don't own that.";

		equipped[def.Slot] = id;

		ApplyEquipment();
		UpdateQuestState();
		SaveGame();
		return $"Equipped {def.Name}.";
	}

	private void GrantStarterKit()
	{
		AddPermanent(ResourceType.Scrap, GameDatabase.StarterScrap);
		AddPermanent(ResourceType.Material, GameDatabase.StarterMaterial);
		LastExpeditionMessage = "Welcome, Runner. Gear up at the Armory before heading out.";
		SaveGame();
	}

	private Dictionary<string, string> GetEquippedMap()
	{
		Dictionary<string, string> map = new Dictionary<string, string>();

		foreach (KeyValuePair<EquipSlot, string> pair in equipped)
			map[pair.Key.ToString()] = pair.Value;

		return map;
	}

	public void ReduceBaseInfection(float amount)
	{
		PermanentInfection = Mathf.Max(0f, PermanentInfection - amount);
	}

	public string UseInfectionReducerFromStock()
	{
		ConsumableDef def = GameDatabase.GetConsumable("infection_reducer");

		if (def == null || GetPermanentItemCount(def.Id) <= 0)
			return "You have no Infection Reducer in stock.";

		if (PermanentInfection <= 0f)
			return "Your base infection is already 0%.";

		permanentItems[def.Id] = GetPermanentItemCount(def.Id) - 1;
		ReduceBaseInfection(def.PermanentInfectionReduction);
		SaveGame();

		return $"Base infection lowered to {PermanentInfection:0}%.";
	}

	public string CraftEquipment(string id)
	{
		EquipmentDef def = GameDatabase.GetEquipment(id);

		if (def == null)
			return "Unknown item.";
		if (ownedEquipment.Contains(id))
			return "Already crafted.";
		if (def.RequiredArmoryLevel > ArmoryLevel)
			return $"Requires Armory Level {def.RequiredArmoryLevel}.";
		ResourceCost cost = GetCost(def.Cost);

		if (!CanAfford(cost))
			return "Not enough resources.";

		Spend(cost);
		ownedEquipment.Add(id);
		SaveGame();
		return $"Crafted {def.Name}.";
	}

	public string CraftConsumable(string id)
	{
		ConsumableDef def = GameDatabase.GetConsumable(id);

		if (def == null)
			return "Unknown item.";

		if (def.RequiredInfirmaryLevel > InfirmaryLevel)
			return $"Requires Infirmary Level {def.RequiredInfirmaryLevel}.";
		ResourceCost cost = GetCost(def.Cost);

		if (!CanAfford(cost))
			return "Not enough resources.";

		Spend(cost);
		permanentItems[id] = GetPermanentItemCount(id) + 1;
		SaveGame();
		return $"Crafted {def.Name}.";
	}

	public string UpgradeStation(StationType station)
	{
		string blocked = UpgradeBlockReason(station);

		if (blocked != "")
			return blocked;

		ResourceCost cost = GetUpgradeCost(station);

		if (cost == null)
			return "Nothing to upgrade.";
		if (!CanAfford(cost))
			return "Not enough resources.";

		Spend(cost);

		switch (station)
		{
			case StationType.Storage:
				StorageLevel++;
				break;
			case StationType.Armory:
				ArmoryLevel++;
				break;
			case StationType.Infirmary:
				InfirmaryLevel++;
				break;
			case StationType.Workshop:
				WorkshopLevel++;
				break;
		}

		ApplyStationBonuses();
		SaveGame();
		return $"{GameDatabase.StationDisplayName(station)} upgraded to Level {GetStationLevel(station)}.";
	}

	private List<int> GetCoreList(bool[] flags)
	{
		List<int> list = new List<int>();

		for (int i = 1; i <= GameDatabase.CoreCount; i++)
		{
			if (flags[i])
				list.Add(i);
		}

		return list;
	}

	public void ResetRunState()
	{
		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
			run[type] = 0;

		runCores.Clear();
		CurrentZone = 1;
		retreatBannerShown = false;

		PlayerStats.CurrentHP = PlayerStats.MaxHP;
		PlayerStats.SetInfection(PermanentInfection);

		ClearDefeatedNPCs();
		HasReturnPosition = false;

		CurrentExpeditionStatus = ExpeditionStatus.Active;
	}

	public void StartExpedition()
	{
		ReturnRunItems();
		ResetRunState();
		SpawnAtInfirmary = false;
		SpawnAtGate = false;

		foreach (ConsumableDef def in GameDatabase.Consumables)
		{
			int take = Mathf.Min(GetPermanentItemCount(def.Id), ItemCarryLimit);
			permanentItems[def.Id] = GetPermanentItemCount(def.Id) - take;
			runItems[def.Id] = take;
		}

		LastExpeditionMessage = "Ready for expedition.";
	}

	private void ReturnRunItems()
	{
		foreach (ConsumableDef def in GameDatabase.Consumables)
		{
			permanentItems[def.Id] = GetPermanentItemCount(def.Id) + GetRunItemCount(def.Id);
			runItems[def.Id] = 0;
		}
	}

	public void EndExpedition()
	{
		List<string> lost = BankRunLoot();

		DeathCount++;
		CurrentExpeditionStatus = ExpeditionStatus.Failed;
		SpawnAtInfirmary = true;
		SpawnAtGate = false;

		float penalty = 0f;

		if (!GameWon)
		{
			penalty = GameDatabase.DeathInfectionPenalty(InfirmaryLevel);
			PermanentInfection = Mathf.Min(PermanentInfection + penalty, Stats.MaxInfection);
		}

		if (PermanentInfection >= Stats.MaxInfection)
		{
			LastGameLoops = DeathCount;
			GameLostPending = true;
			GD.Print($"Game lost after {LastGameLoops} loops.");
			ResetAllProgress();
			return;
		}

		LastExpeditionMessage = GameWon
			? "You fell, but the loop is already broken."
			: $"You died. Loot recovered. Base infection +{penalty:0}% (now {PermanentInfection:0}%).";

		if (lost.Count > 0)
			LastExpeditionMessage += $" Storage full, lost: {string.Join(", ", lost)}.";

		GD.Print($"Expedition ended: {CurrentExpeditionStatus}");
		PrintData();
		SaveGame();
	}

	private List<string> BankRunLoot()
	{
		List<string> lost = new List<string>();

		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
		{
			int amount = run[type];
			int added = AddPermanent(type, amount);
			if (added < amount)
				lost.Add($"{amount - added} {type}");
			run[type] = 0;
		}

		foreach (int index in runCores)
			coreOwned[index] = true;

		runCores.Clear();
		ReturnRunItems();

		return lost;
	}

	public void SetExpeditionStart(Vector2 position)
	{
		ExpeditionStartPosition = position;
		hasStartPosition = true;
	}

	public void CompleteExpedition()
	{
		if (CurrentExpeditionStatus != ExpeditionStatus.Active)
			return;

		List<string> lost = BankRunLoot();

		CurrentExpeditionStatus = ExpeditionStatus.Completed;
		SpawnAtInfirmary = false;
		SpawnAtGate = true;
		retreatBannerShown = false;

		LastExpeditionMessage = "You made it back with the final core. Install it in the Core Chamber.";

		if (lost.Count > 0)
			LastExpeditionMessage += $" Storage full, lost: {string.Join(", ", lost)}.";

		PrintData();
		SaveGame();

		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://Scenes/Shelter/Shelter.tscn");
	}

	private const string PixelFontPath = "res://Assets/ThymusAssets/pixel_ui/font/pixel_ui_font.fnt";
	private static Font pixelFont;

	// Same pixel font as the HUD and menus. The font is drawn at size 8, so use multiples of 8.
	private static void ApplyPixelFont(Control control, int size)
	{
		if (pixelFont == null && ResourceLoader.Exists(PixelFontPath))
			pixelFont = GD.Load<Font>(PixelFontPath);

		if (pixelFont != null)
			control.AddThemeFontOverride("font", pixelFont);

		control.AddThemeFontSizeOverride("font_size", size);
		control.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
	}

	private void EnsureShelterExit()
	{
		if (!hasStartPosition)
			return;

		if (shelterExit != null && IsInstanceValid(shelterExit) && shelterExit.IsInsideTree())
			return;

		Node scene = GetTree().CurrentScene;

		if (scene == null)
			return;

		shelterExit = new Area2D();

		CollisionShape2D shape = new CollisionShape2D();
		CircleShape2D circle = new CircleShape2D();
		circle.Radius = 70f;
		shape.Shape = circle;
		shelterExit.AddChild(shape);

		ShelterMarker marker = new ShelterMarker();
		marker.Radius = 70f;
		shelterExit.AddChild(marker);

		Label label = new Label();
		label.Text = "SHELTER";
		ApplyPixelFont(label, 16);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.Size = new Vector2(140f, 24f);
		label.Position = new Vector2(-70f, -106f);
		shelterExit.AddChild(label);

		shelterExit.BodyEntered += OnShelterExitEntered;

		scene.AddChild(shelterExit);
		shelterExit.GlobalPosition = ExpeditionStartPosition;
	}

	private void OnShelterExitEntered(Node2D body)
	{
		if (body is not Player)
			return;

		CompleteExpedition();
	}

	// Ends the run (death or full infection) with a fade to black, then returns to the shelter.
	public void FinishExpedition()
	{
		if (Fading)
			return;

		Fading = true;
		FadeOutAndFinish();
	}

	private void BuildFadeOverlay()
	{
		fadeLayer = new CanvasLayer();
		fadeLayer.Layer = 100;
		fadeLayer.ProcessMode = ProcessModeEnum.Always;
		AddChild(fadeLayer);

		fadeRect = new ColorRect();
		fadeRect.Color = Colors.Black;
		fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
		fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		fadeRect.Modulate = new Color(1f, 1f, 1f, 0f);
		fadeLayer.AddChild(fadeRect);
	}

	private async void FadeOutAndFinish()
	{
		Tween fadeOut = fadeRect.CreateTween();
		fadeOut.TweenProperty(fadeRect, "modulate:a", 1.0f, DeathFadeOutSeconds);
		await ToSignal(fadeOut, Tween.SignalName.Finished);

		EndExpedition();

		string path = GameLostPending
			? "res://Scenes/GameOver.tscn"
			: "res://Scenes/Shelter/Shelter.tscn";

		GameLostPending = false;
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);

		// Let the new scene finish loading while the screen is still black, then fade back in.
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		// The runner is back at the shelter, so give control back while the screen fades in.
		Fading = false;

		Tween fadeIn = fadeRect.CreateTween();
		fadeIn.TweenProperty(fadeRect, "modulate:a", 0.0f, DeathFadeInSeconds);
	}

	public void FadeToScene(string path)
	{
		if (Fading)
			return;

		Fading = true;
		FadeToSceneAsync(path);
	}

	private async void FadeToSceneAsync(string path)
	{
		fadeRect.Color = Colors.Black;

		Tween fadeOut = fadeRect.CreateTween();
		fadeOut.TweenProperty(fadeRect, "modulate:a", 1.0f, DeathFadeOutSeconds);
		await ToSignal(fadeOut, Tween.SignalName.Finished);

		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		Fading = false;

		Tween fadeIn = fadeRect.CreateTween();
		fadeIn.TweenProperty(fadeRect, "modulate:a", 0.0f, DeathFadeInSeconds);
	}

	public void FlashToScene(string path)
	{
		if (Fading)
			return;

		Fading = true;
		FlashToSceneAsync(path);
	}

	private async void FlashToSceneAsync(string path)
	{
		GetTree().Paused = false;

		fadeRect.Color = Colors.White;

		Tween rise = fadeRect.CreateTween();
		rise.TweenProperty(fadeRect, "modulate:a", 1.0f, FlashRiseSeconds);
		await ToSignal(rise, Tween.SignalName.Finished);

		await ToSignal(GetTree().CreateTimer(FlashHoldSeconds), SceneTreeTimer.SignalName.Timeout);

		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		Fading = false;

		Tween fall = fadeRect.CreateTween();
		fall.TweenProperty(fadeRect, "modulate:a", 0.0f, FlashFadeSeconds);
		await ToSignal(fall, Tween.SignalName.Finished);

		fadeRect.Color = Colors.Black;
	}

	public void ResetAllProgress()
	{
		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
		{
			permanent[type] = 0;
			run[type] = 0;
		}

		foreach (ConsumableDef def in GameDatabase.Consumables)
		{
			permanentItems[def.Id] = 0;
			runItems[def.Id] = 0;
		}

		ownedEquipment.Clear();
		equipped.Clear();
		GearUpQuestDone = false;
		IntroSeen = false;   // a new game shows the intro guide again

		for (int i = 1; i <= GameDatabase.CoreCount; i++)
		{
			coreOwned[i] = false;
			coreInstalled[i] = false;
		}

		runCores.Clear();

		StorageLevel = 1;
		ArmoryLevel = 1;
		InfirmaryLevel = 1;
		WorkshopLevel = 1;

		PermanentInfection = 0f;
		DeathCount = 0;
		GameWon = false;

		DeleteSave();

		ApplyEquipment();
		ApplyStationBonuses();
		ResetRunState();

		SpawnAtInfirmary = false;
		GrantStarterKit();
	}

	private void DeleteSave()
	{
		string path = ProjectSettings.GlobalizePath(SavePath);

		if (System.IO.File.Exists(path))
			System.IO.File.Delete(path);
	}

	private void BuildExplorationHud()
	{
		hudLayer = new CanvasLayer();
		hudLayer.Layer = 8;
		hudLayer.Visible = false;
		AddChild(hudLayer);

		Shader vignette = new Shader();
		vignette.Code =
			"shader_type canvas_item;\n" +
			"uniform float strength = 0.5;\n" +
			"void fragment() {\n" +
			"\tvec2 p = UV - vec2(0.5);\n" +
			"\tfloat d = length(p) * 1.5;\n" +
			"\tfloat v = smoothstep(0.3, 1.0, d);\n" +
			"\tCOLOR = vec4(0.05, 0.0, 0.1, clamp(v * strength + 0.12, 0.0, 1.0));\n" +
			"}\n";

		ShaderMaterial vignetteMaterial = new ShaderMaterial();
		vignetteMaterial.Shader = vignette;

		retreatTint = new ColorRect();
		retreatTint.Color = new Color(1f, 1f, 1f, 1f);
		retreatTint.Material = vignetteMaterial;
		retreatTint.MouseFilter = Control.MouseFilterEnum.Ignore;
		retreatTint.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		retreatTint.Visible = false;
		hudLayer.AddChild(retreatTint);
		hudLayer.MoveChild(retreatTint, 0);

		Tween beat = retreatTint.CreateTween();
		beat.SetLoops();
		beat.TweenMethod(Callable.From<float>(SetVignetteStrength), 0.45f, 0.95f, 0.12f);
		beat.TweenMethod(Callable.From<float>(SetVignetteStrength), 0.95f, 0.55f, 0.12f);
		beat.TweenMethod(Callable.From<float>(SetVignetteStrength), 0.55f, 1.0f, 0.12f);
		beat.TweenMethod(Callable.From<float>(SetVignetteStrength), 1.0f, 0.45f, 0.7f);

		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;

		ashParticles = new CpuParticles2D();
		ashParticles.Position = new Vector2(viewportSize.X / 2f, viewportSize.Y + 10f);
		ashParticles.Amount = 70;
		ashParticles.Lifetime = 8.0;
		ashParticles.Preprocess = 8.0;
		ashParticles.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
		ashParticles.EmissionRectExtents = new Vector2(viewportSize.X / 2f, 10f);
		ashParticles.Direction = new Vector2(0f, -1f);
		ashParticles.Spread = 25f;
		ashParticles.InitialVelocityMin = 15f;
		ashParticles.InitialVelocityMax = 45f;
		ashParticles.Gravity = Vector2.Zero;
		ashParticles.ScaleAmountMin = 1.5f;
		ashParticles.ScaleAmountMax = 3.5f;
		ashParticles.Color = new Color(0.85f, 0.8f, 0.95f, 0.45f);
		ashParticles.Emitting = false;
		hudLayer.AddChild(ashParticles);

		subtitleLabel = new Label();
		subtitleLabel.Text = "The shelter lights flicker ahead and the fog parts.";
		subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		ApplyPixelFont(subtitleLabel, 24);
		hudLayer.AddChild(subtitleLabel);
		subtitleLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
		subtitleLabel.GrowHorizontal = Control.GrowDirection.Both;
		subtitleLabel.OffsetTop -= 90;
		subtitleLabel.OffsetBottom -= 90;
		subtitleLabel.Modulate = new Color(1f, 1f, 1f, 0f);
	}

	private void SetVignetteStrength(float value)
	{
		ShaderMaterial material = retreatTint.Material as ShaderMaterial;

		if (material != null)
			material.SetShaderParameter("strength", value);
	}

	private void ShowSubtitle(string text)
	{
		if (subtitleTween != null && subtitleTween.IsValid())
			subtitleTween.Kill();

		subtitleLabel.Text = text;
		subtitleLabel.Modulate = new Color(1f, 1f, 1f, 0f);

		subtitleTween = subtitleLabel.CreateTween();
		subtitleTween.TweenProperty(subtitleLabel, "modulate:a", 1.0f, 1.0f);
		subtitleTween.TweenInterval(4.0);
		subtitleTween.TweenProperty(subtitleLabel, "modulate:a", 0.0f, 1.5f);
	}

	private Node2D FindPlayer()
	{
		if (playerNode != null && IsInstanceValid(playerNode) && playerNode.IsInsideTree())
			return playerNode;

		Node scene = GetTree().CurrentScene;

		if (scene == null)
			return null;

		playerNode = scene.FindChild("Player", true, false) as Node2D;
		return playerNode;
	}

	private void UpdateRetreatLines()
	{
		if (nextRetreatLine < RetreatLines.Length && retreatTime >= RetreatLines[nextRetreatLine].Time)
		{
			ShowSubtitle(RetreatLines[nextRetreatLine].Text);
			nextRetreatLine++;
		}

		if (nearExitShown || !hasStartPosition)
			return;

		Node2D player = FindPlayer();

		if (player != null && player.GlobalPosition.DistanceTo(ExpeditionStartPosition) < 500f)
		{
			nearExitShown = true;
			ShowSubtitle("The shelter lights flicker ahead.");
		}
	}

	private bool IsExploring()
	{
		Node scene = GetTree().CurrentScene;
		return scene != null && scene.SceneFilePath == "res://Scenes/main.tscn";
	}

	public override void _Process(double delta)
	{
		bool exploring = IsExploring() && CurrentExpeditionStatus == ExpeditionStatus.Active;

		hudLayer.Visible = exploring;
		retreatTint.Visible = exploring && CarryingFinalCore;
		ashParticles.Emitting = exploring && CarryingFinalCore;

		// MeterLabel is authored in main.tscn at Main/UI/MeterLabel.
		Node scene = GetTree().CurrentScene;
		meterLabel = scene != null
			? scene.FindChild("MeterLabel", true, false) as Label
			: null;
		if (meterLabel != null)
			meterLabel.Visible = exploring;

		if (!CarryingFinalCore)
		{
			retreatTime = 0f;
			nextRetreatLine = 0;
			nearExitShown = false;
		}

		if (!exploring)
			return;

		ZoneRule rule = CurrentZoneRule;

		if (CarryingFinalCore)
		{
			if (meterLabel != null)
				meterLabel.Text = "The final core weighs on you. Return to the shelter.";

			retreatTime += (float)delta;
			UpdateRetreatLines();
			EnsureShelterExit();
			return;
		}

		PlayerStats.AddInfection(rule.ExploreInfectionPerSecond * (float)delta);

		if (meterLabel != null)
			meterLabel.Text = $"{rule.Name}   Infection: {PlayerStats.CurrentInfection:0.0}%   Base: {PermanentInfection:0}%";

		if (PlayerStats.IsInfectionMaxed())
			FinishExpedition();
	}

	public void PrintData()
	{
		GD.Print(
			$"RUN -> Scrap: {RunScrap}/{GameDatabase.RunCap(StorageLevel, ResourceType.Scrap)}, " +
			$"Material: {RunMaterial}/{GameDatabase.RunCap(StorageLevel, ResourceType.Material)}, " +
			$"Energy: {RunEnergy}/{GameDatabase.RunCap(StorageLevel, ResourceType.Energy)}, " +
			$"Essence: {RunEssence}/{GameDatabase.RunCap(StorageLevel, ResourceType.Essence)}, " +
			$"Core: {RunCoreFragments}/{MaxRunCoreFragments}"
		);
		GD.Print(
			$"PERMANENT -> Scrap: {PermanentScrap}, Material: {PermanentMaterial}, " +
			$"Energy: {PermanentEnergy}, Essence: {PermanentEssence}, Core: {PermanentCoreFragments}"
		);
		GD.Print($"Base infection: {PermanentInfection}%, deaths: {DeathCount}");
		GD.Print($"Storage Lv {StorageLevel}, Armory Lv {ArmoryLevel}, Infirmary Lv {InfirmaryLevel}, Gear slots: {equipped.Count}/{GameDatabase.AllSlots.Length}");
	}

	public void SaveGame()
	{
		SaveData data = new SaveData
		{
			Scrap = permanent[ResourceType.Scrap],
			Material = permanent[ResourceType.Material],
			Energy = permanent[ResourceType.Energy],
			Essence = permanent[ResourceType.Essence],
			PermanentInfection = PermanentInfection,
			DeathCount = DeathCount,
			CoresOwned = GetCoreList(coreOwned),
			CoresInstalled = GetCoreList(coreInstalled),
			GameWon = GameWon,
			StorageLevel = StorageLevel,
			ArmoryLevel = ArmoryLevel,
			InfirmaryLevel = InfirmaryLevel,
			WorkshopLevel = WorkshopLevel,
			OwnedEquipment = new List<string>(ownedEquipment),
			Equipped = GetEquippedMap(),
			GearUpQuestDone = GearUpQuestDone,
			IntroSeen = IntroSeen,
			Items = new Dictionary<string, int>(permanentItems)
		};

		string json = JsonSerializer.Serialize(data);

		using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);

		if (file == null)
		{
			GD.PushWarning("Could not open save file for writing.");
			return;
		}

		file.StoreString(json);
	}

	private void LoadGame()
	{
		if (!FileAccess.FileExists(SavePath))
			return;

		using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);

		if (file == null)
			return;

		SaveData data;

		try
		{
			data = JsonSerializer.Deserialize<SaveData>(file.GetAsText());
		}
		catch (Exception e)
		{
			GD.PushWarning($"Save file could not be read: {e.Message}");
			return;
		}

		if (data == null)
			return;

		StorageLevel = Mathf.Clamp(data.StorageLevel, 1, GameDatabase.MaxStationLevel);
		ArmoryLevel = Mathf.Clamp(data.ArmoryLevel, 1, GameDatabase.MaxStationLevel);
		InfirmaryLevel = Mathf.Clamp(data.InfirmaryLevel, 1, GameDatabase.MaxStationLevel);
		WorkshopLevel = Mathf.Clamp(data.WorkshopLevel, 1, GameDatabase.MaxStationLevel);

		permanent[ResourceType.Scrap] = Mathf.Max(0, data.Scrap);
		permanent[ResourceType.Material] = Mathf.Max(0, data.Material);
		permanent[ResourceType.Energy] = Mathf.Max(0, data.Energy);
		permanent[ResourceType.Essence] = Mathf.Max(0, data.Essence);
		GameWon = data.GameWon;
		PermanentInfection = Mathf.Clamp(data.PermanentInfection, 0f, 99f);
		DeathCount = Mathf.Max(0, data.DeathCount);

		if (data.CoresOwned != null)
		{
			foreach (int index in data.CoresOwned)
			{
				if (index >= 1 && index <= GameDatabase.CoreCount)
					coreOwned[index] = true;
			}
		}

		if (data.CoresInstalled != null)
		{
			foreach (int index in data.CoresInstalled)
			{
				if (index >= 1 && index <= GameDatabase.CoreCount)
				{
					coreInstalled[index] = true;
					coreOwned[index] = true;
				}
			}
		}

		if (data.OwnedEquipment != null)
		{
			foreach (string id in data.OwnedEquipment)
			{
				if (GameDatabase.GetEquipment(id) != null)
					ownedEquipment.Add(id);
			}
		}

		if (data.Equipped != null)
		{
			foreach (KeyValuePair<string, string> pair in data.Equipped)
			{
				if (!Enum.TryParse(pair.Key, out EquipSlot slot) || !ownedEquipment.Contains(pair.Value))
					continue;

				EquipmentDef def = GameDatabase.GetEquipment(pair.Value);

				if (def != null && def.Slot == slot)
					equipped[slot] = pair.Value;
			}
		}

		GearUpQuestDone = data.GearUpQuestDone;
		IntroSeen = data.IntroSeen;

		if (data.Items != null)
		{
			foreach (KeyValuePair<string, int> pair in data.Items)
			{
				if (GameDatabase.GetConsumable(pair.Key) != null)
					permanentItems[pair.Key] = Mathf.Max(0, pair.Value);
			}
		}
	}

	public void DefeatNpc(string npcID)
	{
		defeatedNPCs.Add(npcID);
	}

	public bool IsNpcDefeated(string npcID)
	{
		return defeatedNPCs.Contains(npcID);
	}

	public void ClearDefeatedNPCs()
	{
		defeatedNPCs.Clear();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.F1)
			{
				GD.Print($"[DEBUG] +{AddRun(ResourceType.Scrap, 10)} Run Scrap");
				PrintData();
			}
			else if (key.Keycode == Key.Key4)
			{
				GD.Print($"[DEBUG] +{AddRun(ResourceType.Energy, 10)} Run Energy");
				PrintData();
			}
			else if (key.Keycode == Key.Key5)
			{
				int nextCore = 0;
				for (int i = 1; i <= GameDatabase.CoreCount; i++)
				{
					if (!coreOwned[i] && !runCores.Contains(i))
					{
						nextCore = i;
						break;
					}
				}

				if (nextCore > 0)
					TryAddRunCore(nextCore);

				GD.Print($"[DEBUG] Run Core: {nextCore}");
				PrintData();
			}
			else if (key.Keycode == Key.Key6)
			{
				GD.Print($"[DEBUG] +{AddRun(ResourceType.Material, 10)} Run Material");
				PrintData();
			}
			else if (key.Keycode == Key.Key7)
			{
				GD.Print($"[DEBUG] +{AddRun(ResourceType.Essence, 2)} Run Essence");
				PrintData();
			}
			else if (key.Keycode == Key.Key8)
			{
				AddPermanent(ResourceType.Scrap, 100);
				AddPermanent(ResourceType.Material, 50);
				AddPermanent(ResourceType.Energy, 30);
				AddPermanent(ResourceType.Essence, 20);
				GD.Print("[DEBUG] Granted storage resources");
				PrintData();
			}
			else if (key.Keycode == Key.Key9)
			{
				foreach (ConsumableDef def in GameDatabase.Consumables)
					permanentItems[def.Id] = GetPermanentItemCount(def.Id) + 3;
				GD.Print("[DEBUG] Granted 3 of each consumable (taken on next expedition)");
			}
			else if (key.Keycode == Key.F3)
			{
				PrintData();
			}
			else if (key.Keycode == Key.F4)
			{
				if (CurrentExpeditionStatus != ExpeditionStatus.Active || CarryingFinalCore)
					return;

				GD.Print("[DEBUG] Simulated Death");
				FinishExpedition();
			}
		}
	}
}
