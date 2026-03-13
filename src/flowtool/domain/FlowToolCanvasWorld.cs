using Godot;

/// <summary>
/// FlowTool 画布领域实体，负责管理画布尺寸与节点坐标约束。
/// </summary>
public sealed class FlowToolCanvasWorld
{
	/// <summary>
	/// 虚拟画布宽度。
	/// </summary>
	public float Width { get; }

	/// <summary>
	/// 虚拟画布高度。
	/// </summary>
	public float Height { get; }

	// 节点宽度。
	private readonly float nodeWidth;
	// 节点高度。
	private readonly float nodeHeight;

	/// <summary>
	/// 创建画布领域对象。
	/// </summary>
	public FlowToolCanvasWorld(float width, float height, float inputNodeWidth, float inputNodeHeight)
	{
		Width = width;
		Height = height;
		nodeWidth = inputNodeWidth;
		nodeHeight = inputNodeHeight;
	}

	/// <summary>
	/// 约束单个节点坐标到画布边界。
	/// </summary>
	public Vector2 ClampNodePosition(Vector2 position)
	{
		float safeX = Mathf.Clamp(position.X, 0f, Mathf.Max(Width - nodeWidth, 0f));
		float safeY = Mathf.Clamp(position.Y, 0f, Mathf.Max(Height - nodeHeight, 0f));
		return new Vector2(Mathf.Round(safeX), Mathf.Round(safeY));
	}

	/// <summary>
	/// 以增量平移后再执行边界约束。
	/// </summary>
	public Vector2 TranslateWithConstraint(Vector2 currentPosition, Vector2 delta)
	{
		return ClampNodePosition(currentPosition + delta);
	}
}
