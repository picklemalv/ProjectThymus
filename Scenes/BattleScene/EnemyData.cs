using Godot;

[GlobalClass]
public partial class EnemyData : Resource
{
	[Export] public string EnemyName = "Enemy";
	[Export] public int BaseHP = 60;
	[Export] public int BaseAttack = 10;
	[Export] public int BaseDefense = 3;
	[Export] public int BaseSpeed = 6;
	[Export] public int ScrapDrop = 15;
	[Export] public int MaterialDrop = 3;
	[Export] public int EnergyDrop = 2;
	[Export] public int EssenceDrop = 1;
	[Export(PropertyHint.Range, "0,1,0.05")] public float EssenceDropChance = 0.2f;
}
