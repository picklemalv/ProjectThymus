using Godot;

public partial class FacilityLevelsHud : Label
{
	public override void _Process(double delta)
	{
		GameState state = GameState.Instance;

		Text =
			"FACILITY LEVELS\n" +
			$"Storage: Lv.{state.StorageLevel}\n" +
			$"Armory: Lv.{state.ArmoryLevel}\n" +
			$"Infirmary: Lv.{state.InfirmaryLevel}\n" +
			$"Workshop: Lv.{state.WorkshopLevel}";
	}
}
