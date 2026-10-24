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
	Knife,
	Rifle,
	Head,
	Body,
	Accessory
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
	public int Tier = 1;
	public int WeaponDamage;
	public int Defense;
	public int HP;
	public int Speed;
	public float InfectionResist;
	public int RequiredArmoryLevel = 1;
	public ResourceCost Cost = new ResourceCost();

	public string StatText()
	{
		List<string> parts = new List<string>();

		if (WeaponDamage != 0)
			parts.Add($"DMG +{WeaponDamage}");
		if (Defense != 0)
			parts.Add($"DEF +{Defense}");
		if (HP != 0)
			parts.Add($"HP +{HP}");
		if (Speed != 0)
		{
			string sign = Speed > 0 ? "+" : "";
			parts.Add($"SPD {sign}{Speed}");
		}
		if (InfectionResist > 0f)
			parts.Add($"Infection gain -{Math.Round(InfectionResist * 100)}%");

		return string.Join(", ", parts);
	}
}

public class ConsumableDef
{
	public string Id;
	public string Name;
	public string Description;
	public ResourceCost Cost = new ResourceCost();
	public int RequiredInfirmaryLevel = 1;
	public int CarryLimit = 3;
	public int Heal;
	public int InfectionReduction;
	public int PermanentInfectionReduction;
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
		new ZoneRule { Number = 3, Name = "Zone 3", InfectionPerTurn = 4f, ExploreInfectionPerSecond = 0.20f, RewardMultiplier = 2.0f }
	};

	public const int RunCapScrap = 150;
	public const int RunCapMaterial = 75;
	public const int RunCapEnergy = 45;
	public const int RunCapEssence = 15;

	public const int HealthPotionCarry = 5;

	public const int StarterScrap = 120;
	public const int StarterMaterial = 30;

	public static readonly EquipSlot[] AllSlots = new EquipSlot[]
	{
		EquipSlot.Knife,
		EquipSlot.Rifle,
		EquipSlot.Head,
		EquipSlot.Body,
		EquipSlot.Accessory
	};

	private static EquipmentDef Gear(
		string id, string name, EquipSlot slot, int tier, ResourceCost cost,
		int weaponDamage = 0, int defense = 0, int hp = 0, int speed = 0, float resist = 0f)
	{
		return new EquipmentDef
		{
			Id = id,
			Name = name,
			Slot = slot,
			Tier = tier,
			RequiredArmoryLevel = tier,
			Cost = cost,
			WeaponDamage = weaponDamage,
			Defense = defense,
			HP = hp,
			Speed = speed,
			InfectionResist = resist
		};
	}

	public static readonly List<EquipmentDef> Equipment = new List<EquipmentDef>
	{
		Gear("knife_1", "Rusty Knife", EquipSlot.Knife, 1, new ResourceCost(20, 3), weaponDamage: 3, speed: 1),
		Gear("knife_2", "Hunter's Knife", EquipSlot.Knife, 2, new ResourceCost(50, 8), weaponDamage: 5, speed: 2),
		Gear("knife_3", "Core-Edge Knife", EquipSlot.Knife, 3, new ResourceCost(85, 16, 0, 3), weaponDamage: 8, speed: 3),

		Gear("rifle_1", "Scrap Rifle", EquipSlot.Rifle, 1, new ResourceCost(30, 5), weaponDamage: 4),
		Gear("rifle_2", "Marksman Rifle", EquipSlot.Rifle, 2, new ResourceCost(70, 12), weaponDamage: 7),
		Gear("rifle_3", "Core Rifle", EquipSlot.Rifle, 3, new ResourceCost(106, 20, 0, 4), weaponDamage: 11),

		Gear("head_1", "Scrap Helmet", EquipSlot.Head, 1, new ResourceCost(15, 2), defense: 1, hp: 5),
		Gear("head_2", "Filter Mask", EquipSlot.Head, 2, new ResourceCost(40, 7), defense: 2, hp: 10),
		Gear("head_3", "Core Visor", EquipSlot.Head, 3, new ResourceCost(63, 13, 0, 2), defense: 4, hp: 15),

		Gear("body_1", "Scrap Vest", EquipSlot.Body, 1, new ResourceCost(25, 4), defense: 2, hp: 10),
		Gear("body_2", "Plated Armor", EquipSlot.Body, 2, new ResourceCost(60, 10), defense: 4, hp: 20),
		Gear("body_3", "Hazmat Suit", EquipSlot.Body, 3, new ResourceCost(93, 18, 0, 3), defense: 7, hp: 30),

		Gear("accessory_1", "Filter Charm", EquipSlot.Accessory, 1, new ResourceCost(15, 3), resist: 0.05f),
		Gear("accessory_2", "Purifier Charm", EquipSlot.Accessory, 2, new ResourceCost(40, 7, 4), resist: 0.10f),
		Gear("accessory_3", "Core Talisman", EquipSlot.Accessory, 3, new ResourceCost(72, 14, 7, 3), resist: 0.15f)
	};

	public static readonly List<ConsumableDef> Consumables = new List<ConsumableDef>
	{
		new ConsumableDef
		{
			Id = "antidote", Name = "Antidote",
			Description = "Reduces Infection by 15%.",
			Cost = new ResourceCost(40, 8), RequiredInfirmaryLevel = 1, InfectionReduction = 15
		},
		new ConsumableDef
		{
			Id = "health_potion", Name = "Health Potion",
			Description = "Restores 40 HP and gives +6 Attack for 3 attacks.",
			Cost = new ResourceCost(35, 6), RequiredInfirmaryLevel = 1, CarryLimit = HealthPotionCarry, Heal = 40, AttackBuff = 6, BuffTurns = 3
		},
		new ConsumableDef
		{
			Id = "infection_reducer", Name = "Infection Reducer",
			Description = "Permanently lowers your starting infection by 8%.",
			Cost = new ResourceCost(0, 9, 7, 3), RequiredInfirmaryLevel = 2, PermanentInfectionReduction = 8
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
		new int[] { 450, 900, 1800 },
		new int[] { 225, 450, 900 },
		new int[] { 150, 300, 600 },
		new int[] { 75, 150, 300 }
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
			return currentLevel == 1 ? new ResourceCost(60, 18, 0, 2) : new ResourceCost(250, 70, 0, 8);

		if (station == StationType.Workshop)
			return currentLevel == 1 ? new ResourceCost(120, 30, 10) : new ResourceCost(280, 70, 20, 6);

		if (station == StationType.Storage)
			return currentLevel == 1 ? new ResourceCost(100, 25) : new ResourceCost(300, 80, 0, 8);

		return currentLevel == 1 ? new ResourceCost(80, 20) : new ResourceCost(120, 30, 0, 3);
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

		return $"Unlocks tier {currentLevel + 1} gear.";
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

	public static string SlotDisplayName(EquipSlot slot)
	{
		switch (slot)
		{
			case EquipSlot.Knife:
				return "Knife";
			case EquipSlot.Rifle:
				return "Rifle";
			case EquipSlot.Head:
				return "Head Gear";
			case EquipSlot.Body:
				return "Body Armor";
			default:
				return "Accessory";
		}
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
