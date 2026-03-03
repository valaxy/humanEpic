using Godot;

/// <summary>
/// 处理地面交互逻辑（射线检测、坐标转换、输入分发）
/// </summary>
[GlobalClass]
public partial class GroundInteractionNode : Node
{
	private GroundNode groundNode = null!;

	/// <summary>交互是否处于活动状态</summary>
	public bool IsActive { get; set; } = false;

	/// <summary>是否允许绘制</summary>
	public bool CanDraw { get; set; } = false;

	/// <summary>是否正在点击/拖拽中</summary>
	public bool IsDrawing { get; set; } = false;

	private Vector2I lastHoveredCell = new(-1, -1);
	private bool hasDrawnInCurrentStroke = false;

	/// <summary>
	/// 初始化
	/// </summary>
	public void Setup(GroundNode groundNode)
	{
		this.groundNode = groundNode;
	}

	public override void _Process(double delta)
	{
		UpdateInteraction();
	}

	/// <summary>
	/// 处理输入事件
	/// </summary>
	public override void _Input(InputEvent @event)
	{
		if (!IsActive) return;

		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				if (mouseButton.Pressed)
				{
					if (GetViewport().GuiGetHoveredControl() != null) return;

					Vector3? intersection = GetRayIntersection();
					if (intersection.HasValue)
					{
						Vector2I cellPos = WorldToGrid(intersection.Value);
						if (IsWithinBounds(cellPos))
						{
							groundNode.SelectionNode?.ProcessSelection(cellPos);

							bool isValid = groundNode.EditorNode.IsCellSelectionValid(cellPos);
							if (CanDraw && isValid)
							{
								IsDrawing = true;
								groundNode.EditorNode.DrawAt(cellPos);
							}
						}
						else
						{
							groundNode.SelectionNode?.ClearSelection();
						}
					}
					else
					{
						groundNode.SelectionNode?.ClearSelection();
					}
				}
				else
				{
					if (IsDrawing && hasDrawnInCurrentStroke)
					{
						groundNode.EmitMapDrawCompleted();
					}
					IsDrawing = false;
					hasDrawnInCurrentStroke = false;
					lastHoveredCell = new Vector2I(-1, -1);
				}
			}
		}
	}

	/// <summary>
	/// 更新每帧交互逻辑
	/// </summary>
	public void UpdateInteraction()
	{
		if (!IsActive || GetViewport().GuiGetHoveredControl() != null)
		{
			groundNode.SetCursorVisible(false);
			IsDrawing = false;
			return;
		}

		Vector3? intersection = GetRayIntersection();
		if (!intersection.HasValue)
		{
			groundNode.SetCursorVisible(false);
			IsDrawing = false;
			return;
		}

		Vector2I cellPos = WorldToGrid(intersection.Value);
		if (IsWithinBounds(cellPos) && CanDraw)
		{
			groundNode.SetCursorVisible(true);
			UpdateCursorVisual(cellPos);

			bool isValid = groundNode.EditorNode.IsCellSelectionValid(cellPos);
			groundNode.SetForbiddenVisible(!isValid);

			if (IsDrawing && isValid)
			{
				if (lastHoveredCell != cellPos)
				{
					lastHoveredCell = cellPos;
					hasDrawnInCurrentStroke = true;
					groundNode.EditorNode.DrawAt(cellPos);
				}
			}
		}
		else
		{
			groundNode.SetCursorVisible(false);
		}
	}

	/// <summary>
	/// 获取射线与地面高度平面的交点
	/// </summary>
	public Vector3? GetRayIntersection()
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
		return groundNode.Camera.ProjectToPlane(mousePos);
	}

	/// <summary>
	/// 将世界坐标转换为地格坐标
	/// </summary>
	public Vector2I WorldToGrid(Vector3 worldPos)
	{
		if (groundNode.World?.Ground == null) return Vector2I.Zero;
		Vector2 pos = groundNode.World.Ground.WorldToGrid(worldPos);
		return new Vector2I(Mathf.RoundToInt(pos.X), Mathf.RoundToInt(pos.Y));
	}

	/// <summary>
	/// 检查坐标是否在地图范围内
	/// </summary>
	public bool IsWithinBounds(Vector2I cellPos)
	{
		if (groundNode.World?.Ground == null) return false;
		return (cellPos.X >= 0 && cellPos.X < groundNode.World.Ground.Width && 
				cellPos.Y >= 0 && cellPos.Y < groundNode.World.Ground.Height);
	}

	private void UpdateCursorVisual(Vector2I cellPos)
	{
		if (groundNode.BrushCursor == null) return;
		Vector3 worldPos = groundNode.GridToWorld(cellPos);
		float visualOffset = groundNode.BrushCursor.Size % 2 == 0 ? 0.5f : 0.0f;
		groundNode.SetCursorPosition(worldPos + new Vector3(visualOffset, 0, visualOffset));
	}
}
