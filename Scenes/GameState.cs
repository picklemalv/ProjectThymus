using Godot;
using System.Collections.Generic;

public enum ExpeditionStatus
{
	Active,
	Completed,
	Failed
}

public partial class GameState : Node
{
	public static GameState Instance { get; private set; }

	public const int MaxRunScrap = 100;
	public const int MaxRunEnergy = 30;
	public const int MaxRunCoreFragments = 1;

	public bool HasReturnPosition = false;
	public Vector2 ReturnPosition = Vector2.Zero;

	private HashSet<string> defeatedNPCs = new HashSet<string>();

	public string CurrentNpcID = "";
	public EnemyData CurrentEnemyData;
	public SpriteFrames CurrentEnemySpriteFrames;

	public Stats PlayerStats;

	public int PermanentScrap = 0;
	public int PermanentEnergy = 0;
	public int PermanentCoreFragments = 0;

	public int RunScrap = 0;
	public int RunEnergy = 0;
	public int RunCoreFragments = 0;

	public ExpeditionStatus CurrentExpeditionStatus = ExpeditionStatus.Active;

	public override void _Ready()
	{
		Instance = this;

		PlayerStats = new Stats();
		AddChild(PlayerStats);

		StartExpedition();
	}

	public int AddRunScrap(int amount)
	{
		int before = RunScrap;
		RunScrap = Mathf.Min(RunScrap + amount, MaxRunScrap);
		return RunScrap - before;
	}

	public int AddRunEnergy(int amount)
	{
		int before = RunEnergy;
		RunEnergy = Mathf.Min(RunEnergy + amount, MaxRunEnergy);
		return RunEnergy - before;
	}

	public int AddRunCoreFragments(int amount)
	{
		int before = RunCoreFragments;
		RunCoreFragments = Mathf.Min(RunCoreFragments + amount, MaxRunCoreFragments);
		return RunCoreFragments - before;
	}

	public void StartExpedition()
	{
		RunScrap = 0;
		RunEnergy = 0;
		RunCoreFragments = 0;

		PlayerStats.CurrentHP = PlayerStats.MaxHP;
		PlayerStats.ResetInfection();

		ClearDefeatedNPCs();
		HasReturnPosition = false;

		CurrentExpeditionStatus = ExpeditionStatus.Active;
	}

	public void EndExpedition(bool success)
	{
		if (success)
		{
			PermanentScrap += RunScrap;
			PermanentEnergy += RunEnergy;
			PermanentCoreFragments += RunCoreFragments;
			CurrentExpeditionStatus = ExpeditionStatus.Completed;
		}
		else
		{
			CurrentExpeditionStatus = ExpeditionStatus.Failed;
		}

		RunScrap = 0;
		RunEnergy = 0;
		RunCoreFragments = 0;

		GD.Print($"Expedition ended: {CurrentExpeditionStatus}");
		PrintData();
	}

	public void PrintData()
	{
		GD.Print($"RUN -> Scrap: {RunScrap}/{MaxRunScrap}, Energy: {RunEnergy}/{MaxRunEnergy}, Core: {RunCoreFragments}/{MaxRunCoreFragments}");
		GD.Print($"PERMANENT -> Scrap: {PermanentScrap}, Energy: {PermanentEnergy}, Core: {PermanentCoreFragments}");
	}

	public void DefeatNpc(string npcID)
	{
		defeatedNPCs.Add(npcID);
	}

	public bool IsNpcDefeated(string npcID)
	{
		return defeatedNPCs.Contains(npcID);
	}

	public void ClearDefeatedNPCs()
	{
		defeatedNPCs.Clear();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.F1)
			{
				int added = AddRunScrap(10);
				GD.Print($"[DEBUG] +{added} Run Scrap");
				PrintData();
			}
			else if (key.Keycode == Key.Key4)
			{
				int added = AddRunEnergy(10);
				GD.Print($"[DEBUG] +{added} Run Energy");
				PrintData();
			}
			else if (key.Keycode == Key.Key5)
			{
				int added = AddRunCoreFragments(1);
				GD.Print($"[DEBUG] +{added} Run Core Fragment");
				PrintData();
			}
			else if (key.Keycode == Key.F2)
			{
				if (CurrentExpeditionStatus != ExpeditionStatus.Active)
					return;

				GD.Print("[DEBUG] Simulated Extract");
				EndExpedition(true);
				GetTree().ChangeSceneToFile("res://Scenes/Shelter/Shelter.tscn");
			}
			else if (key.Keycode == Key.F3)
			{
				PrintData();
			}
		}
	}
}
