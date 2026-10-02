using Godot;

public partial class NPC : CharacterBody2D
{
	[Export]
	public string NpcID = "";

	[Export]
	public EnemyData EnemyData;

	private bool battleStarted = false;

	public override void _Ready()
	{
		if (GameState.Instance.IsNpcDefeated(NpcID))
		{
			QueueFree();
			return;
		}
	}
}
