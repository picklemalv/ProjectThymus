using Godot;

public partial class MainMenu : Node2D
{
	[Export] public float SecondsPerLeg { get; set; } = 15.0f;

	private Sprite2D background;
	private Button continueButton;
	private Button newGameButton;
	private Button quitButton;

	public override void _Ready()
	{
		background = GetNode<Sprite2D>("Sprite2D");
		continueButton = GetNode<Button>("ContinueButton");
		newGameButton = GetNode<Button>("NewGameButton");
		quitButton = GetNode<Button>("QuitButton");

		continueButton.Disabled = !GameState.Instance.HasProgress;
		continueButton.Pressed += OnContinuePressed;
		newGameButton.Pressed += OnNewGamePressed;
		quitButton.Pressed += OnQuitPressed;

		StartBackgroundPan();
	}

	private void StartBackgroundPan()
	{
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 backgroundSize = background.Texture.GetSize() * background.Scale;

		Vector2 topLeft = backgroundSize / 2.0f;
		Vector2 maxPan = new Vector2(
			Mathf.Max(0.0f, backgroundSize.X - viewportSize.X),
			Mathf.Max(0.0f, backgroundSize.Y - viewportSize.Y)
		);

		background.Position = topLeft;

		Vector2 topRight = topLeft - new Vector2(maxPan.X, 0.0f);
		Vector2 bottomRight = topLeft - maxPan;
		Vector2 bottomLeft = topLeft - new Vector2(0.0f, maxPan.Y);

		Tween tween = CreateTween();
		tween.SetLoops();
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

	private void OnContinuePressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Shelter/Shelter.tscn");
	}

	private void OnNewGamePressed()
	{
		// Nothing to lose on a fresh save, so skip the warning.
		if (!GameState.Instance.HasProgress)
		{
			StartNewGame();
			return;
		}

		ConfirmationDialog dialog = new ConfirmationDialog();
		dialog.Title = "New Game";
		dialog.DialogText = "Start a new game?\nYour saved progress will be erased.";
		dialog.OkButtonText = "Erase & Start";
		dialog.CancelButtonText = "Cancel";
		dialog.Confirmed += StartNewGame;
		AddChild(dialog);
		dialog.PopupCentered();
	}

	private void StartNewGame()
	{
		GameState.Instance.ResetAllProgress();
		GetTree().ChangeSceneToFile("res://Scenes/Shelter/Shelter.tscn");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
