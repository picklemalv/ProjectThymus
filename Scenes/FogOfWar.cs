using Godot;

/// <summary>
/// Screen-space, pixelated fog of war for the main exploration scene.
/// Attach this node under Main. The overlay is added to the existing UI CanvasLayer
/// with a low Z index, keeping the pause menu visible above it.
/// </summary>
public partial class FogOfWar : Node2D
{
    [Export] public NodePath PlayerPath { get; set; } = new NodePath("../Objects/Player");
    [Export] public float VisionRadiusPixels { get; set; } = 220.0f;
    [Export] public float PixelSize { get; set; } = 8.0f;
    [Export] public float EdgeSoftnessPixels { get; set; } = 42.0f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float FogOpacity { get; set; } = 0.88f;
    [Export] public Color FogColor { get; set; } = new Color(0.025f, 0.04f, 0.055f, 1.0f);

    private Node2D _player = null!;
    private ColorRect _overlay = null!;
    private ShaderMaterial _fogMaterial = null!;

    private const string FogShaderCode = @"
shader_type canvas_item;

uniform vec2 player_screen_pos = vec2(640.0, 360.0);
uniform float vision_radius_px = 220.0;
uniform float pixel_size = 8.0;
uniform float edge_softness_px = 42.0;
uniform float fog_opacity : hint_range(0.0, 1.0) = 0.88;
uniform vec4 fog_color : source_color = vec4(0.025, 0.04, 0.055, 1.0);

float cell_random(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
    // Snap the fog and player positions to a coarse pixel grid.
    vec2 cell = floor(FRAGCOORD.xy / pixel_size);
    vec2 snapped_pixel = (cell + vec2(0.5)) * pixel_size;
    vec2 player_cell = floor(player_screen_pos / pixel_size);
    vec2 snapped_player = (player_cell + vec2(0.5)) * pixel_size;

    // Random jitter gives the edge a chunky, particle-like silhouette.
    float noise = cell_random(cell + vec2(floor(TIME * 2.0), floor(TIME * 1.3)));
    float jitter = (noise - 0.5) * edge_softness_px * 0.9;
    float distance_to_player = distance(snapped_pixel, snapped_player) + jitter;

    float darkness = smoothstep(
        vision_radius_px - edge_softness_px,
        vision_radius_px + edge_softness_px * 0.25,
        distance_to_player
    );

    // Tiny brighter square flecks near the boundary create a subtle particle-fog look.
    float fleck_random = cell_random(cell + vec2(43.0, floor(TIME * 3.0)));
    float near_edge = smoothstep(vision_radius_px + 8.0, vision_radius_px + 24.0, distance_to_player)
                    * (1.0 - smoothstep(vision_radius_px + 42.0, vision_radius_px + 90.0, distance_to_player));
    float fleck = step(0.94, fleck_random) * near_edge;

    vec3 fog_rgb = min(fog_color.rgb + vec3(fleck * 0.10), vec3(1.0));
    COLOR = vec4(fog_rgb, fog_opacity * darkness);
}
";

    public override void _Ready()
    {
        _player = GetNode<Node2D>(PlayerPath);

        // Reuse the existing UI CanvasLayer so the fog covers the world but stays
        // behind the pause button and pause panel.
        CanvasLayer uiLayer = GetNode<CanvasLayer>("../UI");

        Shader shader = new Shader { Code = FogShaderCode };
        _fogMaterial = new ShaderMaterial { Shader = shader };
        ApplyShaderParameters();

        _overlay = new ColorRect
        {
            Name = "FogOverlay",
            Color = Colors.White,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = _fogMaterial,
            ZIndex = -100
        };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        uiLayer.AddChild(_overlay);
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_player) || !GodotObject.IsInstanceValid(_overlay))
            return;

        // Convert the player's world location to viewport pixels, accounting for the camera.
        Vector2 playerScreenPosition = GetViewport().GetCanvasTransform() * _player.GlobalPosition;
        _fogMaterial.SetShaderParameter("player_screen_pos", playerScreenPosition);
        ApplyShaderParameters();
    }

    private void ApplyShaderParameters()
    {
        _fogMaterial.SetShaderParameter("vision_radius_px", VisionRadiusPixels);
        _fogMaterial.SetShaderParameter("pixel_size", Mathf.Max(1.0f, PixelSize));
        _fogMaterial.SetShaderParameter("edge_softness_px", EdgeSoftnessPixels);
        _fogMaterial.SetShaderParameter("fog_opacity", FogOpacity);
        _fogMaterial.SetShaderParameter("fog_color", FogColor);
    }
}
