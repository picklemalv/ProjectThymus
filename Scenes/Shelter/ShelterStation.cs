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
	private bool showEnding = false;
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
		return GameDatabase.StationDisplayName(Station);
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
			case StationType.Infirmary:
				BuildInfirmaryContent();
				break;
			case StationType.Workshop:
				BuildWorkshopContent();
				break;
			default:
				BuildCoreChamberContent();
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

	private void AddFacilityUpgradeRow(StationType target)
	{
		GameState state = GameState.Instance;
		int level = state.GetStationLevel(target);
		string header = $"{GameDatabase.StationDisplayName(target)} - Level {level}/{GameDatabase.MaxStationLevel}";
		ResourceCost cost = state.GetUpgradeCost(target);

		if (cost == null)
		{
			AddRow($"{header}\nMax level reached.", null, false, null);
			return;
		}

		string benefit = GameDatabase.UpgradeBenefitText(target, level);
		string blocked = state.UpgradeBlockReason(target);

		if (blocked != "")
		{
			AddRow($"{header}\n{benefit}\n{blocked}", "Locked", false, null);
			return;
		}

		AddRow(
			$"{header}\n{benefit}",
			$"Upgrade: {cost.ToText()}",
			state.CanAfford(cost),
			() =>
			{
				message = state.UpgradeStation(target);
				Refresh();
			}
		);
	}

	private void BuildWorkshopContent()
	{
		GameState state = GameState.Instance;
		float discount = GameDatabase.WorkshopDiscount(state.WorkshopLevel);

		AddHeader($"WORKSHOP  -  Level {state.WorkshopLevel}/{GameDatabase.MaxStationLevel}");

		AddRow(
			$"Crafting and upgrade costs are {Math.Round(discount * 100)}% lower.\n" +
			$"Boss cores installed: {state.InstalledUpgradeCores}/{GameDatabase.CoreCount - 1}. " +
			$"Facilities can reach Level {state.GetMaxFacilityLevel()}.",
			null, false, null
		);

		AddRow(
			$"Stock: Scrap {state.PermanentScrap}, Material {state.PermanentMaterial}, " +
			$"Energy {state.PermanentEnergy}, Essence {state.PermanentEssence}",
			null, false, null
		);

		AddHeader("Facilities");
		AddFacilityUpgradeRow(StationType.Storage);
		AddFacilityUpgradeRow(StationType.Armory);
		AddFacilityUpgradeRow(StationType.Infirmary);

		AddHeader("Workshop");
		AddFacilityUpgradeRow(StationType.Workshop);
	}

	private void BuildCoreChamberContent()
	{
		GameState state = GameState.Instance;

		if (showEnding)
		{
			BuildEnding();
			return;
		}

		AddHeader("CORE CHAMBER");

		AddRow(
			"Install the cores taken from each boss. Cores 1 and 2 unlock further facility upgrades. " +
			"Core 3 ends the time loop.",
			null, false, null
		);

		for (int i = 1; i <= GameDatabase.CoreCount; i++)
		{
			int index = i;
			string label = index == GameDatabase.CoreCount ? $"Core {index} (Final)" : $"Core {index}";

			if (state.IsCoreInstalled(index))
			{
				AddRow($"{label}: installed", "Installed", false, null);
			}
			else if (state.IsCoreOwned(index))
			{
				AddRow($"{label}: ready to install", "Install", true, () =>
				{
					message = state.InstallCore(index);

					if (index == GameDatabase.CoreCount && state.GameWon)
					{
						// The final core is in: the game is over.
						GetTree().Paused = false;
						GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://Scenes/Ending.tscn");
						return;
					}

					Refresh();
				});
			}
			else
			{
				AddRow($"{label}: not found. Defeat Boss {index}. The core comes home when you fall.", "Missing", false, null);
			}
		}

		if (state.GameWon)
			AddRow("The time loop has ended.", "Replay ending", true, () =>
			{
				GetTree().Paused = false;
				GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://Scenes/Ending.tscn");
			});
	}

	private void BuildEnding()
	{
		AddHeader("THE LOOP ENDS");

		AddRow(
			"The final core hums to life. The contamination fades, the anomaly collapses, " +
			"and the shelter is saved.\n\nThank you for playing.",
			null, false, null
		);

		AddRow("", "Back to Main Menu", true, () =>
		{
			GetTree().Paused = false;
			GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
		});

		AddRow("", "Keep exploring", true, () =>
		{
			showEnding = false;
			Refresh();
		});
	}

	private void BuildStorageContent()
	{
		GameState state = GameState.Instance;

		AddHeader($"RESOURCE STORAGE  -  Level {state.StorageLevel}/{GameDatabase.MaxStationLevel}");

		foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
		{
			int cap = GameDatabase.StorageCap(state.StorageLevel, type);
			int bag = GameDatabase.RunCap(state.StorageLevel, type);
			AddRow($"{type}: {state.GetPermanent(type)} / {cap}    (run bag: {bag})", null, false, null);
		}

		AddRow($"Core Fragments: {state.PermanentCoreFragments}", null, false, null);

		AddHeader($"Supplies in stock (carry up to {GameState.ItemCarryLimit} of each per run)");

		foreach (ConsumableDef def in GameDatabase.Consumables)
			AddRow($"{def.Name}: {state.GetPermanentItemCount(def.Id)}", null, false, null);

		AddRow("Upgrade this facility at the Workshop.", null, false, null);
	}

	private void BuildArmoryContent()
	{
		GameState state = GameState.Instance;

		AddHeader($"ARMORY  -  Level {state.ArmoryLevel}/{GameDatabase.MaxStationLevel}");

		if (!state.GearUpQuestDone)
			AddRow("QUEST - Gear Up: craft and equip one item in every slot before heading out.", null, false, null);

		AddRow(
			$"Stock: Scrap {state.PermanentScrap}, Material {state.PermanentMaterial}, " +
			$"Energy {state.PermanentEnergy}, Essence {state.PermanentEssence}",
			null, false, null
		);

		AddRow("Knife and Rifle damage stack. Higher tiers unlock as the Armory levels up.", null, false, null);

		foreach (EquipSlot slot in GameDatabase.AllSlots)
		{
			AddHeader(GameDatabase.SlotDisplayName(slot));

			foreach (EquipmentDef def in GameDatabase.Equipment)
			{
				if (def.Slot == slot)
					AddEquipmentRow(def);
			}
		}

		AddRow("Upgrade this facility at the Workshop.", null, false, null);
	}

	private void BuildInfirmaryContent()
	{
		GameState state = GameState.Instance;

		AddHeader($"INFIRMARY  -  Level {state.InfirmaryLevel}/{GameDatabase.MaxStationLevel}");

		AddRow(
			$"Infection gain reduced by {Math.Round(GameDatabase.InfectionResistance(state.InfirmaryLevel) * 100)}%\n" +
			$"Base infection per death: {GameDatabase.DeathInfectionPenalty(state.InfirmaryLevel):0}%  (now {state.PermanentInfection:0}%, tries left {state.TriesLeft})",
			null, false, null
		);

		AddRow(
			$"Stock: Scrap {state.PermanentScrap}, Material {state.PermanentMaterial}, " +
			$"Energy {state.PermanentEnergy}, Essence {state.PermanentEssence}",
			null, false, null
		);

		AddHeader($"Potions (carry up to {GameState.ItemCarryLimit} of each per run)");

		foreach (ConsumableDef def in GameDatabase.Consumables)
		{
			string id = def.Id;
			ResourceCost cost = state.GetCost(def.Cost);
			string info = $"{def.Name} - {def.Description}\nIn stock: {state.GetPermanentItemCount(id)}";

			if (def.RequiredInfirmaryLevel > state.InfirmaryLevel)
			{
				AddRow($"{info}\nRequires Infirmary Level {def.RequiredInfirmaryLevel}", "Locked", false, null);
				continue;
			}

			AddRow(
				info,
				$"Craft: {cost.ToText()}",
				state.CanAfford(cost),
				() =>
				{
					message = state.CraftConsumable(id);
					Refresh();
				}
			);

			if (def.PermanentInfectionReduction > 0)
			{
				int stock = state.GetPermanentItemCount(id);

				AddRow(
					"Use one now to lower your starting infection.",
					$"Use ({stock})",
					stock > 0 && state.PermanentInfection > 0f,
					() =>
					{
						message = state.UseInfectionReducerFromStock();
						Refresh();
					}
				);
			}
		}

		AddRow("Upgrade this facility at the Workshop.", null, false, null);
	}

	private void AddEquipmentRow(EquipmentDef def)
	{
		GameState state = GameState.Instance;
		string id = def.Id;
		string text = $"{def.Name} (Lv {def.Tier})  ({def.StatText()})";

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
			AddRow(text, $"Craft: {state.GetCost(def.Cost).ToText()}", state.CanAfford(state.GetCost(def.Cost)), () =>
			{
				message = state.CraftEquipment(id);
				Refresh();
			});
		}
	}
}
