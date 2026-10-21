using Godot;
using System;

public partial class ShelterStation : Area2D
{
	[Export] public StationType Station = StationType.Storage;

	private CanvasLayer promptLayer;
	private Label promptLabel;
	private CanvasLayer panelLayer;
	private VBoxContainer content;
	private Label messageLabel;

	private bool playerInside = false;
	private bool panelOpen = false;
	private string message = "";

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		BuildPrompt();
		BuildPanel();
	}

	private string StationName()
	{
		switch (Station)
		{
			case StationType.Storage:
				return "Resource Storage";
			case StationType.Armory:
				return "Armory";
			default:
				return "Infirmary";
		}
	}

	private void BuildPrompt()
	{
		promptLayer = new CanvasLayer();
		promptLayer.Layer = 10;
		AddChild(promptLayer);

		promptLabel = new Label();
		promptLabel.Text = $"Press E - {StationName()}";
		promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
		promptLayer.AddChild(promptLabel);

		promptLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
		promptLabel.GrowHorizontal = Control.GrowDirection.Both;
		promptLabel.OffsetTop -= 70;
		promptLabel.OffsetBottom -= 70;
		promptLabel.Visible = false;
	}

	private void BuildPanel()
	{
		panelLayer = new CanvasLayer();
		panelLayer.Layer = 20;
		panelLayer.Visible = false;
		AddChild(panelLayer);

		ColorRect dim = new ColorRect();
		dim.Color = new Color(0, 0, 0, 0.6f);
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		panelLayer.AddChild(dim);

		CenterContainer center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		panelLayer.AddChild(center);

		PanelContainer panel = new PanelContainer();
		center.AddChild(panel);

		MarginContainer margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 20);
		margin.AddThemeConstantOverride("margin_right", 20);
		margin.AddThemeConstantOverride("margin_top", 16);
		margin.AddThemeConstantOverride("margin_bottom", 16);
		panel.AddChild(margin);

		VBoxContainer root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 10);
		margin.AddChild(root);

		ScrollContainer scroll = new ScrollContainer();
		scroll.CustomMinimumSize = new Vector2(720, 400);
		root.AddChild(scroll);

		content = new VBoxContainer();
		content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		content.AddThemeConstantOverride("separation", 8);
		scroll.AddChild(content);

		messageLabel = new Label();
		root.AddChild(messageLabel);

		Button closeButton = new Button();
		closeButton.Text = "Close (E)";
		closeButton.Pressed += ClosePanel;
		root.AddChild(closeButton);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player)
			return;

		playerInside = true;

		if (!panelOpen)
			promptLabel.Visible = true;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not Player)
			return;

		playerInside = false;
		promptLabel.Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (panelOpen && (key.Keycode == Key.E || key.Keycode == Key.Escape))
			{
				ClosePanel();
				GetViewport().SetInputAsHandled();
			}
			else if (!panelOpen && playerInside && key.Keycode == Key.E)
			{
				OpenPanel();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void OpenPanel()
	{
		panelOpen = true;
		message = "";
		Refresh();
		promptLabel.Visible = false;
		panelLayer.Visible = true;
		GetTree().Paused = true;
	}

	private void ClosePanel()
	{
		panelOpen = false;
		panelLayer.Visible = false;
		promptLabel.Visible = playerInside;
		GetTree().Paused = false;
	}

	private void Refresh()
	{
		foreach (Node child in content.GetChildren())
		{
			content.RemoveChild(child);
			child.QueueFree();
		}

		messageLabel.Text = message;

		switch (Station)
		{
			case StationType.Storage:
				BuildStorageContent();
				break;
			case StationType.Armory:
				BuildArmoryContent();
				break;
			default:
				BuildInfirmaryContent();
				break;
		}
	}

	private void AddHeader(string text)
	{
		Label label = new Label();
		label.Text = text;
		label.AddThemeFontSizeOverride("font_size", 22);
		content.AddChild(label);
	}

	private void AddRow(string text, string buttonText, bool enabled, Action onPressed)
	{
		HBoxContainer row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);

		Label label = new Label();
		label.Text = text;
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		label.CustomMinimumSize = new Vector2(420, 0);
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		row.AddChild(label);

		if (buttonText != null)
		{
			Button button = new Button();
			button.Text = buttonText;
			button.Disabled = !enabled;
			button.Pressed += () =>
			{
				if (onPressed != null)
					onPressed();
			};
			row.AddChild(button);
		}

		content.AddChild(row);
	}

	private void AddUpgradeRow()
	{
		GameState state = GameState.Instance;
		int level = state.GetStationLevel(Station);
		ResourceCost cost = GameDatabase.UpgradeCost(Station, level);

		if (cost == null)
		{
			AddRow("This station is at max level.", null, false, null);
			return;
		}

		string benefit = GameDatabase.UpgradeBenefitText(Station, level);

		AddRow(
			$"Upgrade to Level {level + 1}\n{benefit}",
			$"Upgrade: {cost.ToText()}",
			state.CanAfford(cost),
			() =>
			{
				message = state.UpgradeStation(Station);
				Refresh();
			}
		);
	}

	private void BuildStorageContent()
	{
		GameState state = GameState.Instance;

		AddHeader($"RESOURCE STORAGE  -  Level {state.StorageLevel}/{GameDatabase.MaxStationLevel}");

		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
		{
			int cap = GameDatabase.StorageCap(state.StorageLevel, type);
			AddRow($"{type}: {state.GetPermanent(type)} / {cap}", null, false, null);
		}

		AddRow($"Core Fragments: {state.PermanentCoreFragments}", null, false, null);

		AddHeader($"Supplies in stock (carry up to {GameState.ItemCarryLimit} of each per run)");

		foreach (ConsumableDef def in GameDatabase.Consumables)
			AddRow($"{def.Name}: {state.GetPermanentItemCount(def.Id)}", null, false, null);

		AddHeader("Station");
		AddUpgradeRow();
	}

	private void BuildArmoryContent()
	{
		GameState state = GameState.Instance;

		AddHeader($"ARMORY  -  Level {state.ArmoryLevel}/{GameDatabase.MaxStationLevel}");

		AddRow(
			$"Stock: Scrap {state.PermanentScrap}, Material {state.PermanentMaterial}, " +
			$"Energy {state.PermanentEnergy}, Essence {state.PermanentEssence}",
			null, false, null
		);

		EquipmentDef weapon = GameDatabase.GetEquipment(state.EquippedWeaponId);
		EquipmentDef armor = GameDatabase.GetEquipment(state.EquippedArmorId);
		string weaponName = weapon != null ? weapon.Name : "None";
		string armorName = armor != null ? armor.Name : "None";
		AddRow($"Equipped: {weaponName} / {armorName}", null, false, null);

		AddHeader("Weapons");
		foreach (EquipmentDef def in GameDatabase.Equipment)
		{
			if (def.Slot == EquipSlot.Weapon)
				AddEquipmentRow(def);
		}

		AddHeader("Armor");
		foreach (EquipmentDef def in GameDatabase.Equipment)
		{
			if (def.Slot == EquipSlot.Armor)
				AddEquipmentRow(def);
		}

		AddHeader("Station");
		AddUpgradeRow();
	}

	private void BuildInfirmaryContent()
	{
		GameState state = GameState.Instance;

		AddHeader("INFIRMARY");

		AddRow(
			$"Stock: Scrap {state.PermanentScrap}, Material {state.PermanentMaterial}, " +
			$"Energy {state.PermanentEnergy}, Essence {state.PermanentEssence}",
			null, false, null
		);

		AddHeader($"Supplies (carry up to {GameState.ItemCarryLimit} of each per run)");

		foreach (ConsumableDef def in GameDatabase.Consumables)
		{
			string id = def.Id;

			AddRow(
				$"{def.Name} - {def.Description}\nIn stock: {state.GetPermanentItemCount(id)}",
				$"Craft: {def.Cost.ToText()}",
				state.CanAfford(def.Cost),
				() =>
				{
					message = state.CraftConsumable(id);
					Refresh();
				}
			);
		}
	}

	private void AddEquipmentRow(EquipmentDef def)
	{
		GameState state = GameState.Instance;
		string id = def.Id;
		string text = $"{def.Name}  ({def.StatText()})";

		if (state.IsEquipped(id))
		{
			AddRow(text, "Equipped", false, null);
		}
		else if (state.OwnsEquipment(id))
		{
			AddRow(text, "Equip", true, () =>
			{
				message = state.EquipItem(id);
				Refresh();
			});
		}
		else if (def.RequiredArmoryLevel > state.ArmoryLevel)
		{
			AddRow($"{text}\nRequires Armory Level {def.RequiredArmoryLevel}", "Locked", false, null);
		}
		else
		{
			AddRow(text, $"Craft: {def.Cost.ToText()}", state.CanAfford(def.Cost), () =>
			{
				message = state.CraftEquipment(id);
				Refresh();
			});
		}
	}
}
