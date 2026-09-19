using Godot;

public partial class GameState : Node
{
	public static GameState Instance { get; private set; }

	public bool NpcDefeated = false;

	public bool HasReturnPosition = false;
	public Vector2 ReturnPosition = Vector2.Zero;

	public override void _Ready()
	{
		Instance = this;
	}
}
