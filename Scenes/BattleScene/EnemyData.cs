using Godot;

[GlobalClass]
public partial class EnemyData : Resource
{
	[Export] public string EnemyName = "Enemy";
	[Export] public int BaseHP = 60;
	[Export] public int BaseAttack = 10;
	[Export] public int BaseDefense = 3;
	[Export] public int BaseSpeed = 6;
}
