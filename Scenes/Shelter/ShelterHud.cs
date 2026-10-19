using Godot;

public partial class ShelterHud : Label
{
	public override void _Ready()
	{
		Text =
			$"SHELTER\n" +
			$"{GetStatusText()}\n\n" +
			$"Scrap: {GameState.Instance.PermanentScrap}\n" +
			$"Energy: {GameState.Instance.PermanentEnergy}\n" +
			$"Core Fragments: {GameState.Instance.PermanentCoreFragments}";
	}

	private string GetStatusText()
	{
		switch (GameState.Instance.CurrentExpeditionStatus)
		{
			case ExpeditionStatus.Completed:
				return "Expedition completed. Resources secured.";
			case ExpeditionStatus.Failed:
				return "Expedition failed. Run resources lost.";
			default:
				return "Ready for expedition.";
		}
	}
}
