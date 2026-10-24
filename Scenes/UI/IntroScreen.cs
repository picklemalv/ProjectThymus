using Godot;
using System.Collections.Generic;

// "How to play" guide shown before the player crafts their first gear.
// Built entirely in code, so there is no scene to wire up.
// Uses the same pixel font as the shelter HUD (native size 8, so text sizes are multiples of 8).
//
// PICTURE SLOTS: every page has a picture area. Drop a PNG with the file name shown
// in the slot into  res://Assets/ThymusAssets/Intro/  and it appears automatically.
// Until the file exists, a placeholder with the expected file name is shown.
//
// To change the text, edit BuildPages() below.
public partial class IntroScreen : CanvasLayer
{
	public const string PictureFolder = "res://Assets/ThymusAssets/Intro/";
	private const string FontPath = "res://Assets/ThymusAssets/pixel_ui/font/pixel_ui_font.fnt";

	public static bool IsOpen { get; private set; } = false;

	private class Page
	{
		public string Title;
		public string Body;
		public string Picture;   // file name inside PictureFolder
	}

	private readonly List<Page> pages = new List<Page>();
	private int index = 0;
	private bool wasPaused = false;
	private Font font;

	private Label titleLabel;
	private Label counterLabel;
	private RichTextLabel bodyLabel;
	private TextureRect pictureRect;
	private Label placeholderLabel;
	private Button backButton;
	private Button nextButton;

	public static void Open(Node host)
	{
		if (IsOpen || host == null)
			return;

		host.AddChild(new IntroScreen());
	}

	public override void _Ready()
	{
		IsOpen = true;
		Layer = 100;
		ProcessMode = ProcessModeEnum.Always;

		wasPaused = GetTree().Paused;
		GetTree().Paused = true;

		if (ResourceLoader.Exists(FontPath))
			font = GD.Load<Font>(FontPath);

		BuildPages();
		BuildUi();
		ShowPage(0);
	}

	public override void _ExitTree()
	{
		IsOpen = false;
	}

	// ------------------------------------------------------------------ content

	private void BuildPages()
	{
		float penalty = GameDatabase.DeathInfectionPenalty(1);

		AddPage("Welcome, Runner",
			"The world is wrapped in a contaminating fog. You are a [b]Runner[/b]: one of the few who can still leave the shelter to scavenge resources and [b]Core Fragments[/b].\n\n" +
			"Every time a Runner falls, time loops. You wake up back in the shelter with your memories intact, but your body is a little more worn.\n\n" +
			"Each trip makes the shelter stronger. Your goal is to reach the source of the contamination and end the loop.",
			"intro_01_welcome.png");

		AddPage("The Loop",
			"[b]1. Shelter[/b]: prepare, craft gear, upgrade facilities.\n" +
			"[b]2. Expedition[/b]: explore the contaminated zones, fight, collect.\n" +
			"[b]3. Death or 100% Infection[/b]: time loops and you wake up in the shelter.\n" +
			"[b]4. Upgrade[/b]: upgrades are permanent and survive every loop.\n\n" +
			$"What you carry home is banked as permanent loot (as long as storage has room). But every death raises your [b]base infection[/b] by about {penalty:0}%. If it ever reaches 100%, the whole run resets.",
			"intro_02_loop.png");

		AddPage("Infection",
			"Outside the shelter your [b]Infection Meter[/b] (0 to 100%) keeps filling. It rises while you explore and with every action you take in a fight. Deeper zones are worse.\n\n" +
			"At 100% the expedition ends and the loop resets.\n\n" +
			"[b]Base infection[/b] is the level every expedition starts from. It only goes up when you die. [b]Antidotes[/b] cut the meter by 15% during a run, and [b]Infection Reducers[/b] lower your base infection permanently.",
			"intro_03_infection.png");

		AddPage("Combat",
			"Touching an enemy starts a turn-based fight. On your turn you can:\n\n" +
			"[b]Attack[/b]: damage = your Attack + Weapon damage - the enemy's Defense.\n" +
			"[b]Defend[/b]: halves the next hit you take.\n" +
			"[b]Item[/b]: use a potion or antidote.\n" +
			"[b]Escape[/b]: a 50% chance to run away.\n\n" +
			$"Every action costs infection. Bosses warn you before a special attack, so Defend then. You can carry up to {GameState.ItemCarryLimit} of each item.",
			"intro_04_combat.png");

		AddPage("Resources and Core Fragments",
			"[b]Scrap[/b], [b]Material[/b], [b]Energy[/b] and [b]Contaminated Essence[/b] drop from enemies and bosses. Deeper zones pay more.\n\n" +
			$"You can carry only so much per expedition (base limits: Scrap {GameDatabase.RunCapScrap}, Material {GameDatabase.RunCapMaterial}, Energy {GameDatabase.RunCapEnergy}, Essence {GameDatabase.RunCapEssence}). Upgrading Resource Storage raises these limits.\n\n" +
			$"[b]Core Fragments[/b] are dropped by the {GameDatabase.CoreCount} bosses. Bring them home and install them at the Core Chamber.",
			"intro_05_resources.png");

		AddPage("Station: Armory",
			"Press [b]E[/b] at the Armory to craft and equip gear in five slots: Knife, Rifle, Head, Body and Accessory.\n\n" +
			"Weapons add damage, armor adds Defense and HP, and accessories resist infection.\n\n" +
			"Better tiers need a higher Armory level. You must equip all five slots before the Expedition Gate will let you out.",
			"intro_06_armory.png");

		AddPage("Station: Workshop",
			"The Workshop makes everything cheaper. Each level takes a percentage off the cost of crafting gear, brewing items and upgrading the other facilities.\n\n" +
			"It does not discount its own upgrades, so it pays off the more you build.",
			"intro_07_workshop.png");

		AddPage("Station: Infirmary",
			"Craft [b]Antidotes[/b] and [b]Health Potions[/b] here, and from level 2 also [b]Infection Reducers[/b].\n\n" +
			"Upgrading the Infirmary slows how fast infection builds up and shrinks the base-infection penalty you suffer each time you die.",
			"intro_08_infirmary.png");

		AddPage("Station: Resource Storage",
			"Everything you bank lives here. Its level sets how much the shelter can hold and how much you can carry on each expedition.\n\n" +
			"Anything that does not fit is lost, so upgrade it before it fills up.",
			"intro_09_storage.png");

		AddPage("Station: Core Chamber",
			"Install the Core Fragments you recovered. Each installed core lets the other facilities reach one level higher.\n\n" +
			"Install the final core to break the time loop and end the game.",
			"intro_10_core_chamber.png");

		AddPage("Your First Goal",
			"Start with the [b]Gear Up[/b] quest: craft and equip one item in every slot at the Armory (the top-right room). You begin with just enough scrap and material for the basic gear.\n\n" +
			"Then walk through the Expedition Gate.\n\n" +
			"Move with [b]WASD[/b] and interact with [b]E[/b]. You can reopen this guide any time with [b]H[/b].",
			"intro_11_first_goal.png");
	}

	private void AddPage(string title, string body, string picture)
	{
		pages.Add(new Page { Title = title, Body = body, Picture = picture });
	}

	// ------------------------------------------------------------------ ui

	private void BuildUi()
	{
		ColorRect dim = new ColorRect();
		dim.Color = new Color(0f, 0f, 0f, 0.85f);
		dim.MouseFilter = Control.MouseFilterEnum.Stop;
		dim.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(dim);

		Panel panel = new Panel();
		panel.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;   // keeps the pixel font crisp for every child
		panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		panel.OffsetLeft = 48;
		panel.OffsetTop = 32;
		panel.OffsetRight = -48;
		panel.OffsetBottom = -32;
		panel.AddThemeStyleboxOverride("panel", MakeBox(new Color(0.07f, 0.08f, 0.10f), new Color(0.35f, 0.40f, 0.45f), 3));
		AddChild(panel);

		MarginContainer margin = new MarginContainer();
		margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 24);
		margin.AddThemeConstantOverride("margin_right", 24);
		margin.AddThemeConstantOverride("margin_top", 18);
		margin.AddThemeConstantOverride("margin_bottom", 18);
		panel.AddChild(margin);

		VBoxContainer root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 12);
		margin.AddChild(root);

		// header
		HBoxContainer header = new HBoxContainer();
		root.AddChild(header);

		titleLabel = MakeLabel(32, new Color(0.95f, 0.85f, 0.55f));
		titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		header.AddChild(titleLabel);

		counterLabel = MakeLabel(16, new Color(0.65f, 0.70f, 0.75f));
		counterLabel.VerticalAlignment = VerticalAlignment.Center;
		header.AddChild(counterLabel);

		// content: picture slot on the left, text on the right
		HBoxContainer content = new HBoxContainer();
		content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		content.AddThemeConstantOverride("separation", 20);
		root.AddChild(content);

		PanelContainer pictureFrame = new PanelContainer();
		pictureFrame.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		pictureFrame.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		pictureFrame.SizeFlagsStretchRatio = 1.15f;
		pictureFrame.CustomMinimumSize = new Vector2(380, 0);
		pictureFrame.AddThemeStyleboxOverride("panel", MakeBox(new Color(0.12f, 0.13f, 0.16f), new Color(0.45f, 0.50f, 0.55f), 2));
		content.AddChild(pictureFrame);

		pictureRect = new TextureRect();
		pictureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		pictureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		pictureRect.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		pictureRect.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		pictureRect.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		pictureFrame.AddChild(pictureRect);

		placeholderLabel = MakeLabel(16, new Color(0.60f, 0.65f, 0.70f));
		placeholderLabel.HorizontalAlignment = HorizontalAlignment.Center;
		placeholderLabel.VerticalAlignment = VerticalAlignment.Center;
		placeholderLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		placeholderLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		placeholderLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		pictureFrame.AddChild(placeholderLabel);

		bodyLabel = new RichTextLabel();
		bodyLabel.BbcodeEnabled = true;
		bodyLabel.ScrollActive = true;
		bodyLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		bodyLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		bodyLabel.SizeFlagsStretchRatio = 1.0f;
		bodyLabel.AddThemeColorOverride("default_color", new Color(0.88f, 0.90f, 0.92f));

		if (font != null)
		{
			bodyLabel.AddThemeFontOverride("normal_font", font);
			bodyLabel.AddThemeFontOverride("bold_font", font);
		}

		bodyLabel.AddThemeFontSizeOverride("normal_font_size", 16);
		bodyLabel.AddThemeFontSizeOverride("bold_font_size", 16);
		content.AddChild(bodyLabel);

		// footer
		HBoxContainer footer = new HBoxContainer();
		footer.AddThemeConstantOverride("separation", 10);
		root.AddChild(footer);

		Button skip = MakeButton("Skip guide");
		skip.Pressed += Close;
		footer.AddChild(skip);

		Control spacer = new Control();
		spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		footer.AddChild(spacer);

		backButton = MakeButton("< Back");
		backButton.Pressed += Back;
		footer.AddChild(backButton);

		nextButton = MakeButton("Next >");
		nextButton.Pressed += Next;
		footer.AddChild(nextButton);
	}

	private StyleBoxFlat MakeBox(Color fill, Color border, int borderWidth)
	{
		StyleBoxFlat box = new StyleBoxFlat();
		box.BgColor = fill;
		box.BorderColor = border;
		box.SetBorderWidthAll(borderWidth);
		box.SetCornerRadiusAll(4);
		return box;
	}

	private Label MakeLabel(int size, Color color)
	{
		Label label = new Label();
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);

		if (font != null)
			label.AddThemeFontOverride("font", font);

		return label;
	}

	private Button MakeButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.FocusMode = Control.FocusModeEnum.None;
		button.CustomMinimumSize = new Vector2(130, 36);
		button.AddThemeFontSizeOverride("font_size", 16);

		if (font != null)
			button.AddThemeFontOverride("font", font);

		return button;
	}

	// ------------------------------------------------------------------ paging

	private void ShowPage(int newIndex)
	{
		index = Mathf.Clamp(newIndex, 0, pages.Count - 1);
		Page page = pages[index];

		titleLabel.Text = page.Title;
		counterLabel.Text = $"{index + 1} / {pages.Count}";
		bodyLabel.Text = page.Body;

		string path = PictureFolder + page.Picture;

		if (ResourceLoader.Exists(path))
		{
			pictureRect.Texture = GD.Load<Texture2D>(path);
			placeholderLabel.Visible = false;
		}
		else
		{
			pictureRect.Texture = null;
			placeholderLabel.Visible = true;
			placeholderLabel.Text = $"PICTURE SLOT\n\n{page.Picture}\n\nPut the image in\n{PictureFolder}\n(suggested 640 x 360)";
		}

		backButton.Disabled = index == 0;
		nextButton.Text = index == pages.Count - 1 ? "Start playing" : "Next >";
	}

	private void Next()
	{
		if (index >= pages.Count - 1)
			Close();
		else
			ShowPage(index + 1);
	}

	private void Back()
	{
		ShowPage(index - 1);
	}

	private void Close()
	{
		GetTree().Paused = wasPaused;

		if (GameState.Instance != null && !GameState.Instance.IntroSeen)
		{
			GameState.Instance.IntroSeen = true;
			GameState.Instance.SaveGame();
		}

		QueueFree();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey key || !key.Pressed || key.Echo)
			return;

		switch (key.Keycode)
		{
			case Key.Right:
			case Key.Enter:
			case Key.KpEnter:
			case Key.Space:
				Next();
				break;
			case Key.Left:
				Back();
				break;
			case Key.Escape:
				Close();
				break;
			default:
				return;
		}

		GetViewport().SetInputAsHandled();
	}
}
