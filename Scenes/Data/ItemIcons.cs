using Godot;
using System.Collections.Generic;

// =====================================================================================
//  ITEM ICONS  -  sprites for every piece of equipment, resource and potion.
//
//  HOW TO ADD A SPRITE
//  Put a PNG (or .webp / .svg) in  res://Assets/ThymusAssets/Icons/<Folder>/  named exactly
//  like the item id. That is all: the game finds it by name. A missing sprite is simply
//  not drawn, so you can add them one at a time.
//
//      Equipment/  knife_1  knife_2  knife_3  rifle_1 ... accessory_3     (15 files)
//      Resources/  scrap  material  energy  essence  core_fragment        (5 files)
//      Potions/    antidote  health_potion  infection_reducer             (3 files)
//
//  The full list is in  Assets/ThymusAssets/Icons/ICON_LIST.txt.
//  Item ids come from GameDatabase, so new items you add there use the same rules.
//  Small pixel-art sprites are drawn with nearest-neighbour filtering (stay crisp);
//  sprites larger than their slot are smoothed instead.
//
//  Where icons show up:
//    - Shelter stations: every equipment, potion and resource row
//    - "EQUIPPED ITEMS" HUD in the shelter
//    - The potion buttons in combat
//
//  CHECK WHAT IS MISSING:  ItemIcons.ReportMissing();   (prints to the Output panel)
// =====================================================================================
public static class ItemIcons
{
	public const string Root = "res://Assets/ThymusAssets/Icons/";
	private static readonly string[] Extensions = { ".png", ".webp", ".svg" };

	private static readonly Dictionary<string, string> folders = new Dictionary<string, string>();
	private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
	private static bool built = false;

	private static void Build()
	{
		if (built)
			return;

		built = true;

		foreach (EquipmentDef def in GameDatabase.Equipment)
			folders[def.Id] = "Equipment";

		foreach (ConsumableDef def in GameDatabase.Consumables)
			folders[def.Id] = "Potions";

		foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
			folders[type.ToString().ToLowerInvariant()] = "Resources";

		folders["core_fragment"] = "Resources";
	}

	// Returns the sprite for an item id, or null if you have not added it yet.
	public static Texture2D Get(string id)
	{
		if (string.IsNullOrEmpty(id))
			return null;

		Build();

		if (cache.TryGetValue(id, out Texture2D cached))
			return cached;

		if (!folders.TryGetValue(id, out string folder))
			return null;

		foreach (string ext in Extensions)
		{
			string path = $"{Root}{folder}/{id}{ext}";

			if (ResourceLoader.Exists(path))
			{
				Texture2D texture = GD.Load<Texture2D>(path);
				cache[id] = texture;
				return texture;
			}
		}

		return null;   // not cached, so a sprite added while the game runs is picked up later
	}

	// A ready-made icon node, or null if there is no sprite yet.
	public static TextureRect MakeIcon(string id, int size = 40)
	{
		Texture2D texture = Get(id);

		if (texture == null)
			return null;

		TextureRect rect = new TextureRect();
		rect.Texture = texture;
		rect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		rect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		rect.CustomMinimumSize = new Vector2(size, size);

		// Sprites bigger than their slot (e.g. 64px art shown at 44px) look best smoothed;
		// small pixel art that gets scaled up stays crisp with nearest-neighbour.
		rect.TextureFilter = texture.GetWidth() > size
			? CanvasItem.TextureFilterEnum.Linear
			: CanvasItem.TextureFilterEnum.Nearest;

		rect.MouseFilter = Control.MouseFilterEnum.Ignore;
		return rect;
	}

	// Puts the sprite on a button (used for the potion buttons in combat).
	public static void ApplyToButton(Button button, string id, int size = 28)
	{
		Texture2D texture = Get(id);

		if (button == null || texture == null)
			return;

		button.Icon = texture;
		button.IconAlignment = HorizontalAlignment.Left;
		button.ExpandIcon = true;
		button.AddThemeConstantOverride("icon_max_width", size);
		button.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
	}

	// Prints every item that still has no sprite.
	public static void ReportMissing()
	{
		Build();

		int missing = 0;

		foreach (KeyValuePair<string, string> pair in folders)
		{
			if (Get(pair.Key) != null)
				continue;

			missing++;
			GD.Print($"[ICON] missing: {pair.Key}   ->   {Root}{pair.Value}/{pair.Key}.png");
		}

		GD.Print($"[ICON] {folders.Count - missing} of {folders.Count} sprites found, {missing} missing.");
	}
}
