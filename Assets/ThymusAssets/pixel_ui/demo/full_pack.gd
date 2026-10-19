extends CanvasLayer
## The free sampler's line to its full pack, along the foot of the demo, right of its first quarter.
## Store captures leave it out.

const TEXT := "Pixel UI has six colour themes (stone, wood, sky, forest, royal and ember) and 50 icons: food, bombs, elements, trophies, locks, and a full menu set with sound, music, settings, save and delete."
const NAME := "Pixel UI"
const URL := "https://heyheythere.itch.io/pixel-ui"

var bg := StyleBoxFlat.new()
var row: HBoxContainer
var label: Label
var link: Button


func _ready() -> void:
	layer = 100
	if Engine.get_write_movie_path() != "":
		return
	await get_tree().process_frame
	var main: String = ProjectSettings.get_setting("application/run/main_scene")
	if main.begins_with("uid://"):
		main = ResourceUID.get_id_path(ResourceUID.text_to_id(main))
	var scene := get_tree().current_scene
	if scene == null or scene.scene_file_path != main:
		return
	var bar := PanelContainer.new()
	bg.bg_color = Color(0.04, 0.04, 0.08, 0.86)
	bar.add_theme_stylebox_override("panel", bg)
	add_child(bar)
	bar.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
	bar.grow_vertical = Control.GROW_DIRECTION_BEGIN
	bar.anchor_left = 0.25  # the bottom-left corner stays the demo's: the UI kits keep their theme buttons there
	row = HBoxContainer.new()
	bar.add_child(row)
	label = Label.new()
	label.text = TEXT
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	label.add_theme_color_override("font_color", Color(0.86, 0.86, 0.92))
	row.add_child(label)
	link = Button.new()
	link.text = "Get " + NAME
	link.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	link.pressed.connect(func() -> void: OS.shell_open(URL))
	row.add_child(link)
	fit()
	get_window().size_changed.connect(fit, CONNECT_DEFERRED)


## The same size on screen whatever content scale the demo sets (the UI kits scale theirs up).
func fit() -> void:
	var s := get_window().content_scale_factor
	for c: Control in [label, link]:
		c.add_theme_font_size_override("font_size", maxi(1, roundi(14.0 / s)))
	bg.set_content_margin_all(6.0 / s)
	bg.content_margin_left = 12.0 / s
	bg.content_margin_right = 12.0 / s
	row.add_theme_constant_override("separation", roundi(12.0 / s))
	bg.corner_radius_top_left = roundi(6.0 / s)
