using Godot;

public partial class Ending : Control
{
	public override void _Ready()
	{
		GetTree().Paused = false;

		int loops = GameState.Instance.DeathCount;

		SetAnchorsPreset(LayoutPreset.FullRect);

		ColorRect background = new ColorRect();
		background.Color = new Color(0.02f, 0.02f, 0.03f);
		background.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(background);

		CenterContainer center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);

		VBoxContainer box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 18);
		center.AddChild(box);

		Label title = new Label();
		title.Text = "THE LOOP ENDS";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 40);
		box.AddChild(title);

		Label detail = new Label();
		detail.Text =
			"The final core hums to life.\n" +
			"The contamination fades, the anomaly collapses,\n" +
			"and the shelter is saved.\n\n" +
			$"Loops endured: {loops}\n\n" +
			"Thank you for playing.";
		detail.HorizontalAlignment = HorizontalAlignment.Center;
		box.AddChild(detail);

		Button newGame = new Button();
		newGame.Text = "New Game";
		newGame.CustomMinimumSize = new Vector2(240, 48);
		newGame.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/Shelter/Shelter.tscn");
		box.AddChild(newGame);

		Button menu = new Button();
		menu.Text = "Main Menu";
		menu.CustomMinimumSize = new Vector2(240, 48);
		menu.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
		box.AddChild(menu);

		Button quit = new Button();
		quit.Text = "Quit";
		quit.CustomMinimumSize = new Vector2(240, 48);
		quit.Pressed += () => GetTree().Quit();
		box.AddChild(quit);

		// The run is finished: wipe the save so the next game starts fresh.
		GameState.Instance.ResetAllProgress();
	}
}
