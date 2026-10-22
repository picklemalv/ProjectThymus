using Godot;

public partial class GameOver : Control
{
	public override void _Ready()
	{
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
		title.Text = "THE CONTAMINATION CONSUMED YOU";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 36);
		box.AddChild(title);

		Label detail = new Label();
		detail.Text =
			$"The loop could not be broken.\n" +
			$"You lasted {GameState.Instance.LastGameLoops} loops.";
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
	}
}
