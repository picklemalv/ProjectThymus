using Godot;

public partial class ShelterSpawn : Marker2D
{
	public override void _Ready()
	{
		if (!GameState.Instance.SpawnAtInfirmary)
			return;

		GameState.Instance.SpawnAtInfirmary = false;
		CallDeferred(nameof(MovePlayer));
	}

	private void MovePlayer()
	{
		Node2D player = GetParent().GetNodeOrNull<Node2D>("Player");

		if (player == null)
		{
			GD.PushWarning("ShelterSpawn: no Player node found next to this marker.");
			return;
		}

		player.GlobalPosition = GlobalPosition;

		Camera2D camera = player.GetNodeOrNull<Camera2D>("Camera2D");

		if (camera != null)
			camera.ResetSmoothing();
	}
}
