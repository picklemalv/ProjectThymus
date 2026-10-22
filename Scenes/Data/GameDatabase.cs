using System;
using System.Collections.Generic;

public enum ResourceType
{
	Scrap,
	Material,
	Energy,
	Essence
}

public enum EquipSlot
{
	Weapon,
	Armor
}

public enum StationType
{
	Storage,
	Armory,
	Infirmary,
	Workshop,
	CoreChamber
}

public class BossSpecial
{
	public string Name;
	public int EveryNAttacks;
	public float DamageMultiplier;
	public float InfectionGain;
}

public class ZoneRule
{
	public int Number;
	public string Name;
	public float InfectionPerTurn;
	public float ExploreInfectionPerSecond;
	public float RewardMultiplier;
}

public class ResourceCost
{
	public int Scrap;
	public int Material;
	public int Energy;
	public int Essence;

	public ResourceCost(int scrap = 0, int material = 0, int energy = 0, int essence = 0)
	{
		Scrap = scrap;
		Material = material;
		Energy = energy;
		Essence = essence;
	}

	public int Get(ResourceType type)
	{
		switch (type)
		{
			case ResourceType.Scrap:
				return Scrap;
			case ResourceType.Material:
				return Material;
			case ResourceType.Energy:
				return Energy;
			default:
				return Essence;
		}
	}

	public string ToText()
	{
		List<string> parts = new List<string>();

		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
		{
			int amount = Get(type);
			if (amount > 0)
				parts.Add($"{amount} {type}");
		}

		return parts.Count > 0 ? string.Join(", ", parts) : "Free";
	}
}

public class EquipmentDef
{
	public string Id;
	public string Name;
	public EquipSlot Slot;
	public int WeaponDamage;
	public int Defense;
	public int HP;
	public int Speed;
	public int RequiredArmoryLevel = 1;
	public ResourceCost Cost = new ResourceCost();

	public string StatText()
	{
		List<string> parts = new List<string>();

		if (WeaponDamage != 0)
			parts.Add($"Weapon DMG +{WeaponDamage}");
		if (Defense != 0)
			parts.Add($"DEF +{Defense}");
		if (HP != 0)
			parts.Add($"HP +{HP}");
		if (Speed != 0)
		{
			string sign = Speed > 0 ? "+" : "";
			parts.Add($"SPD {sign}{Speed}");
		}

		return string.Join(", ", parts);
	}
}

public class ConsumableDef
{
	public string Id;
	public string Name;
	public string Description;
	public ResourceCost Cost = new ResourceCost();
	public int Heal;
	public int InfectionReduction;
	public int AttackBuff;
	public int BuffTurns;
}

public static class GameDatabase
{
	public const int MaxStationLevel = 3;
	public const int CoreCount = 3;

	public static readonly List<ZoneRule> Zones = new List<ZoneRule>
	{
		new ZoneRule { Number = 1, Name = "Zone 1", InfectionPerTurn = 1f, ExploreInfectionPerSecond = 0.08f, RewardMultiplier = 1.0f },
		new ZoneRule { Number = 2, Name = "Zone 2", InfectionPerTurn = 2f, ExploreInfectionPerSecond = 0.15f, RewardMultiplier = 1.5f },
		new ZoneRule { Number = 3, Name = "Zone 3", InfectionPerTurn = 4f, ExploreInfectionPerSecond = 0.30f, RewardMultiplier = 2.0f }
	};

	public const int RunCapScrap = 100;
	public const int RunCapMaterial = 50;
	public const int RunCapEnergy = 30;
	public const int RunCapEssence = 10;

	public static readonly List<EquipmentDef> Equipment = new List<EquipmentDef>
	{
		new EquipmentDef
		{
			Id = "rusty_blade", Name = "Rusty Blade", Slot = EquipSlot.Weapon,
			WeaponDamage = 3, RequiredArmoryLevel = 1, Cost = new ResourceCost()
		},
		new EquipmentDef
		{
			Id = "scrap_axe", Name = "Scrap Axe", Slot = EquipSlot.Weapon,
			WeaponDamage = 6, RequiredArmoryLevel = 1, Cost = new ResourceCost(40, 5)
		},
		new EquipmentDef
		{
			Id = "reinforced_blade", Name = "Reinforced Blade", Slot = EquipSlot.Weapon,
			WeaponDamage = 10, RequiredArmoryLevel = 2, Cost = new ResourceCost(90, 15)
		},
		new EquipmentDef
		{
			Id = "contaminated_edge", Name = "Contaminated Edge", Slot = EquipSlot.Weapon,
			WeaponDamage = 15, RequiredArmoryLevel = 3, Cost = new ResourceCost(150, 20, 0, 5)
		},
		new EquipmentDef
		{
			Id = "cloth_wraps", Name = "Cloth Wraps", Slot = EquipSlot.Armor,
			Defense = 1, RequiredArmoryLevel = 1, Cost = new ResourceCost()
		},
		new EquipmentDef
		{
			Id = "scrap_vest", Name = "Scrap Vest", Slot = EquipSlot.Armor,
			Defense = 3, HP = 10, RequiredArmoryLevel = 1, Cost = new ResourceCost(35, 5)
		},
		new EquipmentDef
		{
			Id = "plated_vest", Name = "Plated Vest", Slot = EquipSlot.Armor,
			Defense = 6, HP = 20, RequiredArmoryLevel = 2, Cost = new ResourceCost(80, 12)
		},
		new EquipmentDef
		{
			Id = "filter_suit", Name = "Filter Suit", Slot = EquipSlot.Armor,
			Defense = 9, HP = 30, RequiredArmoryLevel = 3, Cost = new ResourceCost(140, 20, 0, 5)
		}
	};

	public static readonly List<ConsumableDef> Consumables = new List<ConsumableDef>
	{
		new ConsumableDef
		{
			Id = "health_potion", Name = "Health Potion",
			Description = "Restores 40 HP.",
			Cost = new ResourceCost(10, 2), Heal = 40
		},
		new ConsumableDef
		{
			Id = "antidote", Name = "Antidote",
			Description = "Reduces Infection by 10%.",
			Cost = new ResourceCost(15, 3), InfectionReduction = 10
		},
		new ConsumableDef
		{
			Id = "temporary_buff", Name = "Temporary Buff",
			Description = "+6 Attack for the next 3 attacks.",
			Cost = new ResourceCost(0, 2, 5), AttackBuff = 6, BuffTurns = 3
		},
		new ConsumableDef
		{
			Id = "infection_reducer", Name = "Infection Reducer",
			Description = "Reduces Infection by 25%.",
			Cost = new ResourceCost(0, 5, 5, 2), InfectionReduction = 25
		}
	};

	public static readonly List<BossSpecial> BossSpecials = new List<BossSpecial>
	{
		new BossSpecial { Name = "Crushing Blow", EveryNAttacks = 4, DamageMultiplier = 1.8f, InfectionGain = 0f },
		new BossSpecial { Name = "Contaminated Burst", EveryNAttacks = 3, DamageMultiplier = 1.6f, InfectionGain = 5f },
		new BossSpecial { Name = "Core Collapse", EveryNAttacks = 3, DamageMultiplier = 2.0f, InfectionGain = 8f }
	};

	private static readonly int[][] storageCaps = new int[][]
	{
		new int[] { 300, 600, 1200 },
		new int[] { 150, 300, 600 },
		new int[] { 100, 200, 400 },
		new int[] { 50, 100, 200 }
	};

	public static int RunCapBase(ResourceType type)
	{
		switch (type)
		{
			case ResourceType.Scrap:
				return RunCapScrap;
			case ResourceType.Material:
				return RunCapMaterial;
			case ResourceType.Energy:
				return RunCapEnergy;
			default:
				return RunCapEssence;
		}
	}

	public static int RunCap(int storageLevel, ResourceType type)
	{
		float multiplier = 1f + 0.5f * (Math.Clamp(storageLevel, 1, MaxStationLevel) - 1);
		return (int)Math.Round(RunCapBase(type) * multiplier);
	}

	public static int StorageCap(int level, ResourceType type)
	{
		int index = Math.Clamp(level, 1, MaxStationLevel) - 1;
		return storageCaps[(int)type][index];
	}

	public static ResourceCost UpgradeCost(StationType station, int currentLevel)
	{
		if (station == StationType.CoreChamber || currentLevel >= MaxStationLevel)
			return null;

		if (station == StationType.Infirmary)
			return currentLevel == 1 ? new ResourceCost(100, 30, 0, 3) : new ResourceCost(250, 70, 0, 8);

		if (station == StationType.Workshop)
			return currentLevel == 1 ? new ResourceCost(120, 30, 10) : new ResourceCost(280, 70, 20, 6);

		if (station == StationType.Storage)
			return currentLevel == 1 ? new ResourceCost(100, 25) : new ResourceCost(300, 80, 0, 8);

		return currentLevel == 1 ? new ResourceCost(120, 30) : new ResourceCost(300, 80, 0, 8);
	}

	public static string UpgradeBenefitText(StationType station, int currentLevel)
	{
		if (station == StationType.CoreChamber || currentLevel >= MaxStationLevel)
			return "";

		if (station == StationType.Workshop)
			return $"Crafting and upgrade costs {Math.Round(WorkshopDiscount(currentLevel + 1) * 100)}% lower.";

		if (station == StationType.Infirmary)
			return
				$"Infection gain {Math.Round(InfectionResistance(currentLevel + 1) * 100)}% lower. " +
				$"Death penalty {DeathInfectionPenalty(currentLevel)}% -> {DeathInfectionPenalty(currentLevel + 1)}%.";

		if (station == StationType.Storage)
		{
			int next = currentLevel + 1;
			return
				$"Capacity -> Scrap {StorageCap(next, ResourceType.Scrap)}, " +
				$"Material {StorageCap(next, ResourceType.Material)}, " +
				$"Energy {StorageCap(next, ResourceType.Energy)}, " +
				$"Essence {StorageCap(next, ResourceType.Essence)}. " +
				$"Run bag -> Scrap {RunCap(next, ResourceType.Scrap)}, " +
				$"Material {RunCap(next, ResourceType.Material)}, " +
				$"Energy {RunCap(next, ResourceType.Energy)}, " +
				$"Essence {RunCap(next, ResourceType.Essence)}";
		}

		return $"Unlocks tier {currentLevel + 1} weapons and armor.";
	}

	public static float WorkshopDiscount(int workshopLevel)
	{
		switch (Math.Clamp(workshopLevel, 1, MaxStationLevel))
		{
			case 2:
				return 0.15f;
			case 3:
				return 0.30f;
			default:
				return 0f;
		}
	}

	public static string StationDisplayName(StationType station)
	{
		switch (station)
		{
			case StationType.Storage:
				return "Resource Storage";
			case StationType.Armory:
				return "Armory";
			case StationType.Infirmary:
				return "Infirmary";
			case StationType.Workshop:
				return "Workshop";
			default:
				return "Core Chamber";
		}
	}

	public static BossSpecial GetBossSpecial(int bossIndex)
	{
		if (bossIndex < 1 || bossIndex > BossSpecials.Count)
			return null;

		return BossSpecials[bossIndex - 1];
	}

	public static ZoneRule GetZone(int number)
	{
		return Zones[Math.Clamp(number, 1, Zones.Count) - 1];
	}

	public static int BossCoreIndex(string npcId)
	{
		if (string.IsNullOrEmpty(npcId) || !npcId.StartsWith("Boss"))
			return 0;

		if (int.TryParse(npcId.Substring(4), out int index) && index >= 1 && index <= CoreCount)
			return index;

		return 0;
	}

	public static float DeathInfectionPenalty(int infirmaryLevel)
	{
		switch (Math.Clamp(infirmaryLevel, 1, MaxStationLevel))
		{
			case 2:
				return 4f;
			case 3:
				return 3f;
			default:
				return 5f;
		}
	}

	public static float InfectionResistance(int infirmaryLevel)
	{
		switch (Math.Clamp(infirmaryLevel, 1, MaxStationLevel))
		{
			case 2:
				return 0.15f;
			case 3:
				return 0.30f;
			default:
				return 0f;
		}
	}

	public static EquipmentDef GetEquipment(string id)
	{
		return Equipment.Find(e => e.Id == id);
	}

	public static ConsumableDef GetConsumable(string id)
	{
		return Consumables.Find(c => c.Id == id);
	}
}
