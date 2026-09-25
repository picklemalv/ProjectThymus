using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
	public static GameState Instance { get; private set; }

	public bool HasReturnPosition = false;
	public Vector2 ReturnPosition = Vector2.Zero;

	private HashSet<string> defeatedNPCs = new HashSet<string>();

	public string CurrentNpcID = "";

	public override void _Ready()
	{
		Instance = this;
	}

	public void DefeatNpc(string npcID)
	{
		defeatedNPCs.Add(npcID);
	}

	public bool IsNpcDefeated(string npcID)
	{
		return defeatedNPCs.Contains(npcID);
	}
}
