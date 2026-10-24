using Godot;
using System.Collections.Generic;

// A green dashed line from the player to the Expedition Gate, plus a pulsing ring and a
// "GO OUT" label at the gate. It appears once the Gear Up quest is done.
//
// The line does NOT cut straight through walls: it follows a path through the shelter that the
// player can really walk. The walkable area comes from the collision shapes that already exist
// in the scene (the "Map" body), tested with the player's own collision shape.
//
// By default it only shows until the first time the Runner dies (the first loop).
// Set OnlyBeforeFirstLoop to false to show it every time the quest is done.
public partial class GateGuide : Node2D
{
	private const string PixelFontPath = "res://Assets/ThymusAssets/pixel_ui/font/pixel_ui_font.fnt";
	private const bool OnlyBeforeFirstLoop = true;

	// Set to false to go back to the old straight line.
	private const bool UsePathfinding = true;

	// Set to true to draw the walkability grid (red = the player can't stand there). Useful for checking.
	private const bool DebugDrawGrid = false;

	private const float CellSize = 8f;             // walkability grid resolution (pixels)
	private const float ProbeRadius = 9f;          // size of the "can the player stand here?" test (the player's collider is 24 tall)
	private const int CellsPerPhysicsFrame = 800;  // spreads the one-time grid build over a few frames
	private const float RepathDistance = 6f;       // recompute the path when the player moved this far
	private const float RepathInterval = 0.2f;     // ...or at least this often (seconds)
	private const float LineOfSightStep = 4f;

	private static readonly Color Green = new Color(0.45f, 0.95f, 0.55f);

	private Node2D gate;
	private Node2D player;
	private CollisionObject2D playerBody;
	private CollisionShape2D playerShape;
	private Font font;

	// walkability grid
	private bool gridFailed = false;
	private bool gridReady = false;
	private bool[] blocked;
	private int gridW;
	private int gridH;
	private Vector2 gridOrigin;
	private int buildCursor = 0;
	private AStarGrid2D astar;
	private Vector2 shapeOffset;
	private CircleShape2D probe;
	private int lastPathStatus = -1;   // 0 = full path, 1 = partial path, 2 = none (for logging only)

	// current path (world coordinates) and its cumulative lengths
	private Vector2[] pathPoints = null;
	private float[] pathCum = null;
	private Vector2 lastRepathFrom = new Vector2(float.MaxValue, float.MaxValue);
	private float repathTimer = 0f;

	public static void Attach(Node2D gate)
	{
		if (gate == null || gate.GetParent() == null)
			return;

		GateGuide guide = new GateGuide();
		guide.gate = gate;
		gate.GetParent().CallDeferred(Node.MethodName.AddChild, guide);
	}

	public override void _Ready()
	{
		TopLevel = true;            // draw in world coordinates
		ZAsRelative = false;
		ZIndex = 20;
		TextureFilter = CanvasItem.TextureFilterEnum.Nearest;

		font = ResourceLoader.Exists(PixelFontPath) ? GD.Load<Font>(PixelFontPath) : ThemeDB.FallbackFont;
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (gate == null || !GodotObject.IsInstanceValid(gate))
			return;

		GameState state = GameState.Instance;

		bool show = state != null
			&& state.GearUpQuestDone
			&& (!OnlyBeforeFirstLoop || state.DeathCount == 0);

		Visible = show;

		if (!show)
			return;

		EnsurePlayer();

		if (UsePathfinding && gridReady && player != null)
		{
			repathTimer -= (float)delta;
			Vector2 playerPos = player.GlobalPosition;

			if (repathTimer <= 0f || playerPos.DistanceTo(lastRepathFrom) > RepathDistance)
			{
				Repath(playerPos);
				lastRepathFrom = playerPos;
				repathTimer = RepathInterval;
			}
		}

		QueueRedraw();
	}

	// The physics space may only be queried safely during the physics step.
	public override void _PhysicsProcess(double delta)
	{
		if (!UsePathfinding || !Visible || gridReady || gridFailed)
			return;

		EnsurePlayer();

		if (player == null || playerBody == null)
		{
			if (player != null)
				gridFailed = true;   // player is not a physics body: fall back to the straight line

			return;
		}

		BuildGridStep();
	}

	private void EnsurePlayer()
	{
		if (gate == null || !GodotObject.IsInstanceValid(gate) || gate.GetParent() == null)
			return;

		if (player != null && GodotObject.IsInstanceValid(player))
			return;

		player = gate.GetParent().GetNodeOrNull<Node2D>("Player");
		playerBody = player as CollisionObject2D;
		playerShape = player?.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
	}

	private Vector2 GatePosition()
	{
		CollisionShape2D shape = gate.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		return shape != null ? shape.GlobalPosition : gate.GlobalPosition;
	}

	// ---------------------------------------------------------------- walkable grid

	private void BuildGridStep()
	{
		if (blocked == null)
		{
			Rect2 bounds = ComputeBounds();

			gridOrigin = bounds.Position;
			gridW = Mathf.Max(1, Mathf.CeilToInt(bounds.Size.X / CellSize));
			gridH = Mathf.Max(1, Mathf.CeilToInt(bounds.Size.Y / CellSize));
			blocked = new bool[gridW * gridH];

			// where the player's collision shape sits relative to its origin
			shapeOffset = playerShape != null ? playerShape.GlobalPosition - player.GlobalPosition : Vector2.Zero;
			probe = new CircleShape2D();
			probe.Radius = ProbeRadius;
			buildCursor = 0;
		}

		PhysicsDirectSpaceState2D space = GetWorld2D().DirectSpaceState;

		PhysicsShapeQueryParameters2D query = new PhysicsShapeQueryParameters2D();
		query.Shape = probe;
		query.CollisionMask = playerBody.CollisionMask;
		query.CollideWithBodies = true;
		query.CollideWithAreas = false;
		query.Exclude = new Godot.Collections.Array<Rid> { playerBody.GetRid() };

		int total = gridW * gridH;
		int end = Mathf.Min(total, buildCursor + CellsPerPhysicsFrame);

		for (int i = buildCursor; i < end; i++)
		{
			int cx = i % gridW;
			int cy = i / gridW;
			Vector2 origin = CellCenter(cx, cy);

			query.Transform = new Transform2D(0f, origin + shapeOffset);
			blocked[i] = space.IntersectShape(query, 1).Count > 0;
		}

		buildCursor = end;

		if (buildCursor < total)
			return;

		astar = new AStarGrid2D();
		astar.Region = new Rect2I(0, 0, gridW, gridH);
		astar.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
		astar.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Octile;
		astar.DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Octile;
		astar.Update();

		for (int i = 0; i < total; i++)
		{
			if (blocked[i])
				astar.SetPointSolid(new Vector2I(i % gridW, i / gridW), true);
		}

		int blockedCount = 0;

		for (int i = 0; i < total; i++)
		{
			if (blocked[i])
				blockedCount++;
		}

		GD.Print($"[GateGuide] walkability grid ready: {gridW}x{gridH} cells, {blockedCount} blocked.");

		if (blockedCount == 0)
			GD.PushWarning("[GateGuide] no blocked cells found - the player's collision layer/mask may not match the Map walls.");

		gridReady = true;
	}

	private Rect2 ComputeBounds()
	{
		Rect2 bounds;
		Sprite2D map = gate.GetParent().GetNodeOrNull<Sprite2D>("Map/Map");

		if (map != null && map.Texture != null)
		{
			Vector2 size = map.Texture.GetSize() * map.GlobalScale.Abs();
			bounds = new Rect2(map.GlobalPosition - size / 2f, size);
		}
		else
		{
			bounds = new Rect2(gate.GlobalPosition - new Vector2(1500f, 1000f), new Vector2(3000f, 2000f));
		}

		bounds = bounds.Expand(GatePosition());
		bounds = bounds.Expand(player.GlobalPosition);
		return bounds.Grow(CellSize);
	}

	private Vector2 CellCenter(int cx, int cy)
	{
		return gridOrigin + new Vector2((cx + 0.5f) * CellSize, (cy + 0.5f) * CellSize);
	}

	private Vector2I ToCell(Vector2 world)
	{
		Vector2 local = (world - gridOrigin) / CellSize;
		return new Vector2I(Mathf.FloorToInt(local.X), Mathf.FloorToInt(local.Y));
	}

	private bool InGrid(Vector2I c)
	{
		return c.X >= 0 && c.Y >= 0 && c.X < gridW && c.Y < gridH;
	}

	private bool IsFree(Vector2I c, HashSet<int> forcedFree)
	{
		if (!InGrid(c))
			return false;

		int idx = c.Y * gridW + c.X;
		return !blocked[idx] || (forcedFree != null && forcedFree.Contains(idx));
	}

	// ---------------------------------------------------------------- path

	private void Repath(Vector2 from)
	{
		pathPoints = null;
		pathCum = null;

		Vector2 target = GatePosition();
		Vector2I start = ToCell(from);

		if (!InGrid(start))
			return;

		// The cells right around the player count as walkable (the player may be touching a wall).
		HashSet<int> forced = new HashSet<int>();
		List<Vector2I> unblocked = new List<Vector2I>();

		for (int dy = -2; dy <= 2; dy++)
		{
			for (int dx = -2; dx <= 2; dx++)
			{
				Vector2I c = new Vector2I(start.X + dx, start.Y + dy);

				if (!InGrid(c))
					continue;

				int idx = c.Y * gridW + c.X;

				if (blocked[idx])
				{
					forced.Add(idx);
					unblocked.Add(c);
					astar.SetPointSolid(c, false);
				}
			}
		}

		Vector2I goal;
		bool haveGoal = FindNearestFree(ToCell(target), 24, forced, out goal);
		Godot.Collections.Array<Vector2I> ids = null;

		int status = 2;

		if (haveGoal)
		{
			ids = astar.GetIdPath(start, goal);
			status = 0;

			// no complete route: lead as far toward the gate as the player can actually get
			if (ids == null || ids.Count < 2)
			{
				ids = astar.GetIdPath(start, goal, true);
				status = 1;
			}
		}

		foreach (Vector2I c in unblocked)
			astar.SetPointSolid(c, true);

		if (status != lastPathStatus)
		{
			lastPathStatus = status;

			if (status == 0)
				GD.Print("[GateGuide] full walkable path found.");
			else if (status == 1)
				GD.PushWarning("[GateGuide] no complete walkable path to the gate - showing the closest reachable route.");
			else
				GD.PushWarning("[GateGuide] no walkable path found (is the gate inside a wall?).");
		}

		if (!haveGoal || ids == null || ids.Count < 2)
			return;

		List<Vector2> raw = new List<Vector2>();
		raw.Add(from);

		for (int i = 1; i < ids.Count; i++)
			raw.Add(CellCenter(ids[i].X, ids[i].Y));

		// Pull the path tight: skip every waypoint that has a clear walkable line to the next one.
		List<Vector2> smooth = new List<Vector2>();
		smooth.Add(raw[0]);
		int a = 0;

		while (a < raw.Count - 1)
		{
			int b = a + 1;

			while (b + 1 < raw.Count && LineWalkable(raw[a], raw[b + 1], forced))
				b++;

			smooth.Add(raw[b]);
			a = b;
		}

		pathPoints = smooth.ToArray();
		pathCum = new float[pathPoints.Length];

		for (int i = 1; i < pathPoints.Length; i++)
			pathCum[i] = pathCum[i - 1] + pathPoints[i - 1].DistanceTo(pathPoints[i]);
	}

	private bool FindNearestFree(Vector2I center, int maxRadius, HashSet<int> forced, out Vector2I result)
	{
		for (int r = 0; r <= maxRadius; r++)
		{
			float bestDist = float.MaxValue;
			bool found = false;
			result = center;

			for (int dy = -r; dy <= r; dy++)
			{
				for (int dx = -r; dx <= r; dx++)
				{
					if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r)
						continue;

					Vector2I c = new Vector2I(center.X + dx, center.Y + dy);

					if (!IsFree(c, forced))
						continue;

					float d = dx * dx + dy * dy;

					if (d < bestDist)
					{
						bestDist = d;
						result = c;
						found = true;
					}
				}
			}

			if (found)
				return true;
		}

		result = center;
		return false;
	}

	private bool LineWalkable(Vector2 a, Vector2 b, HashSet<int> forced)
	{
		float length = a.DistanceTo(b);
		int steps = Mathf.Max(1, Mathf.CeilToInt(length / LineOfSightStep));

		for (int i = 1; i <= steps; i++)
		{
			Vector2 p = a.Lerp(b, i / (float)steps);

			if (!IsFree(ToCell(p), forced))
				return false;
		}

		return true;
	}

	private Vector2 PointAlongPath(float distance)
	{
		if (distance <= 0f)
			return pathPoints[0];

		for (int i = 1; i < pathPoints.Length; i++)
		{
			if (distance <= pathCum[i])
			{
				float seg = pathCum[i] - pathCum[i - 1];
				float t = seg > 0.0001f ? (distance - pathCum[i - 1]) / seg : 0f;
				return pathPoints[i - 1].Lerp(pathPoints[i], t);
			}
		}

		return pathPoints[pathPoints.Length - 1];
	}

	// ---------------------------------------------------------------- drawing

	public override void _Draw()
	{
		if (gate == null || !GodotObject.IsInstanceValid(gate))
			return;

		EnsurePlayer();

		if (player == null)
			return;

		Vector2 target = GatePosition();
		Vector2 from = player.GlobalPosition;
		Vector2 toTarget = target - from;
		float distance = toTarget.Length();

		float t = (float)Time.GetTicksMsec() / 1000f;
		float pulse = 0.5f + 0.5f * Mathf.Sin(t * 4f);

		// ring and label on the gate
		DrawArc(target, 62f + 6f * pulse, 0f, Mathf.Tau, 48, new Color(Green, 0.55f + 0.35f * pulse), 3f, true);
		DrawString(font, new Vector2(target.X - 70f, target.Y - 84f), "GO OUT", HorizontalAlignment.Center, 140f, 16, new Color(Green, 0.9f));

		if (DebugDrawGrid && gridReady)
		{
			for (int i = 0; i < blocked.Length; i++)
			{
				if (blocked[i])
					DrawRect(new Rect2(CellCenter(i % gridW, i / gridW) - new Vector2(CellSize, CellSize) / 2f, new Vector2(CellSize, CellSize)), new Color(1f, 0.2f, 0.2f, 0.35f));
			}
		}

		if (distance < 110f)
			return;

		if (UsePathfinding && !gridFailed)
		{
			if (!gridReady)
				return;   // still building the walkable grid (a fraction of a second)

			if (pathPoints != null && pathPoints.Length >= 2)
			{
				DrawPathGuide(target, t);
				return;
			}
		}

		DrawStraightGuide(from, toTarget, distance, t);
	}

	private void DrawPathGuide(Vector2 target, float t)
	{
		const float stopShort = 70f;   // stop the arrow this far before the gate centre
		float total = pathCum[pathCum.Length - 1];

		// find where the path first gets within stopShort of the gate
		float end = total;

		for (int i = 1; i < pathPoints.Length; i++)
		{
			if (pathPoints[i].DistanceTo(target) > stopShort)
				continue;

			if (pathPoints[i - 1].DistanceTo(target) <= stopShort)
			{
				end = pathCum[i - 1];
			}
			else
			{
				float lo = 0f;
				float hi = 1f;

				for (int k = 0; k < 12; k++)
				{
					float mid = (lo + hi) / 2f;

					if (pathPoints[i - 1].Lerp(pathPoints[i], mid).DistanceTo(target) > stopShort)
						lo = mid;
					else
						hi = mid;
				}

				end = pathCum[i - 1] + (pathCum[i] - pathCum[i - 1]) * lo;
			}

			break;
		}

		const float start = 34f;

		if (end - start < 20f)
			return;

		const float dash = 14f;
		const float gap = 10f;
		const float period = dash + gap;
		float phase = (t * 36f) % period;
		Color lineColor = new Color(Green, 0.85f);

		for (float d = start - phase; d < end; d += period)
		{
			float a = Mathf.Max(d, start);
			float b = Mathf.Min(d + dash, end);

			if (b <= a)
				continue;

			List<Vector2> pts = new List<Vector2>();
			pts.Add(PointAlongPath(a));

			for (int i = 1; i < pathPoints.Length - 1; i++)
			{
				if (pathCum[i] > a && pathCum[i] < b)
					pts.Add(pathPoints[i]);
			}

			pts.Add(PointAlongPath(b));
			DrawPolyline(pts.ToArray(), lineColor, 3f, true);
		}

		// arrow head, pointing along the last bit of the path
		Vector2 tip = PointAlongPath(end);
		Vector2 back = PointAlongPath(Mathf.Max(0f, end - 14f));
		Vector2 dir = (tip - back).Normalized();

		if (dir == Vector2.Zero)
			return;

		Vector2 side = new Vector2(-dir.Y, dir.X);
		DrawLine(tip, tip - dir * 16f + side * 10f, lineColor, 3f, true);
		DrawLine(tip, tip - dir * 16f - side * 10f, lineColor, 3f, true);
	}

	// The original straight line (used if no path can be found).
	private void DrawStraightGuide(Vector2 from, Vector2 toTarget, float distance, float t)
	{
		Vector2 dir = toTarget / distance;
		float start = 34f;
		float end = distance - 70f;

		if (end <= start)
			return;

		const float dash = 14f;
		const float gap = 10f;
		const float period = dash + gap;
		float phase = (t * 36f) % period;
		Color lineColor = new Color(Green, 0.85f);

		for (float d = start - phase; d < end; d += period)
		{
			float a = Mathf.Max(d, start);
			float b = Mathf.Min(d + dash, end);

			if (b > a)
				DrawLine(from + dir * a, from + dir * b, lineColor, 3f, true);
		}

		// arrow head
		Vector2 tip = from + dir * end;
		Vector2 side = new Vector2(-dir.Y, dir.X);
		DrawLine(tip, tip - dir * 16f + side * 10f, lineColor, 3f, true);
		DrawLine(tip, tip - dir * 16f - side * 10f, lineColor, 3f, true);
	}
}
