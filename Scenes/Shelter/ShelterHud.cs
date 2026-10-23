using Godot;

public partial class ShelterHud : Label
{
	private float refreshTimer = 0f;

	public override void _Ready()
	{
		RefreshText();
	}

	public override void _Process(double delta)
	{
		refreshTimer += (float)delta;

		if (refreshTimer < 0.25f)
			return;

		refreshTimer = 0f;
		RefreshText();
	}

	private void RefreshText()
	{
		GameState state = GameState.Instance;

		string quest = "";

		if (!state.GearUpQuestDone)
		{
			quest = "QUEST - Gear Up\nCraft and equip one item in every slot at the Armory (press E in the top-right room).\n";

			foreach (EquipSlot slot in GameDatabase.AllSlots)
			{
				string mark = state.GetEquippedId(slot) != null ? "x" : " ";
				quest += $"[{mark}] {GameDatabase.SlotDisplayName(slot)}  ";
			}

			quest += "\n\n";
		}

		string gear = "";

		foreach (EquipSlot slot in GameDatabase.AllSlots)
		{
			int tier = state.GetEquippedTier(slot);
			string tierText = tier > 0 ? "Lv" + tier : "-";
			gear += $"{GameDatabase.SlotDisplayName(slot)}: {tierText}   ";
		}

		Text =
			$"SHELTER   Loop #{state.DeathCount + 1}\n" +
			$"{state.LastExpeditionMessage}\n" +
			$"Base infection: {state.PermanentInfection:0}%  (+{GameDatabase.DeathInfectionPenalty(state.InfirmaryLevel):0}% per death)  Tries left: {state.TriesLeft}\n\n" +
			quest +
			$"Scrap: {state.PermanentScrap}/{GameDatabase.StorageCap(state.StorageLevel, ResourceType.Scrap)}\n" +
			$"Material: {state.PermanentMaterial}/{GameDatabase.StorageCap(state.StorageLevel, ResourceType.Material)}\n" +
			$"Energy: {state.PermanentEnergy}/{GameDatabase.StorageCap(state.StorageLevel, ResourceType.Energy)}\n" +
			$"Essence: {state.PermanentEssence}/{GameDatabase.StorageCap(state.StorageLevel, ResourceType.Essence)}\n" +
			$"Cores: {state.InstalledCoreCount}/{GameDatabase.CoreCount} installed, {state.PermanentCoreFragments} waiting\n\n";
	}
}
