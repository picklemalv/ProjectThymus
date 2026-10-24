using Godot;

public partial class PauseMenu : Node
{
	private Button _pauseButton;
	private Control _pausePanel;
	private Button _continueButton;
	private Button _menuButton;
	private Button _quitButton;

	public override void _Ready()
	{
		// Keep this autoload responsive while the game is paused.
		ProcessMode = Node.ProcessModeEnum.Always;

		GetTree().SceneChanged += OnSceneChanged;
		CallDeferred(nameof(ConnectToCurrentScene));
	}

	private void OnSceneChanged()
	{
		CallDeferred(nameof(ConnectToCurrentScene));
	}

	private void ConnectToCurrentScene()
	{
		Node scene = GetTree().CurrentScene;
		if (scene == null)
			return;

		_pauseButton = scene.GetNodeOrNull<Button>("UI/PauseButton");
		_pausePanel = scene.GetNodeOrNull<Control>("UI/PausePanel");
		_continueButton = scene.GetNodeOrNull<Button>(
			"UI/PausePanel/ContinueButton");
		_menuButton = scene.GetNodeOrNull<Button>(
			"UI/PausePanel/MenuButton");
		_quitButton = scene.GetNodeOrNull<Button>(
			"UI/PausePanel/QuitButton");

		// Other scenes may not contain the pause menu.
		if (_pauseButton == null ||
			_pausePanel == null ||
			_continueButton == null ||
			_menuButton == null ||
			_quitButton == null)
			return;

		// UI controls must remain usable while the scene tree is paused.
		_pauseButton.ProcessMode = Node.ProcessModeEnum.Always;
		_pausePanel.ProcessMode = Node.ProcessModeEnum.Always;

		_pausePanel.Visible = false;

		// Avoid duplicate connections when scenes change.
		_pauseButton.Pressed -= OpenPauseMenu;
		_continueButton.Pressed -= ResumeGame;
		_menuButton.Pressed -= ReturnToMainMenu;
		_quitButton.Pressed -= QuitGame;

		_pauseButton.Pressed += OpenPauseMenu;
		_continueButton.Pressed += ResumeGame;
		_menuButton.Pressed += ReturnToMainMenu;
		_quitButton.Pressed += QuitGame;

		SetupSettingsButton();
	}

	private void SetupSettingsButton()
	{
		Button settingsButton = _pausePanel.GetNodeOrNull<Button>("SettingsButton");

		if (settingsButton == null)
		{
			settingsButton = new Button();
			settingsButton.Name = "SettingsButton";
			settingsButton.Text = "Settings";
			settingsButton.TextureFilter = _continueButton.TextureFilter;
			settingsButton.AddThemeFontOverride("font", _continueButton.GetThemeFont("font"));
			_pausePanel.AddChild(settingsButton);
		}

		settingsButton.ProcessMode = Node.ProcessModeEnum.Always;
		settingsButton.OffsetLeft = _continueButton.OffsetLeft;
		settingsButton.OffsetRight = _continueButton.OffsetRight;

		PlaceButton(_continueButton, 125f);
		PlaceButton(settingsButton, 200f);
		PlaceButton(_menuButton, 275f);
		PlaceButton(_quitButton, 350f);

		settingsButton.Pressed -= OpenSettings;
		settingsButton.Pressed += OpenSettings;
	}

	private void PlaceButton(Button button, float top)
	{
		float height = _continueButton.OffsetBottom - _continueButton.OffsetTop;
		button.OffsetTop = top;
		button.OffsetBottom = top + height;
	}

	private void OpenSettings()
	{
		SettingsMenu.Open(this);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("pause_menu"))
			return;

		if (_pauseButton == null || _pausePanel == null || !IsInstanceValid(_pausePanel))
			return;

		if (_pausePanel.Visible)
			ResumeGame();
		else if (!GetTree().Paused)
			OpenPauseMenu();
		else
			return;

		GetViewport().SetInputAsHandled();
	}

	private void OpenPauseMenu()
	{
		_pausePanel.Visible = true;
		GetTree().Paused = true;
	}

	private void ResumeGame()
	{
		_pausePanel.Visible = false;
		GetTree().Paused = false;
	}

	private void ReturnToMainMenu()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	private void QuitGame()
	{
		GetTree().Quit();
	}
}
