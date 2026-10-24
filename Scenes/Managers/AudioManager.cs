
using Godot;

public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; }

	private AudioStreamPlayer musicPlayer;
	private AudioStreamPlayer globalMusicPlayer;
	private AudioStreamPlayer soundPlayer;

	private string currentMusicPath = "";

	
	public override void _Ready()
	{
		Instance = this;
		
		ProcessMode = Node.ProcessModeEnum.Always;

		musicPlayer = GetNode<AudioStreamPlayer>("MusicPlayer");
		globalMusicPlayer = GetNode<AudioStreamPlayer>("GlobalMusicPlayer");
		soundPlayer = GetNode<AudioStreamPlayer>("SoundPlayer");

		GameSettings.Load();
		musicPlayer.Bus = GameSettings.MusicBus;
		globalMusicPlayer.Bus = GameSettings.MusicBus;
		soundPlayer.Bus = GameSettings.SfxBus;

		string path =
			"res://Assets/ThymusAssets/Audio/StockTune-Echoes Of The Fallen_1792780190.mp3";

		AudioStream music = GD.Load<AudioStream>(path);

		if (music == null)
		{
			GD.PushError($"Failed to load global BGM: {path}");
			return;
		}

		globalMusicPlayer.Stream = music;
		globalMusicPlayer.VolumeDb = 0.0f;

		// Enable looping for MP3 music.
		if (music is AudioStreamMP3 mp3)
		{
			mp3.Loop = true;
		}

		globalMusicPlayer.Play();

		GD.Print("Global BGM loaded. Playing: ",
			globalMusicPlayer.Playing);
	}
	
	public void PlaySound(string soundPath)
	{
		AudioStream sound = GD.Load<AudioStream>(soundPath);

		if (sound == null)
		{
			GD.PushError($"Could not load sound: {soundPath}");
			return;
		}

		soundPlayer.Stream = sound;
		soundPlayer.Play();
	}


	public void PlayMusic(string musicPath)
	{
		if (currentMusicPath == musicPath && musicPlayer.Playing)
			return;

		AudioStream music = GD.Load<AudioStream>(musicPath);

		if (music == null)
		{
			GD.PushError($"Could not load music: {musicPath}");
			return;
		}

		musicPlayer.Stream = music;
		currentMusicPath = musicPath;
		musicPlayer.Play();
	}

	public void StopMusic()
	{
		musicPlayer.Stop();
		currentMusicPath = "";
	}
}
