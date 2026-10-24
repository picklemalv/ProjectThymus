using Godot;
using System.Collections.Generic;

public static class EnemyBalance
{
	private class Entry
	{
		public int HP;
		public int Attack;
		public int Defense;
		public int Speed = 6;
		public bool OverrideDrops = false;
		public int Scrap;
		public int Material;
		public int Energy;
		public int Essence;
		public float EssenceChance = 1f;
	}

	private static readonly Dictionary<string, Entry> table = Build();

	private static Dictionary<string, Entry> Build()
	{
		Dictionary<string, Entry> t = new Dictionary<string, Entry>();

		Add(t, new[] { "1", "2", "6", "7", "9", "10", "11", "12", "14", "15" }, 50, 11, 3);
		Add(t, new[] { "3", "4", "5", "8", "13", "16", "17", "18" }, 40, 14, 3);

		Add(t, new[] { "23", "24", "25" }, 80, 24, 6);
		Add(t, new[] { "19", "20", "21", "22" }, 100, 22, 6);

		Add(t, new[] { "29", "30" }, 120, 34, 8);
		Add(t, new[] { "28" }, 100, 36, 8);
		Add(t, new[] { "26", "27" }, 140, 32, 8);

		AddBoss(t, "Boss1", 220, 20, 4, 40, 8, 5, 2);
		AddBoss(t, "Boss2", 250, 20, 6, 40, 8, 5, 2);
		AddBoss(t, "Boss3", 220, 24, 10, 60, 15, 8, 3);

		return t;
	}

	private static void Add(Dictionary<string, Entry> t, string[] ids, int hp, int atk, int def)
	{
		foreach (string id in ids)
			t[id] = new Entry { HP = hp, Attack = atk, Defense = def };
	}

	private static void AddBoss(Dictionary<string, Entry> t, string id, int hp, int atk, int def,
		int scrap, int material, int energy, int essence)
	{
		t[id] = new Entry
		{
			HP = hp,
			Attack = atk,
			Defense = def,
			OverrideDrops = true,
			Scrap = scrap,
			Material = material,
			Energy = energy,
			Essence = essence,
			EssenceChance = 1f
		};
	}

	public static EnemyData Apply(string npcId, EnemyData source)
	{
		if (string.IsNullOrEmpty(npcId) || !table.TryGetValue(npcId, out Entry entry))
			return source;

		EnemyData data = source != null ? source.Duplicate() as EnemyData : null;

		if (data == null)
			data = new EnemyData();

		data.BaseHP = entry.HP;
		data.BaseAttack = entry.Attack;
		data.BaseDefense = entry.Defense;
		data.BaseSpeed = entry.Speed;

		if (entry.OverrideDrops)
		{
			data.ScrapDrop = entry.Scrap;
			data.MaterialDrop = entry.Material;
			data.EnergyDrop = entry.Energy;
			data.EssenceDrop = entry.Essence;
			data.EssenceDropChance = entry.EssenceChance;
		}

		return data;
	}
}
