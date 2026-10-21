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
	Infirmary
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

	private static readonly int[][] storageCaps = new int[][]
	{
		new int[] { 300, 600, 1200 },
		new int[] { 150, 300, 600 },
		new int[] { 100, 200, 400 },
		new int[] { 50, 100, 200 }
	};

	public static int RunCap(ResourceType type)
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

	public static int StorageCap(int level, ResourceType type)
	{
		int index = Math.Clamp(level, 1, MaxStationLevel) - 1;
		return storageCaps[(int)type][index];
	}

	public static ResourceCost UpgradeCost(StationType station, int currentLevel)
	{
		if (station == StationType.Infirmary || currentLevel >= MaxStationLevel)
			return null;

		if (station == StationType.Storage)
			return currentLevel == 1 ? new ResourceCost(100, 25) : new ResourceCost(300, 80, 0, 8);

		return currentLevel == 1 ? new ResourceCost(120, 30) : new ResourceCost(300, 80, 0, 8);
	}

	public static string UpgradeBenefitText(StationType station, int currentLevel)
	{
		if (station == StationType.Infirmary || currentLevel >= MaxStationLevel)
			return "";

		if (station == StationType.Storage)
		{
			int next = currentLevel + 1;
			return
				$"Capacity -> Scrap {StorageCap(next, ResourceType.Scrap)}, " +
				$"Material {StorageCap(next, ResourceType.Material)}, " +
				$"Energy {StorageCap(next, ResourceType.Energy)}, " +
				$"Essence {StorageCap(next, ResourceType.Essence)}";
		}

		return $"Unlocks tier {currentLevel + 1} weapons and armor.";
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
