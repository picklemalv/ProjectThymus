extends Control
## The kit in use: a HUD, a settings window and an inventory, all plain Controls styled by one
## Theme. The buttons along the bottom swap the theme.

const KIT := "res://addons/pixel_ui/"
const ALL_THEMES: Array[String] = ["stone", "wood", "sky", "forest", "royal", "ember"]
const ART_HEIGHT := 240.0

## Store capture only: switch to the next theme every this many frames.
@export var cycle_frames := 0

var frame := 0
var theme_buttons := {}


## Draws the UI at a whole-number scale so the art pixels stay square: the largest that still
## fits `art_height` pixels of art on screen.
static func pixel_scale(node: Node, art_height: float) -> void:
	var window := node.get_tree().root
	window.content_scale_factor = 1.0
	window.content_scale_factor = maxf(1.0, floorf(window.get_visible_rect().size.y / art_height))


static func themes() -> Array[String]:
	var installed: Array[String] = []
	for n in ALL_THEMES:
		if ResourceLoader.exists(KIT + "themes/%s.tres" % n):
			installed.append(n)
	return installed


## The named theme, or the first one installed.
static func kit_theme(name: String) -> Theme:
	return load(KIT + "themes/%s.tres" % (name if name in themes() else themes()[0]))


## Null when the icon isn't installed.
static func icon_texture(name: String) -> Texture2D:
	var path := KIT + "icons/%s.png" % name
	return load(path) if ResourceLoader.exists(path) else null


static func icon(name: String) -> TextureRect:
	var t := TextureRect.new()
	t.texture = icon_texture(name)
	t.stretch_mode = TextureRect.STRETCH_KEEP_CENTERED
	return t


## A titled window; add its content to the returned VBox.
static func window(parent: Control, title: String, at: Vector2, width: float) -> VBoxContainer:
	var panel := PanelContainer.new()
	panel.theme_type_variation = &"WindowPanel"
	panel.position = at
	panel.custom_minimum_size.x = width
	parent.add_child(panel)
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 3)
	panel.add_child(box)
	var label := Label.new()
	label.text = title
	label.theme_type_variation = &"Title"
	box.add_child(label)
	return box


static func row(parent: Control, separation := 3) -> HBoxContainer:
	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", separation)
	parent.add_child(h)
	return h


static func bar(parent: Control, colour: String, value: float, width: float) -> ProgressBar:
	var b := ProgressBar.new()
	b.theme_type_variation = StringName(colour + "Bar")
	b.show_percentage = false
	b.value = value
	b.custom_minimum_size = Vector2(width, 8)
	parent.add_child(b)
	return b


static func slider(parent: Control, label: String, value: float) -> void:
	var r := row(parent)
	var l := Label.new()
	l.text = label
	l.custom_minimum_size.x = 34
	r.add_child(l)
	var s := HSlider.new()
	s.value = value
	s.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	s.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	r.add_child(s)


func _ready() -> void:
	get_viewport().canvas_item_default_texture_filter = Viewport.DEFAULT_CANVAS_ITEM_TEXTURE_FILTER_NEAREST  # project.godot's, for a project without it
	pixel_scale(self, ART_HEIGHT)
	theme = kit_theme("wood")
	_hud()
	_settings()
	_inventory()
	_dialog()
	var switcher := row(self, 2)
	switcher.position = Vector2(6, 220)
	var group := ButtonGroup.new()
	for name in themes():
		var b := Button.new()
		b.text = name.capitalize()
		b.toggle_mode = true
		b.button_group = group
		b.button_pressed = name == "wood"
		b.toggled.connect(func(on: bool) -> void: if on: theme = kit_theme(name))
		switcher.add_child(b)
		theme_buttons[name] = b


func _process(_delta: float) -> void:
	if cycle_frames <= 0:
		return
	frame += 1
	if frame % cycle_frames == 0:
		var names := themes()
		var next: String = names[(names.find("wood") + frame / cycle_frames) % names.size()]
		theme_buttons[next].button_pressed = true


func _hud() -> void:
	var hearts := row(self, 0)
	hearts.position = Vector2(6, 5)
	for name in ["heart", "heart", "heart", "heart_half", "heart_empty"]:
		hearts.add_child(icon(name))
	var bars := VBoxContainer.new()
	bars.position = Vector2(92, 5)
	bars.add_theme_constant_override("separation", 2)
	add_child(bars)
	bar(bars, "Red", 70, 90)
	bar(bars, "Blue", 45, 90)
	var money := row(self, 1)
	money.position = Vector2(300, 5)
	for pair in [["coin", "1250"], ["gem", "12"], ["key", "3"]]:
		money.add_child(icon(pair[0]))
		var l := Label.new()
		l.text = pair[1]
		money.add_child(l)
		var gap := Control.new()
		gap.custom_minimum_size.x = 4
		money.add_child(gap)


func _settings() -> void:
	var box := window(self, "SETTINGS", Vector2(6, 28), 148)
	slider(box, "Music", 70)
	slider(box, "Sound", 40)
	var full := CheckBox.new()
	full.text = "Fullscreen"
	full.button_pressed = true
	box.add_child(full)
	var hints := CheckBox.new()
	hints.text = "Show hints"
	box.add_child(hints)
	var mode := OptionButton.new()
	for m in ["Easy", "Normal", "Hard"]:
		mode.add_item(m)
	mode.select(1)
	box.add_child(mode)
	var name_edit := LineEdit.new()
	name_edit.placeholder_text = "Hero name"
	box.add_child(name_edit)
	var buttons := row(box)
	for t in ["Back", "Apply"]:
		var b := Button.new()
		b.text = t
		b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		buttons.add_child(b)


func _inventory() -> void:
	var box := window(self, "INVENTORY", Vector2(162, 28), 258)
	var tabs := TabContainer.new()
	box.add_child(tabs)
	var items := GridContainer.new()
	items.name = "Items"
	items.columns = 9
	items.add_theme_constant_override("h_separation", 2)
	items.add_theme_constant_override("v_separation", 2)
	tabs.add_child(items)
	var stock := [["potion", 3], ["potion_blue", 2], ["potion_green", 1], ["apple", 5], ["bomb", 4],
		["key", 1], ["gem", 12], ["gem_red", 2], ["gem_green", 0], ["coin_silver", 30], ["star", 0],
		["leaf", 7], ["flame", 0], ["lightning", 0], ["chest", 0], ["bag", 0], ["sword", 0], ["shield", 0]]
	stock = stock.filter(func(pair: Array) -> bool: return icon_texture(pair[0]) != null).slice(0, 16)
	for i in 18:
		var slot := PanelContainer.new()
		slot.theme_type_variation = &"InsetPanel"
		slot.custom_minimum_size = Vector2(24, 24)
		items.add_child(slot)
		if i >= stock.size():
			continue
		var pair: Array = stock[i]
		var art := icon(pair[0])
		art.tooltip_text = String(pair[0]).capitalize()
		slot.add_child(art)
		if pair[1] > 1:
			var count := Label.new()
			count.text = str(pair[1])
			count.add_theme_color_override("font_shadow_color", Color.BLACK)
			art.add_child(count)
			count.position = Vector2(19, 10) - Vector2(count.get_minimum_size().x, 0)
	var gear := VBoxContainer.new()
	gear.name = "Gear"
	tabs.add_child(gear)
	for pair in [["sword", "Iron sword"], ["shield", "Oak shield"]]:
		var r := row(gear)
		r.add_child(icon(pair[0]))
		var l := Label.new()
		l.text = pair[1]
		l.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		r.add_child(l)
		var b := Button.new()
		b.text = "Equip"
		r.add_child(b)
	var quest := row(box)
	quest.add_child(icon("trophy"))
	var l := Label.new()
	l.text = "Level 7"
	quest.add_child(l)
	var xp := bar(quest, "Gold", 60, 0)
	xp.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	xp.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	var actions := row(box)
	for pair in [["check", "Use"], ["trash", "Drop"], ["info", "Info"]]:
		var b := Button.new()
		b.text = pair[1]
		b.icon = icon_texture(pair[0])
		b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		actions.add_child(b)


func _dialog() -> void:
	var panel := PanelContainer.new()
	panel.position = Vector2(6, 176)
	panel.custom_minimum_size.x = 414
	add_child(panel)
	var r := row(panel, 6)
	var face := PanelContainer.new()
	face.theme_type_variation = &"InsetPanel"
	face.add_child(icon("user"))
	r.add_child(face)
	var text := Label.new()
	text.text = "Welcome, traveller! The shop opens at dawn.\nBring coins, and mind the bombs."
	text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	r.add_child(text)
	var next := Button.new()
	next.icon = icon_texture("arrow_right")
	next.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	r.add_child(next)
