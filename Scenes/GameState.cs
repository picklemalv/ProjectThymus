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
	public int CoreFragments { get; set; }
	public int StorageLevel { get; set; } = 1;
	public int ArmoryLevel { get; set; } = 1;
	public int InfirmaryLevel { get; set; } = 1;
	public List<string> OwnedEquipment { get; set; } = new List<string>();
	public string EquippedWeaponId { get; set; } = "rusty_blade";
	public string EquippedArmorId { get; set; } = "cloth_wraps";
	public Dictionary<string, int> Items { get; set; } = new Dictionary<string, int>();
}

public partial class GameState : Node
{
	public static GameState Instance { get; private set; }

	public const int MaxRunScrap = GameDatabase.RunCapScrap;
	public const int MaxRunMaterial = GameDatabase.RunCapMaterial;
	public const int MaxRunEnergy = GameDatabase.RunCapEnergy;
	public const int MaxRunEssence = GameDatabase.RunCapEssence;
	public const int MaxRunCoreFragments = 1;
	public const int ItemCarryLimit = 3;

	private const string SavePath = "user://thymus_save.json";

	public bool SpawnAtInfirmary = false;

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

	public int PermanentCoreFragments = 0;
	public int RunCoreFragments = 0;

	public int StorageLevel = 1;
	public int ArmoryLevel = 1;
	public int InfirmaryLevel = 1;

	public ulong EscapeGraceUntilMs = 0;

	public string EquippedWeaponId = "rusty_blade";
	public string EquippedArmorId = "cloth_wraps";

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

		ownedEquipment.Add("rusty_blade");
		ownedEquipment.Add("cloth_wraps");

		PlayerStats = new Stats();
		AddChild(PlayerStats);

		LoadGame();
		ApplyEquipment();
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

		int cap = GameDatabase.RunCap(type);
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

	public int AddRunCoreFragments(int amount)
	{
		int before = RunCoreFragments;
		RunCoreFragments = Mathf.Min(RunCoreFragments + amount, MaxRunCoreFragments);
		return RunCoreFragments - before;
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
		PlayerStats.InfectionGainMultiplier = 1f - GameDatabase.InfectionResistance(InfirmaryLevel);
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
		return id == EquippedWeaponId || id == EquippedArmorId;
	}

	public int GetStationLevel(StationType station)
	{
		if (station == StationType.Storage)
			return StorageLevel;
		if (station == StationType.Armory)
			return ArmoryLevel;
		return InfirmaryLevel;
	}

	public void ApplyEquipment()
	{
		EquipmentDef weapon = GameDatabase.GetEquipment(EquippedWeaponId);
		EquipmentDef armor = GameDatabase.GetEquipment(EquippedArmorId);

		PlayerStats.WeaponDamage = weapon != null ? weapon.WeaponDamage : 0;
		PlayerStats.EquipmentAttackBonus = 0;
		PlayerStats.EquipmentDefenseBonus =
			(weapon != null ? weapon.Defense : 0) + (armor != null ? armor.Defense : 0);
		PlayerStats.EquipmentHPBonus =
			(weapon != null ? weapon.HP : 0) + (armor != null ? armor.HP : 0);
		PlayerStats.EquipmentSpeedBonus =
			(weapon != null ? weapon.Speed : 0) + (armor != null ? armor.Speed : 0);

		PlayerStats.CurrentHP = Mathf.Min(PlayerStats.CurrentHP, PlayerStats.MaxHP);
	}

	public string EquipItem(string id)
	{
		EquipmentDef def = GameDatabase.GetEquipment(id);

		if (def == null || !ownedEquipment.Contains(id))
			return "You don't own that.";

		if (def.Slot == EquipSlot.Weapon)
			EquippedWeaponId = id;
		else
			EquippedArmorId = id;

		ApplyEquipment();
		SaveGame();
		return $"Equipped {def.Name}.";
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
		if (!CanAfford(def.Cost))
			return "Not enough resources.";

		Spend(def.Cost);
		ownedEquipment.Add(id);
		SaveGame();
		return $"Crafted {def.Name}.";
	}

	public string CraftConsumable(string id)
	{
		ConsumableDef def = GameDatabase.GetConsumable(id);

		if (def == null)
			return "Unknown item.";
		if (!CanAfford(def.Cost))
			return "Not enough resources.";

		Spend(def.Cost);
		permanentItems[id] = GetPermanentItemCount(id) + 1;
		SaveGame();
		return $"Crafted {def.Name}.";
	}

	public string UpgradeStation(StationType station)
	{
		int level = GetStationLevel(station);
		ResourceCost cost = GameDatabase.UpgradeCost(station, level);

		if (cost == null)
			return "Nothing to upgrade.";
		if (!CanAfford(cost))
			return "Not enough resources.";

		Spend(cost);

		if (station == StationType.Storage)
			StorageLevel++;
		else if (station == StationType.Armory)
			ArmoryLevel++;
		else
			InfirmaryLevel++;

		ApplyStationBonuses();
		SaveGame();
		return $"{station} upgraded to Level {GetStationLevel(station)}.";
	}

	public void ResetRunState()
	{
		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
			run[type] = 0;

		RunCoreFragments = 0;

		PlayerStats.CurrentHP = PlayerStats.MaxHP;
		PlayerStats.ResetInfection();

		ClearDefeatedNPCs();
		HasReturnPosition = false;

		CurrentExpeditionStatus = ExpeditionStatus.Active;
	}

	public void StartExpedition()
	{
		ReturnRunItems();
		ResetRunState();
		SpawnAtInfirmary = false;

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

	public void EndExpedition(bool success)
	{
		if (success)
		{
			List<string> lost = new List<string>();

			foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
			{
				int amount = run[type];
				int added = AddPermanent(type, amount);
				if (added < amount)
					lost.Add($"{amount - added} {type}");
			}

			PermanentCoreFragments += RunCoreFragments;
			ReturnRunItems();

			CurrentExpeditionStatus = ExpeditionStatus.Completed;
			SpawnAtInfirmary = false;
			LastExpeditionMessage = "Expedition completed. Resources secured.";

			if (lost.Count > 0)
				LastExpeditionMessage += $" Storage full, lost: {string.Join(", ", lost)}.";
		}
		else
		{
			CurrentExpeditionStatus = ExpeditionStatus.Failed;
			SpawnAtInfirmary = true;
			LastExpeditionMessage = "Expedition failed. Run resources lost.";

			foreach (ConsumableDef def in GameDatabase.Consumables)
				runItems[def.Id] = 0;
		}

		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
			run[type] = 0;

		RunCoreFragments = 0;

		GD.Print($"Expedition ended: {CurrentExpeditionStatus}");
		PrintData();
		SaveGame();
	}

	public void PrintData()
	{
		GD.Print(
			$"RUN -> Scrap: {RunScrap}/{MaxRunScrap}, Material: {RunMaterial}/{MaxRunMaterial}, " +
			$"Energy: {RunEnergy}/{MaxRunEnergy}, Essence: {RunEssence}/{MaxRunEssence}, " +
			$"Core: {RunCoreFragments}/{MaxRunCoreFragments}"
		);
		GD.Print(
			$"PERMANENT -> Scrap: {PermanentScrap}, Material: {PermanentMaterial}, " +
			$"Energy: {PermanentEnergy}, Essence: {PermanentEssence}, Core: {PermanentCoreFragments}"
		);
		GD.Print($"Storage Lv {StorageLevel}, Armory Lv {ArmoryLevel}, Infirmary Lv {InfirmaryLevel}, Weapon: {EquippedWeaponId}, Armor: {EquippedArmorId}");
	}

	public void SaveGame()
	{
		SaveData data = new SaveData
		{
			Scrap = permanent[ResourceType.Scrap],
			Material = permanent[ResourceType.Material],
			Energy = permanent[ResourceType.Energy],
			Essence = permanent[ResourceType.Essence],
			CoreFragments = PermanentCoreFragments,
			StorageLevel = StorageLevel,
			ArmoryLevel = ArmoryLevel,
			InfirmaryLevel = InfirmaryLevel,
			OwnedEquipment = new List<string>(ownedEquipment),
			EquippedWeaponId = EquippedWeaponId,
			EquippedArmorId = EquippedArmorId,
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

		permanent[ResourceType.Scrap] = Mathf.Max(0, data.Scrap);
		permanent[ResourceType.Material] = Mathf.Max(0, data.Material);
		permanent[ResourceType.Energy] = Mathf.Max(0, data.Energy);
		permanent[ResourceType.Essence] = Mathf.Max(0, data.Essence);
		PermanentCoreFragments = Mathf.Max(0, data.CoreFragments);

		if (data.OwnedEquipment != null)
		{
			foreach (string id in data.OwnedEquipment)
			{
				if (GameDatabase.GetEquipment(id) != null)
					ownedEquipment.Add(id);
			}
		}

		if (data.EquippedWeaponId != null && ownedEquipment.Contains(data.EquippedWeaponId))
			EquippedWeaponId = data.EquippedWeaponId;

		if (data.EquippedArmorId != null && ownedEquipment.Contains(data.EquippedArmorId))
			EquippedArmorId = data.EquippedArmorId;

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
				GD.Print($"[DEBUG] +{AddRunCoreFragments(1)} Run Core Fragment");
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
			else if (key.Keycode == Key.F2)
			{
				if (CurrentExpeditionStatus != ExpeditionStatus.Active)
					return;

				GD.Print("[DEBUG] Simulated Extract");
				EndExpedition(true);
				GetTree().ChangeSceneToFile("res://Scenes/Shelter/Shelter.tscn");
			}
			else if (key.Keycode == Key.F3)
			{
				PrintData();
			}
			else if (key.Keycode == Key.F4)
			{
				if (CurrentExpeditionStatus != ExpeditionStatus.Active)
					return;

				GD.Print("[DEBUG] Simulated Death");
				EndExpedition(false);
				GetTree().ChangeSceneToFile("res://Scenes/Shelter/Shelter.tscn");
			}
		}
	}
}
