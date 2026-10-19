# Pixel UI

A pixel-art GUI skin for Godot 4.3+: colour themes as ready-made `Theme` resources, icons, bar
fills, a cursor and a bitmap font. Every image is also a plain PNG for any other engine.

**The full pack:** **[Pixel UI](https://heyheythere.itch.io/pixel-ui)** has six colour themes (stone, wood, sky, forest, royal and ember) and
50 icons: food, bombs, elements, trophies, locks, and a full menu set with sound, music, settings,
save and delete. Same files and names: install it over this one.

## Use it

1. Copy `addons/pixel_ui/` into your project.
2. Project Settings:
   - *Rendering > Textures > Default Texture Filter*: **Nearest**, or the pixels blur.
   - *GUI > Theme > Custom*: `res://addons/pixel_ui/themes/wood.tres` (or any theme), so every
     Control uses it. Or set `theme` on one Control and its children follow.
3. Draw the UI at a whole-number scale. The art is 1x (the font is 8px tall), made for a UI about
   240 pixels high: set *Display > Window > Stretch > Mode* to `canvas_items` and either use a
   small viewport (for example 320x180 or 480x270), or keep yours and set
   `get_tree().root.content_scale_factor` to 2, 3 or 4, as `demo/demo.gd` does.

## Themes

Each `themes/<name>.tres` styles Button, OptionButton, CheckBox (check and radio), Label, Panel,
PanelContainer, LineEdit, HSlider, ProgressBar, TabContainer, PopupMenu, tooltips, scroll bars and
HSeparator. Set `theme_type_variation` for the extras:

| Variation | On | Looks like |
|---|---|---|
| `WindowPanel` | PanelContainer | a window with a title bar; its first line of content sits in the bar |
| `Title` | Label | dark text for that title bar |
| `InsetPanel` | PanelContainer | a sunken slot, 24x24 around a 16px icon |
| `RedBar`, `GreenBar`, `BlueBar`, `GoldBar` | ProgressBar | health, stamina, mana and XP fills |

A window: PanelContainer (`WindowPanel`) > VBoxContainer > Label (`Title`) first, then the content.

## Files

- `themes/<name>.tres` and `themes/<name>/*.png`: the Theme and its 9-slice parts.
- `icons/*.png`: 16x16, outlined, on a transparent background. `icons/_sheet.png` has all of them
  on one 8-column sheet.
- `bars/bar_*.png`: the bar fills.
- `cursor.png` and `cursor_2x/3x/4x.png`: a mouse pointer, hotspot at the top-left pixel. The OS
  draws cursors unscaled, so pick the copy that matches your UI scale for *Display > Mouse Cursor >
  Custom Image*.
- `font/pixel_ui_font.fnt`: a proportional BMFont with all printable ASCII, 8px glyphs on a 10px
  line. Keep it at size 8 and scale the UI instead.
