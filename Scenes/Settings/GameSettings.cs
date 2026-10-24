using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public class SettingsData
{
	public float MasterVolume { get; set; } = 1f;
	public float MusicVolume { get; set; } = 0.8f;
	public float SfxVolume { get; set; } = 1f;
	public bool Muted { get; set; } = false;
	public Dictionary<string, long> Bindings { get; set; } = new Dictionary<string, long>();
}

public class RebindableAction
{
	public string Action;
	public string Label;
	public Key DefaultKey;
}

public static class GameSettings
{
	public const string MusicBus = "Music";
	public const string SfxBus = "SFX";

	private const string SettingsPath = "user://thymus_settings.json";

	public static float MasterVolume = 1f;
	public static float MusicVolume = 0.8f;
	public static float SfxVolume = 1f;
	public static bool Muted = false;

	public static readonly RebindableAction[] Actions = new RebindableAction[]
	{
		new RebindableAction { Action = "move_up", Label = "Move Up", DefaultKey = Key.W },
		new RebindableAction { Action = "move_down", Label = "Move Down", DefaultKey = Key.S },
		new RebindableAction { Action = "move_left", Label = "Move Left", DefaultKey = Key.A },
		new RebindableAction { Action = "move_right", Label = "Move Right", DefaultKey = Key.D },
		new RebindableAction { Action = "interact", Label = "Interact", DefaultKey = Key.E },
		new RebindableAction { Action = "pause_menu", Label = "Pause", DefaultKey = Key.Escape }
	};

	private static readonly Dictionary<string, long> bindings = new Dictionary<string, long>();
	private static bool loaded = false;

	public static void Load()
	{
		if (loaded)
			return;

		loaded = true;

		EnsureBuses();
		EnsureActions();

		if (FileAccess.FileExists(SettingsPath))
		{
			using FileAccess file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);

			if (file != null)
			{
				try
				{
					SettingsData data = JsonSerializer.Deserialize<SettingsData>(file.GetAsText());

					if (data != null)
					{
						MasterVolume = Mathf.Clamp(data.MasterVolume, 0f, 1f);
						MusicVolume = Mathf.Clamp(data.MusicVolume, 0f, 1f);
						SfxVolume = Mathf.Clamp(data.SfxVolume, 0f, 1f);
						Muted = data.Muted;

						if (data.Bindings != null)
						{
							foreach (KeyValuePair<string, long> pair in data.Bindings)
							{
								if (FindAction(pair.Key) != null)
									bindings[pair.Key] = pair.Value;
							}
						}
					}
				}
				catch (Exception e)
				{
					GD.PushWarning($"Settings file could not be read: {e.Message}");
				}
			}
		}

		ApplyBindings();
		ApplyAudio();
	}

	public static void Save()
	{
		SettingsData data = new SettingsData
		{
			MasterVolume = MasterVolume,
			MusicVolume = MusicVolume,
			SfxVolume = SfxVolume,
			Muted = Muted,
			Bindings = new Dictionary<string, long>(bindings)
		};

		using FileAccess file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);

		if (file == null)
		{
			GD.PushWarning("Could not open settings file for writing.");
			return;
		}

		file.StoreString(JsonSerializer.Serialize(data));
	}

	public static void EnsureBuses()
	{
		EnsureBus(MusicBus);
		EnsureBus(SfxBus);
	}

	private static void EnsureBus(string name)
	{
		if (AudioServer.GetBusIndex(name) >= 0)
			return;

		AudioServer.AddBus();
		int index = AudioServer.BusCount - 1;
		AudioServer.SetBusName(index, name);
		AudioServer.SetBusSend(index, "Master");
	}

	public static void ApplyAudio()
	{
		EnsureBuses();

		int master = AudioServer.GetBusIndex("Master");
		AudioServer.SetBusVolumeDb(master, VolumeToDb(MasterVolume));
		AudioServer.SetBusMute(master, Muted);

		SetBusVolume(MusicBus, MusicVolume);
		SetBusVolume(SfxBus, SfxVolume);
	}

	private static void SetBusVolume(string name, float volume)
	{
		int index = AudioServer.GetBusIndex(name);

		if (index < 0)
			return;

		AudioServer.SetBusVolumeDb(index, VolumeToDb(volume));
		AudioServer.SetBusMute(index, volume <= 0.001f);
	}

	private static float VolumeToDb(float volume)
	{
		return volume <= 0.001f ? -80f : Mathf.LinearToDb(volume);
	}

	private static void EnsureActions()
	{
		foreach (RebindableAction action in Actions)
		{
			if (InputMap.HasAction(action.Action))
				continue;

			InputMap.AddAction(action.Action);
			InputMap.ActionAddEvent(action.Action, MakeEvent(action.DefaultKey));
		}
	}

	private static void ApplyBindings()
	{
		foreach (KeyValuePair<string, long> pair in bindings)
		{
			InputMap.ActionEraseEvents(pair.Key);
			InputMap.ActionAddEvent(pair.Key, MakeEvent((Key)pair.Value));
		}
	}

	private static InputEventKey MakeEvent(Key key)
	{
		InputEventKey keyEvent = new InputEventKey();
		keyEvent.PhysicalKeycode = key;
		return keyEvent;
	}

	public static RebindableAction FindAction(string actionName)
	{
		foreach (RebindableAction action in Actions)
		{
			if (action.Action == actionName)
				return action;
		}

		return null;
	}

	public static Key GetKey(string actionName)
	{
		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(actionName))
		{
			if (inputEvent is InputEventKey keyEvent)
				return keyEvent.PhysicalKeycode != Key.None ? keyEvent.PhysicalKeycode : keyEvent.Keycode;
		}

		return Key.None;
	}

	public static string KeyName(Key key)
	{
		if (key == Key.None)
			return "None";

		string name = OS.GetKeycodeString(key);
		return string.IsNullOrEmpty(name) ? "?" : name;
	}

	public static string GetKeyName(string actionName)
	{
		return KeyName(GetKey(actionName));
	}

	public static RebindableAction FindConflict(Key key, string exceptAction)
	{
		foreach (RebindableAction action in Actions)
		{
			if (action.Action != exceptAction && GetKey(action.Action) == key)
				return action;
		}

		return null;
	}

	public static void SetBinding(string actionName, Key key)
	{
		InputMap.ActionEraseEvents(actionName);
		InputMap.ActionAddEvent(actionName, MakeEvent(key));
		bindings[actionName] = (long)key;
		Save();
	}

	public static void ResetControls()
	{
		foreach (RebindableAction action in Actions)
		{
			InputMap.ActionEraseEvents(action.Action);
			InputMap.ActionAddEvent(action.Action, MakeEvent(action.DefaultKey));
		}

		bindings.Clear();
		Save();
	}

	public static void ResetSound()
	{
		MasterVolume = 1f;
		MusicVolume = 0.8f;
		SfxVolume = 1f;
		Muted = false;
		ApplyAudio();
		Save();
	}
}
