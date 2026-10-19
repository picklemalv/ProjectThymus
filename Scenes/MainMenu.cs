using Godot;

public partial class MainMenu : Node2D
{
	[Export] public float SecondsPerLeg { get; set; } = 15.0f;

	private Sprite2D background;
	private Button startButton;
	private Button quitButton;

	public override void _Ready()
	{
		background = GetNode<Sprite2D>("Sprite2D");
		startButton = GetNode<Button>("StartButton");
		quitButton = GetNode<Button>("QuitButton");

		startButton.Pressed += OnStartPressed;
		quitButton.Pressed += OnQuitPressed;

		StartBackgroundPan();
	}

	private void StartBackgroundPan()
	{
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 backgroundSize = background.Texture.GetSize() * background.Scale;

		// Align the image's top-left corner with the viewport's top-left corner.
		// This assumes the background image itself contains the full 3-column x 2-row panorama.
		Vector2 topLeft = backgroundSize / 2.0f;
		Vector2 maxPan = new Vector2(
			Mathf.Max(0.0f, backgroundSize.X - viewportSize.X),
			Mathf.Max(0.0f, backgroundSize.Y - viewportSize.Y)
		);

		background.Position = topLeft;

		// Moving the sprite left/up lets the viewport reveal the panorama to the right/down.
		Vector2 topRight = topLeft - new Vector2(maxPan.X, 0.0f);
		Vector2 bottomRight = topLeft - maxPan;
		Vector2 bottomLeft = topLeft - new Vector2(0.0f, maxPan.Y);

		Tween tween = CreateTween();
		tween.SetLoops(); // Repeat the full panorama route indefinitely.
		tween.TweenProperty(background, "position", topRight, SecondsPerLeg)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		tween.TweenProperty(background, "position", bottomRight, SecondsPerLeg)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		tween.TweenProperty(background, "position", bottomLeft, SecondsPerLeg)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		tween.TweenProperty(background, "position", topLeft, SecondsPerLeg)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	private void OnStartPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/main.tscn");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
