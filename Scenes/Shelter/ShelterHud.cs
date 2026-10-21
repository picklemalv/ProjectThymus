using Godot;

public partial class ShelterHud : Label
{
	public override void _Ready()
	{
		GameState state = GameState.Instance;

		EquipmentDef weapon = GameDatabase.GetEquipment(state.EquippedWeaponId);
		EquipmentDef armor = GameDatabase.GetEquipment(state.EquippedArmorId);
		string weaponName = weapon != null ? weapon.Name : "None";
		string armorName = armor != null ? armor.Name : "None";

		Text =
			$"SHELTER\n" +
			$"{state.LastExpeditionMessage}\n\n" +
			$"Scrap: {state.PermanentScrap}/{GameDatabase.StorageCap(state.StorageLevel, ResourceType.Scrap)}\n" +
			$"Material: {state.PermanentMaterial}/{GameDatabase.StorageCap(state.StorageLevel, ResourceType.Material)}\n" +
			$"Energy: {state.PermanentEnergy}/{GameDatabase.StorageCap(state.StorageLevel, ResourceType.Energy)}\n" +
			$"Essence: {state.PermanentEssence}/{GameDatabase.StorageCap(state.StorageLevel, ResourceType.Essence)}\n" +
			$"Core Fragments: {state.PermanentCoreFragments}\n\n" +
			$"Weapon: {weaponName}\n" +
			$"Armor: {armorName}\n" +
			$"Storage Lv.{state.StorageLevel}  Armory Lv.{state.ArmoryLevel}";
	}
}
