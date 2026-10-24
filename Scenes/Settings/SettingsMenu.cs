using Godot;
using System.Collections.Generic;

public partial class SettingsMenu : CanvasLayer
{
	private const string FontPath = "res://Assets/ThymusAssets/pixel_ui/font/pixel_ui_font.fnt";
	private const string TestSoundPath = "res://Assets/ThymusAssets/Audio/footstep.mp3";

	private static SettingsMenu instance;

	private static readonly Color PanelBg = new Color(0.02f, 0.06f, 0.04f, 0.98f);
	private static readonly Color ButtonBg = new Color(0.03f, 0.075f, 0.05f);
	private static readonly Color ButtonHover = new Color(0.06f, 0.15f, 0.10f);
	private static readonly Color ButtonPressed = new Color(0.09f, 0.22f, 0.14f);
	private static readonly Color Border = new Color(0.30f, 0.55f, 0.30f);
	private static readonly Color BorderBright = new Color(0.55f, 0.85f, 0.45f);
	private static readonly Color TextColor = new Color(0.56f, 0.82f, 0.48f);
	private static readonly Color TextBright = new Color(0.78f, 0.96f, 0.66f);
	private static readonly Color TextDim = new Color(0.27f, 0.40f, 0.26f);
	private static readonly Color WarningColor = new Color(0.95f, 0.55f, 0.40f);

	private Theme theme;
	private Control soundPage;
	private Control controlsPage;
	private Button soundTab;
	private Button controlsTab;
	private Label messageLabel;
	private Button muteButton;

	private string waitingAction = null;
	private readonly Dictionary<string, Button> keyButtons = new Dictionary<string, Button>();

	public static void Open(Node host)
	{
		if (instance != null && GodotObject.IsInstanceValid(instance))
			return;

		SettingsMenu menu = new SettingsMenu();
		host.GetTree().Root.AddChild(menu);
		instance = menu;
	}

	public override void _Ready()
	{
		Layer = 95;
		ProcessMode = ProcessModeEnum.Always;

		BuildTheme();
		BuildUi();
		ShowTab(true);
	}

	private static StyleBoxFlat Box(Color background, Color border, int borderWidth, float margin)
	{
		StyleBoxFlat box = new StyleBoxFlat();
		box.BgColor = background;
		box.BorderColor = border;
		box.SetBorderWidthAll(borderWidth);
		box.SetCornerRadiusAll(0);
		box.ContentMarginLeft = margin;
		box.ContentMarginRight = margin;
		box.ContentMarginTop = Mathf.Max(4f, margin / 2f);
		box.ContentMarginBottom = Mathf.Max(4f, margin / 2f);
		return box;
	}

	private static ImageTexture MakeGrabber(Color fill, Color border)
	{
		Image image = Image.CreateEmpty(12, 22, false, Image.Format.Rgba8);
		image.Fill(border);

		for (int y = 2; y < 20; y++)
		{
			for (int x = 2; x < 10; x++)
				image.SetPixel(x, y, fill);
		}

		return ImageTexture.CreateFromImage(image);
	}

	private void BuildTheme()
	{
		theme = new Theme();

		Font font = GD.Load<Font>(FontPath);

		if (font != null)
			theme.DefaultFont = font;

		theme.DefaultFontSize = 16;

		theme.SetColor("font_color", "Label", TextColor);

		theme.SetColor("font_color", "Button", TextColor);
		theme.SetColor("font_hover_color", "Button", TextBright);
		theme.SetColor("font_pressed_color", "Button", TextBright);
		theme.SetColor("font_hover_pressed_color", "Button", TextBright);
		theme.SetColor("font_focus_color", "Button", TextBright);
		theme.SetColor("font_disabled_color", "Button", TextDim);

		theme.SetStylebox("normal", "Button", Box(ButtonBg, Border, 2, 12f));
		theme.SetStylebox("hover", "Button", Box(ButtonHover, BorderBright, 2, 12f));
		theme.SetStylebox("pressed", "Button", Box(ButtonPressed, BorderBright, 2, 12f));
		theme.SetStylebox("hover_pressed", "Button", Box(ButtonPressed, BorderBright, 2, 12f));
		theme.SetStylebox("disabled", "Button", Box(ButtonBg, TextDim, 2, 12f));
		theme.SetStylebox("focus", "Button", new StyleBoxEmpty());

		theme.SetStylebox("slider", "HSlider", Box(new Color(0.01f, 0.03f, 0.02f), Border, 1, 0f));
		theme.SetStylebox("grabber_area", "HSlider", Box(new Color(0.30f, 0.55f, 0.26f), Border, 0, 0f));
		theme.SetStylebox("grabber_area_highlight", "HSlider", Box(new Color(0.42f, 0.72f, 0.34f), Border, 0, 0f));
		theme.SetIcon("grabber", "HSlider", MakeGrabber(TextColor, Border));
		theme.SetIcon("grabber_highlight", "HSlider", MakeGrabber(TextBright, BorderBright));
		theme.SetIcon("grabber_disabled", "HSlider", MakeGrabber(TextDim, Border));

		theme.SetStylebox("scroll", "VScrollBar", Box(new Color(0.01f, 0.03f, 0.02f), Border, 1, 2f));
		theme.SetStylebox("grabber", "VScrollBar", Box(new Color(0.30f, 0.55f, 0.26f), Border, 1, 2f));
		theme.SetStylebox("grabber_highlight", "VScrollBar", Box(new Color(0.42f, 0.72f, 0.34f), BorderBright, 1, 2f));
		theme.SetStylebox("grabber_pressed", "VScrollBar", Box(new Color(0.56f, 0.85f, 0.45f), BorderBright, 1, 2f));
	}

	private void BuildUi()
	{
		Control root = new Control();
		root.Theme = theme;
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.MouseFilter = Control.MouseFilterEnum.Stop;
		AddChild(root);

		ColorRect dim = new ColorRect();
		dim.Color = new Color(0f, 0f, 0f, 0.85f);
		dim.MouseFilter = Control.MouseFilterEnum.Ignore;
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(dim);

		CenterContainer center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(center);

		PanelContainer panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", Box(PanelBg, Border, 3, 24f));
		center.AddChild(panel);

		VBoxContainer box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 14);
		panel.AddChild(box);

		Label title = new Label();
		title.Text = "SETTINGS";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 24);
		title.AddThemeColorOverride("font_color", TextBright);
		box.AddChild(title);

		HBoxContainer tabs = new HBoxContainer();
		tabs.Alignment = BoxContainer.AlignmentMode.Center;
		tabs.AddThemeConstantOverride("separation", 12);
		box.AddChild(tabs);

		soundTab = MakeTab("SOUND");
		soundTab.Pressed += () => ShowTab(true);
		tabs.AddChild(soundTab);

		controlsTab = MakeTab("CONTROLS");
		controlsTab.Pressed += () => ShowTab(false);
		tabs.AddChild(controlsTab);

		Control pages = new Control();
		pages.CustomMinimumSize = new Vector2(640, 280);
		box.AddChild(pages);

		soundPage = BuildSoundPage();
		soundPage.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		pages.AddChild(soundPage);

		controlsPage = BuildControlsPage();
		controlsPage.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		pages.AddChild(controlsPage);

		messageLabel = new Label();
		messageLabel.HorizontalAlignment = HorizontalAlignment.Center;
		messageLabel.CustomMinimumSize = new Vector2(0, 20);
		box.AddChild(messageLabel);

		Button back = new Button();
		back.Text = "BACK";
		back.CustomMinimumSize = new Vector2(220, 40);
		back.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		back.Pressed += Close;
		box.AddChild(back);
	}

	private Button MakeTab(string text)
	{
		Button tab = new Button();
		tab.Text = text;
		tab.ToggleMode = true;
		tab.CustomMinimumSize = new Vector2(200, 40);
		return tab;
	}

	private void ShowTab(bool sound)
	{
		CancelRebind();
		SetMessage("", false);

		soundPage.Visible = sound;
		controlsPage.Visible = !sound;
		soundTab.SetPressedNoSignal(sound);
		controlsTab.SetPressedNoSignal(!sound);
	}

	private void SetMessage(string text, bool warning)
	{
		messageLabel.Text = text;
		messageLabel.AddThemeColorOverride("font_color", warning ? WarningColor : TextColor);
	}

	private Control BuildSoundPage()
	{
		VBoxContainer page = new VBoxContainer();
		page.AddThemeConstantOverride("separation", 18);

		AddVolumeRow(page, "Master", GameSettings.MasterVolume, value => GameSettings.MasterVolume = value, false);
		AddVolumeRow(page, "Music", GameSettings.MusicVolume, value => GameSettings.MusicVolume = value, false);
		AddVolumeRow(page, "Effects", GameSettings.SfxVolume, value => GameSettings.SfxVolume = value, true);

		muteButton = new Button();
		muteButton.ToggleMode = true;
		muteButton.ButtonPressed = GameSettings.Muted;
		muteButton.CustomMinimumSize = new Vector2(260, 40);
		muteButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		UpdateMuteText();
		muteButton.Toggled += on =>
		{
			GameSettings.Muted = on;
			GameSettings.ApplyAudio();
			UpdateMuteText();
		};
		page.AddChild(muteButton);

		Button reset = new Button();
		reset.Text = "RESET SOUND";
		reset.CustomMinimumSize = new Vector2(260, 40);
		reset.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		reset.Pressed += () =>
		{
			GameSettings.ResetSound();
			RebuildSoundPage();
			SetMessage("Sound reset.", false);
		};
		page.AddChild(reset);

		return page;
	}

	private void RebuildSoundPage()
	{
		Node parent = soundPage.GetParent();
		bool visible = soundPage.Visible;

		parent.RemoveChild(soundPage);
		soundPage.QueueFree();

		soundPage = BuildSoundPage();
		soundPage.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		soundPage.Visible = visible;
		parent.AddChild(soundPage);
		parent.MoveChild(soundPage, 0);
	}

	private void UpdateMuteText()
	{
		muteButton.Text = GameSettings.Muted ? "MUTE ALL: ON" : "MUTE ALL: OFF";
	}

	private void AddVolumeRow(VBoxContainer page, string name, float value, System.Action<float> apply, bool playTest)
	{
		HBoxContainer row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 14);
		page.AddChild(row);

		Label label = new Label();
		label.Text = name;
		label.CustomMinimumSize = new Vector2(120, 0);
		row.AddChild(label);

		HSlider slider = new HSlider();
		slider.MinValue = 0;
		slider.MaxValue = 100;
		slider.Step = 1;
		slider.Value = Mathf.Round(value * 100f);
		slider.CustomMinimumSize = new Vector2(360, 28);
		slider.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		slider.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		row.AddChild(slider);

		Label percent = new Label();
		percent.Text = $"{(int)slider.Value}%";
		percent.CustomMinimumSize = new Vector2(64, 0);
		percent.HorizontalAlignment = HorizontalAlignment.Right;
		row.AddChild(percent);

		slider.ValueChanged += newValue =>
		{
			percent.Text = $"{(int)newValue}%";
			apply((float)newValue / 100f);
			GameSettings.ApplyAudio();
		};

		if (playTest)
		{
			slider.DragEnded += changed =>
			{
				if (AudioManager.Instance != null)
					AudioManager.Instance.PlaySound(TestSoundPath);
			};
		}
	}

	private Control BuildControlsPage()
	{
		ScrollContainer scroll = new ScrollContainer();
		scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;

		VBoxContainer page = new VBoxContainer();
		page.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		page.AddThemeConstantOverride("separation", 10);
		scroll.AddChild(page);

		Label hint = new Label();
		hint.Text = "Click a key to change it. Esc cancels.";
		hint.AddThemeColorOverride("font_color", TextDim);
		page.AddChild(hint);

		keyButtons.Clear();

		foreach (RebindableAction action in GameSettings.Actions)
		{
			HBoxContainer row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 14);
			page.AddChild(row);

			Label label = new Label();
			label.Text = action.Label;
			label.CustomMinimumSize = new Vector2(240, 0);
			row.AddChild(label);

			string actionName = action.Action;
			Button keyButton = new Button();
			keyButton.Text = GameSettings.GetKeyName(actionName);
			keyButton.CustomMinimumSize = new Vector2(220, 36);
			keyButton.Pressed += () => BeginRebind(actionName);
			row.AddChild(keyButton);

			keyButtons[actionName] = keyButton;
		}

		Label mouse = new Label();
		mouse.Text = "Menus and battle buttons: Mouse click";
		mouse.AddThemeColorOverride("font_color", TextDim);
		page.AddChild(mouse);

		Label panels = new Label();
		panels.Text = "Close station panels: Interact key or Esc";
		panels.AddThemeColorOverride("font_color", TextDim);
		page.AddChild(panels);

		Button reset = new Button();
		reset.Text = "RESET CONTROLS";
		reset.CustomMinimumSize = new Vector2(260, 40);
		reset.Pressed += () =>
		{
			CancelRebind();
			GameSettings.ResetControls();
			RefreshKeyButtons();
			SetMessage("Controls reset.", false);
		};
		page.AddChild(reset);

		return scroll;
	}

	private void RefreshKeyButtons()
	{
		foreach (KeyValuePair<string, Button> pair in keyButtons)
			pair.Value.Text = GameSettings.GetKeyName(pair.Key);
	}

	private void BeginRebind(string actionName)
	{
		CancelRebind();

		waitingAction = actionName;
		keyButtons[actionName].Text = "Press a key...";
		SetMessage("Press the new key. Esc cancels.", false);
	}

	private void CancelRebind()
	{
		if (waitingAction == null)
			return;

		waitingAction = null;
		RefreshKeyButtons();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey key || !key.Pressed || key.Echo)
			return;

		if (waitingAction != null)
		{
			GetViewport().SetInputAsHandled();

			if (key.Keycode == Key.Escape)
			{
				CancelRebind();
				SetMessage("", false);
				return;
			}

			Key newKey = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
			RebindableAction conflict = GameSettings.FindConflict(newKey, waitingAction);

			if (conflict != null)
			{
				SetMessage($"{GameSettings.KeyName(newKey)} is already used by {conflict.Label}.", true);
				CancelRebind();
				return;
			}

			GameSettings.SetBinding(waitingAction, newKey);
			waitingAction = null;
			RefreshKeyButtons();
			SetMessage("Saved.", false);
			return;
		}

		if (key.Keycode == Key.Escape)
		{
			GetViewport().SetInputAsHandled();
			Close();
		}
	}

	private void Close()
	{
		GameSettings.Save();

		if (instance == this)
			instance = null;

		QueueFree();
	}
}
