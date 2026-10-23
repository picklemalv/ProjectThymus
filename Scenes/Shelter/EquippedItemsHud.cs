using Godot;

public partial class EquippedItemsHud : Label
{
	public override void _Process(double delta)
	{
		GameState state = GameState.Instance;
		string gear = "";

		foreach (EquipSlot slot in GameDatabase.AllSlots)
		{
			int tier = state.GetEquippedTier(slot);
			string tierText = tier > 0 ? "Lv" + tier : "-";

			gear += $"{GameDatabase.SlotDisplayName(slot)}: {tierText}\n";
		}

		Text = "EQUIPPED ITEMS\n" + gear;
	}
}
