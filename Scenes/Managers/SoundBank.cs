using Godot;
using System.Collections.Generic;

// =====================================================================================
//  SOUND BANK  -  the one place for every sound listed in the GDD (section 5.2).
//
//  HOW TO ADD A SOUND
//  1. Put the file (.ogg, .wav or .mp3) in  res://Assets/ThymusAssets/Audio/<Folder>/
//     and name it exactly like the "file" column below (without the extension).
//     Example:  Assets/ThymusAssets/Audio/SFX/attack.ogg
//  2. That is all. The game finds it by name. Missing files are skipped silently.
//
//  WANT A DIFFERENT FILE NAME OR LOCATION?
//     Type the full path in the last argument of that line, for example
//     Add("attack", Kind.Sfx, "SFX", "attack", 0f, "res://Assets/MyStuff/slash.mp3");
//
//  VOLUME: the number before the path is the volume in dB (0 = original, -6 = quieter).
//
//  HOW TO PLAY A SOUND FROM CODE
//     SoundBank.Play("attack");               // one-shot effect
//     SoundBank.PlayMusic("music_combat");    // background music (replaces the previous one)
//     SoundBank.PlayAmbient("ambient_shelter");
//     SoundBank.PlayLoop("infection_heartbeat", "infection");  // looping layer on its own channel
//     SoundBank.StopMusic();  SoundBank.StopLoop("infection");
//
//  CHECK WHAT IS STILL MISSING:  SoundBank.ReportMissing();   (prints to the Output panel)
//
//  Use .ogg or .mp3 for anything that loops (music, ambience, infection layers).
//  NOTE: the old global background track in AudioManager keeps playing on its own.
//        Once you use music_shelter etc., stop that one in AudioManager._Ready().
// =====================================================================================
public static class SoundBank
{
	public enum Kind { Music, Sfx, Loop }

	private class Entry
	{
		public string Id;
		public Kind Kind;
		public string Folder;
		public string File;
		public float VolumeDb;
		public string Path;   // optional full path override
	}

	private const string Root = "res://Assets/ThymusAssets/Audio/";
	private static readonly string[] Extensions = { ".ogg", ".wav", ".mp3" };

	private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
	private static readonly Dictionary<string, AudioStream> cache = new Dictionary<string, AudioStream>();
	private static readonly Dictionary<string, AudioStreamPlayer> channels = new Dictionary<string, AudioStreamPlayer>();
	private static readonly Dictionary<string, string> channelSound = new Dictionary<string, string>();
	private static readonly HashSet<string> warned = new HashSet<string>();

	static SoundBank()
	{
		// id, kind, folder, file name (no extension), volume dB, optional full path
		Add("music_shelter", Kind.Music, "Music", "music_shelter", -6f, "");  // Shelter: calm, simple, safe-feeling music.
		Add("music_exploration", Kind.Music, "Music", "music_exploration", -6f, "");  // Exploration: dark, slow tempo, lonely and unsafe.
		Add("music_combat", Kind.Music, "Music", "music_combat", -6f, "");  // Combat: faster tempo, raises tension.
		Add("music_high_contamination", Kind.Music, "Music", "music_high_contamination", -6f, "");  // High contamination area: more intense, unstable music or ambience.
		Add("music_final_boss", Kind.Music, "Music", "music_final_boss", -6f, "");  // Final boss: heavy, tense, big threat.
		Add("music_ending", Kind.Music, "Music", "music_ending", -6f, "");  // Ending: after the final boss is defeated and the loop stops.
		Add("footstep", Kind.Sfx, "SFX", "footstep", -8f, "");  // Player movement: footstep.
		Add("door_open", Kind.Sfx, "SFX", "door_open", 0f, "");  // Opening a door.
		Add("pickup_resource", Kind.Sfx, "SFX", "pickup_resource", 0f, "");  // Picking up a resource.
		Add("item_use", Kind.Sfx, "SFX", "item_use", 0f, "");  // Using an item.
		Add("attack", Kind.Sfx, "SFX", "attack", 0f, "");  // Attacking.
		Add("hit_impact", Kind.Sfx, "SFX", "hit_impact", 0f, "");  // Hit landing on an enemy (hit / impact).
		Add("skill", Kind.Sfx, "SFX", "skill", 0f, "");  // Using a skill (skill effect).
		Add("enemy_death", Kind.Sfx, "SFX", "enemy_death", 0f, "");  // Enemy dies.
		Add("interact", Kind.Sfx, "SFX", "interact", 0f, "");  // Interacting with an object.
		Add("pickup_core_fragment", Kind.Sfx, "SFX", "pickup_core_fragment", 0f, "");  // Getting a Core Fragment (special item sound).
		Add("extraction", Kind.Sfx, "SFX", "extraction", 0f, "");  // Extraction / confirmation sound.
		Add("ui_click", Kind.Sfx, "SFX", "ui_click", -4f, "");  // UI: button click.
		Add("ui_select", Kind.Sfx, "SFX", "ui_select", -4f, "");  // UI: menu selection.
		Add("ui_notification", Kind.Sfx, "SFX", "ui_notification", -4f, "");  // UI: notification.
		Add("infection_heartbeat", Kind.Loop, "Infection", "infection_heartbeat", -12f, "");  // Heartbeat; gets stronger as infection rises.
		Add("infection_drone", Kind.Loop, "Infection", "infection_drone", -14f, "");  // Low-frequency drone.
		Add("infection_hum", Kind.Loop, "Infection", "infection_hum", -14f, "");  // Humming sound.
		Add("infection_distortion", Kind.Loop, "Infection", "infection_distortion", -16f, "");  // Audio distortion.
		Add("infection_breath", Kind.Loop, "Infection", "infection_breath", -12f, "");  // Breathing (running out of oxygen).
		Add("infection_near_max", Kind.Loop, "Infection", "infection_near_max", -10f, "");  // Strongest effect as the meter approaches 100%.
		Add("time_loop_trigger", Kind.Sfx, "Infection", "time_loop_trigger", 0f, "");  // Special effect: infection hit 100%, expedition failed, Time Loop activates.
		Add("boss_music", Kind.Music, "Boss", "boss_music", -6f, "");  // Boss background music.
		Add("boss_ambience", Kind.Loop, "Boss", "boss_ambience", -14f, "");  // Boss ambience.
		Add("boss_attack", Kind.Sfx, "Boss", "boss_attack", 0f, "");  // Boss attack sound.
		Add("boss_heavy_impact", Kind.Sfx, "Boss", "boss_heavy_impact", 0f, "");  // Heavy impact.
		Add("boss_skill", Kind.Sfx, "Boss", "boss_skill", 0f, "");  // Boss skill sound.
		Add("boss_damage", Kind.Sfx, "Boss", "boss_damage", 0f, "");  // Boss takes damage.
		Add("boss_death", Kind.Sfx, "Boss", "boss_death", 0f, "");  // Boss death sound.
		Add("boss_transition", Kind.Sfx, "Boss", "boss_transition", 0f, "");  // Ending / transition sound after the boss is defeated.
		Add("ambient_shelter", Kind.Loop, "Ambient", "ambient_shelter", -14f, "");  // Ambient sound for the shelter (optional).
		Add("ambient_wasteland", Kind.Loop, "Ambient", "ambient_wasteland", -14f, "");  // Ambient sound for the contaminated area (optional).
	}

	private static void Add(string id, Kind kind, string folder, string file, float volumeDb, string path)
	{
		entries[id] = new Entry { Id = id, Kind = kind, Folder = folder, File = file, VolumeDb = volumeDb, Path = path };
	}

	// ----------------------------------------------------------------- public API

	public static bool Has(string id)
	{
		return entries.ContainsKey(id) && LoadStream(entries[id]) != null;
	}

	public static void Play(string id, float pitchScale = 1f)
	{
		if (!TryGet(id, out Entry entry))
			return;

		AudioStream stream = LoadStream(entry);
		SceneTree tree = Engine.GetMainLoop() as SceneTree;

		if (stream == null || tree == null)
			return;

		AudioStreamPlayer player = new AudioStreamPlayer();
		player.Stream = stream;
		player.VolumeDb = entry.VolumeDb;
		player.PitchScale = pitchScale;
		player.ProcessMode = Node.ProcessModeEnum.Always;
		player.Autoplay = true;
		player.Finished += player.QueueFree;

		tree.Root.CallDeferred(Node.MethodName.AddChild, player);
	}

	public static void PlayMusic(string id)
	{
		PlayLoop(id, "music");
	}

	public static void PlayAmbient(string id)
	{
		PlayLoop(id, "ambient");
	}

	public static void StopMusic()
	{
		StopLoop("music");
	}

	public static void PlayLoop(string id, string channel)
	{
		if (!TryGet(id, out Entry entry))
			return;

		if (channelSound.TryGetValue(channel, out string current) && current == id
			&& channels.TryGetValue(channel, out AudioStreamPlayer playing)
			&& GodotObject.IsInstanceValid(playing) && playing.Playing)
			return;

		AudioStream stream = LoadStream(entry);
		SceneTree tree = Engine.GetMainLoop() as SceneTree;

		if (stream == null || tree == null)
			return;

		if (stream is AudioStreamMP3 mp3)
			mp3.Loop = true;
		else if (stream is AudioStreamOggVorbis ogg)
			ogg.Loop = true;

		if (!channels.TryGetValue(channel, out AudioStreamPlayer player) || !GodotObject.IsInstanceValid(player))
		{
			player = new AudioStreamPlayer();
			player.ProcessMode = Node.ProcessModeEnum.Always;
			channels[channel] = player;
			tree.Root.CallDeferred(Node.MethodName.AddChild, player);
		}

		player.Stream = stream;
		player.VolumeDb = entry.VolumeDb;
		channelSound[channel] = id;

		if (player.IsInsideTree())
			player.Play();
		else
			player.Autoplay = true;   // starts as soon as it enters the tree
	}

	public static void StopLoop(string channel)
	{
		if (channels.TryGetValue(channel, out AudioStreamPlayer player) && GodotObject.IsInstanceValid(player))
		{
			player.Autoplay = false;
			player.Stop();
		}

		channelSound.Remove(channel);
	}

	// Change a running loop's volume, e.g. SoundBank.SetLoopVolume("infection", -20f).
	public static void SetLoopVolume(string channel, float volumeDb)
	{
		if (channels.TryGetValue(channel, out AudioStreamPlayer player) && GodotObject.IsInstanceValid(player))
			player.VolumeDb = volumeDb;
	}

	// Prints every sound that has no file yet.
	public static void ReportMissing()
	{
		int missing = 0;

		foreach (Entry entry in entries.Values)
		{
			if (FindPath(entry) != null)
				continue;

			missing++;
			GD.Print($"[SOUND] missing: {entry.Id}   ->   {Root}{entry.Folder}/{entry.File}.ogg (or .wav / .mp3)");
		}

		GD.Print($"[SOUND] {entries.Count - missing} of {entries.Count} sounds found, {missing} missing.");
	}

	// ----------------------------------------------------------------- internals

	private static bool TryGet(string id, out Entry entry)
	{
		if (entries.TryGetValue(id, out entry))
			return true;

		if (warned.Add(id))
			GD.PushWarning($"SoundBank: unknown sound id '{id}'.");

		return false;
	}

	private static string FindPath(Entry entry)
	{
		if (!string.IsNullOrEmpty(entry.Path))
			return ResourceLoader.Exists(entry.Path) ? entry.Path : null;

		foreach (string ext in Extensions)
		{
			string path = $"{Root}{entry.Folder}/{entry.File}{ext}";

			if (ResourceLoader.Exists(path))
				return path;
		}

		return null;
	}

	private static AudioStream LoadStream(Entry entry)
	{
		if (cache.TryGetValue(entry.Id, out AudioStream cached))
			return cached;

		string path = FindPath(entry);

		if (path == null)
			return null;

		AudioStream stream = GD.Load<AudioStream>(path);

		if (stream != null)
			cache[entry.Id] = stream;

		return stream;
	}
}
