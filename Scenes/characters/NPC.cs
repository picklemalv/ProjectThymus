using Godot;

public partial class NPC : CharacterBody2D
{
	public override void _Ready()
	{
		if (GameState.Instance.NpcDefeated)
		{
			QueueFree();
		}
	}
}
