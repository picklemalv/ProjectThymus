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
