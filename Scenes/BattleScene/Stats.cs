using Godot;

public partial class Stats : Node
{
	[Export] public int BaseHP = 100;
	[Export] public int BaseAttack = 15;
	[Export] public int BaseDefense = 5;
	[Export] public int BaseSpeed = 10;

	public int CurrentHP;

	public const float MaxInfection = 100f;
	public float CurrentInfection = 0f;

	public int WeaponDamage = 0;

	public float InfectionGainMultiplier = 1f;

	public int EquipmentAttackBonus = 0;
	public int EquipmentDefenseBonus = 0;
	public int EquipmentSpeedBonus = 0;
	public int EquipmentHPBonus = 0;

	public int MaxHP => BaseHP + EquipmentHPBonus;
	public int Attack => BaseAttack + EquipmentAttackBonus;
	public int Defense => BaseDefense + EquipmentDefenseBonus;
	public int Speed => BaseSpeed + EquipmentSpeedBonus;

	public override void _Ready()
	{
		CurrentHP = MaxHP;
	}

	public void AddInfection(float amount)
	{
		// After the final boss falls, infection can no longer rise.
		if (amount > 0f && GameState.Instance != null && GameState.Instance.CarryingFinalCore)
			return;

		if (amount > 0f)
			amount *= InfectionGainMultiplier;

		CurrentInfection += amount;
		CurrentInfection = Mathf.Clamp(CurrentInfection, 0f, MaxInfection);
	}

	public bool IsInfectionMaxed()
	{
		return CurrentInfection >= MaxInfection;
	}

	public void ResetInfection()
	{
		CurrentInfection = 0f;
	}

	public void SetInfection(float value)
	{
		CurrentInfection = Mathf.Clamp(value, 0f, MaxInfection);
	}

	public void PrintStats()
	{
		GD.Print($"Runner Stats -> HP: {CurrentHP}/{MaxHP}, ATK: {Attack}, Weapon DMG: {WeaponDamage}, DEF: {Defense}, SPD: {Speed}, Infection: {CurrentInfection}%");
	}
}
