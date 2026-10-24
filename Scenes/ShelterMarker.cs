using Godot;

// The round "SHELTER" marker shown at the expedition start once the final core is carried.
public partial class ShelterMarker : Node2D
{
	public float Radius = 70f;
	public Color Fill = new Color(0.3f, 0.9f, 0.5f, 0.35f);
	public Color Ring = new Color(0.55f, 0.95f, 0.65f, 0.9f);

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, Radius, Fill);
		DrawArc(Vector2.Zero, Radius, 0f, Mathf.Tau, 64, Ring, 3f, true);
	}
}
