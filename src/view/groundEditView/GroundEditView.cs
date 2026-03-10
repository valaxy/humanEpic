using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 地图编辑器状态对象。
/// 用于统一管理编辑模式状态、笔刷预览与连绘状态。
/// </summary>
[GlobalClass]
public partial class GroundEditView : Node
{
	// 地图交互入口引用。
	private GroundView groundView = null!;

	// 地图笔刷引用。
	private Brush brush = null!;

	// 地图地面模型引用。
	private Ground ground = null!;
	// 建筑集合引用。
	private BuildingCollection buildingCollection = null!;

	// 预览节点容器。
	private Node3D previewRoot = null!;

	// 预览节点池。
	private readonly List<MeshInstance3D> previewCells = new();

	// 当前预览模式。
	private PreviewMode previewMode = PreviewMode.Default;

	// 最近一次连续绘制经过的地格。
	private Vector2I lastDrawnCell = new(-1, -1);

	/// <summary>
	/// 编辑器请求在指定格点执行一次绘制。
	/// </summary>
	[Signal]
	public delegate void EditCellRequestedEventHandler(Vector2I cellPos);

	/// <summary>
	/// 一次连续绘制完成时发出。
	/// </summary>
	[Signal]
	public delegate void DrawCompletedEventHandler();

	/// <summary>
	/// 当前选中的地表类型。
	/// </summary>
	public SurfaceType.Enums SurfaceType { get; private set; } = global::SurfaceType.Enums.GRASSLAND;

	/// <summary>
	/// 当前选中的覆盖物类型。
	/// </summary>
	public OverlayType.Enums OverlayType { get; private set; } = global::OverlayType.Enums.NONE;

	/// <summary>
	/// 当前选中的建筑类型。
	/// </summary>
	public BuildingType.Enums BuildingType { get; private set; } = global::BuildingType.Enums.Residential;

	/// <summary>
	/// 当前是否可进行绘制。
	/// </summary>
	public bool CanDraw => previewMode != PreviewMode.Default;

	/// <summary>
	/// 获取笔刷节点。
	/// </summary>
	public Brush GetBrush()
	{
		return brush;
	}

	/// <summary>
	/// 当前是否处于连续绘制中。
	/// </summary>
	public bool IsDrawing { get; private set; }

	/// <summary>
	/// 初始化运行期节点引用。
	/// </summary>
	public override void _Ready()
	{
		brush = GetNode<Brush>("../Brush");
		brush.Visible = false;
		previewRoot = GetNode<Node3D>("../BrushPreviewRoot");
	}

	/// <summary>
	/// 初始化编辑器依赖并绑定地面输入事件。
	/// </summary>
	public void Setup(Ground groundRef, BuildingCollection buildingCollectionRef, GroundView groundViewRef)
	{
		ground = groundRef;
		buildingCollection = buildingCollectionRef;

		if (groundView != null)
		{
			groundView.EditPointerCellChanged -= onEditPointerCellChanged;
			groundView.EditPointerCellCleared -= onEditPointerCellCleared;
			groundView.EditPrimaryPressed -= onEditPrimaryPressed;
			groundView.EditPrimaryReleased -= onEditPrimaryReleased;
		}

		groundView = groundViewRef;
		groundView.EditPointerCellChanged += onEditPointerCellChanged;
		groundView.EditPointerCellCleared += onEditPointerCellCleared;
		groundView.EditPrimaryPressed += onEditPrimaryPressed;
		groundView.EditPrimaryReleased += onEditPrimaryReleased;

		previewMode = PreviewMode.Default;
		IsDrawing = false;
		lastDrawnCell = new Vector2I(-1, -1);
		clearPreview();
	}

	/// <summary>
	/// 设置覆盖物编辑模式。
	/// </summary>
	public void SetOverlayMode(bool enabled, OverlayType.Enums overlayType)
	{
		previewMode = enabled ? PreviewMode.EditOverlay : PreviewMode.Default;
		OverlayType = overlayType;
		if (!enabled)
		{
			clearPreview();
		}
	}

	/// <summary>
	/// 设置地表编辑模式。
	/// </summary>
	public void SetSurfaceMode(bool enabled, SurfaceType.Enums surfaceType)
	{
		previewMode = enabled ? PreviewMode.EditSurface : PreviewMode.Default;
		SurfaceType = surfaceType;
		if (!enabled)
		{
			clearPreview();
		}
	}

	/// <summary>
	/// 设置建筑编辑模式。
	/// </summary>
	public void SetBuildingMode(bool enabled, BuildingType.Enums buildingType)
	{
		previewMode = enabled ? PreviewMode.EditBuilding : PreviewMode.Default;
		BuildingType = buildingType;
		if (!enabled)
		{
			clearPreview();
		}
	}

	/// <summary>
	/// 关闭编辑模式并清理视觉。
	/// </summary>
	public void DisableEditMode()
	{
		previewMode = PreviewMode.Default;
		endDrawInternal();
		brush.Visible = false;
		clearPreview();
	}

	// 处理地面指针变化。
	private void onEditPointerCellChanged(Vector2I cellPos)
	{
		if (!CanDraw || !ground.IsInsideGround(cellPos))
		{
			brush.Visible = false;
			HidePreviewAndForbidden();
			return;
		}

		brush.Visible = true;
		updateCursorVisual(cellPos);
		updatePreview(cellPos);

		if (tryConsumeDrawCell(cellPos))
		{
			EmitSignal(SignalName.EditCellRequested, cellPos);
		}
	}

	// 处理地面指针清理。
	private void onEditPointerCellCleared()
	{
		brush.Visible = false;
		HidePreviewAndForbidden();
	}

	// 处理地面主指针按下。
	private void onEditPrimaryPressed(Vector2I cellPos)
	{
		if (!CanDraw || !ground.IsInsideGround(cellPos))
		{
			return;
		}

		IsDrawing = true;
		lastDrawnCell = cellPos;
		EmitSignal(SignalName.EditCellRequested, cellPos);
	}

	// 处理地面主指针抬起。
	private void onEditPrimaryReleased()
	{
		if (!endDrawInternal())
		{
			return;
		}

		EmitSignal(SignalName.DrawCompleted);
	}

	// 更新笔刷光标视觉位置。
	private void updateCursorVisual(Vector2I cellPos)
	{
		Vector3 worldPos = ground.GridToWorld(cellPos, YConfig.CursorY + 0.02f);
		float visualOffset = brush.Size % 2 == 0 ? 0.5f : 0.0f;
		brush.GlobalPosition = worldPos + new Vector3(visualOffset, 0, visualOffset);
	}

	// 更新刷子覆盖区预览与非法提示。
	private void updatePreview(Vector2I centerCell)
	{
		if (previewMode == PreviewMode.Default)
		{
			HidePreviewAndForbidden();
			return;
		}

		Vector2I[] affectedCells = brush.GetAffectedCells(centerCell.X, centerCell.Y);
		List<Vector2I> insideCells = affectedCells.Where(ground.IsInsideGround).ToList();
		bool hasOutOfBounds = affectedCells.Any(cell => !ground.IsInsideGround(cell));
		bool hasInvalidOverlay = previewMode == PreviewMode.EditOverlay && insideCells.Any(cell => !OverlayTemplate.IsValid(ground.GetGrid(cell.X, cell.Y).SurfaceType, OverlayType));
		bool hasInvalidBuilding = previewMode == PreviewMode.EditBuilding && insideCells.Any(buildingCollection.HasKeyByPos); // TODO 为什么这里要检查方法？

		brush.SetForbiddenIcon(hasOutOfBounds || hasInvalidOverlay || hasInvalidBuilding);
		renderPreviewCells(insideCells, getPreviewColor());
	}

	// 在连绘期间消费一个格点。
	private bool tryConsumeDrawCell(Vector2I cellPos)
	{
		if (!IsDrawing || cellPos == lastDrawnCell)
		{
			return false;
		}

		lastDrawnCell = cellPos;
		return true;
	}

	// 结束绘制状态并返回结束前状态。
	private bool endDrawInternal()
	{
		bool wasDrawing = IsDrawing;
		IsDrawing = false;
		lastDrawnCell = new Vector2I(-1, -1);
		return wasDrawing;
	}

	/// <summary>
	/// 隐藏预览并关闭非法提示图标。
	/// </summary>
	public void HidePreviewAndForbidden()
	{
		clearPreview();
		brush.SetForbiddenIcon(false);
	}





	// 获取当前模式对应的预览颜色。
	private Color getPreviewColor()
	{
		Color baseColor = previewMode switch
		{
			PreviewMode.EditSurface => SurfaceTemplate.GetTemplate(SurfaceType).Color,
			PreviewMode.EditOverlay => OverlayTemplate.GetTemplate(OverlayType).Color,
			PreviewMode.EditBuilding => BuildingTemplate.GetTemplate(BuildingType).Color,
			_ => Colors.White,
		};

		return new Color(baseColor.R, baseColor.G, baseColor.B, 0.45f);
	}

	// 渲染预览格子。
	private void renderPreviewCells(List<Vector2I> cells, Color color)
	{
		if (cells.Count == 0)
		{
			clearPreview();
			return;
		}

		ensurePreviewCellPool(cells.Count);

		cells.Select((cell, index) => (cell, index)).ToList().ForEach(item =>
		{
			MeshInstance3D previewCell = previewCells[item.index];
			previewCell.Visible = true;
			previewCell.GlobalPosition = ground.GridToWorld(item.cell, YConfig.CursorY + 0.01f);
			((StandardMaterial3D)previewCell.MaterialOverride).AlbedoColor = color;
		});

		previewCells.Skip(cells.Count).ToList().ForEach(node => node.Visible = false);
	}

	// 确保预览池容量。
	private void ensurePreviewCellPool(int targetCount)
	{
		int missingCount = targetCount - previewCells.Count;
		if (missingCount <= 0)
		{
			return;
		}

		Enumerable.Range(0, missingCount).ToList().ForEach(_ =>
		{
			MeshInstance3D previewCell = createPreviewCellNode();
			previewCells.Add(previewCell);
			previewRoot.AddChild(previewCell);
		});
	}

	// 创建单个预览格子节点。
	private MeshInstance3D createPreviewCellNode()
	{
		MeshInstance3D previewCell = new MeshInstance3D();
		previewCell.Mesh = new BoxMesh { Size = new Vector3(0.9f, 0.05f, 0.9f) };

		StandardMaterial3D material = new StandardMaterial3D();
		material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		material.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Always;
		material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		previewCell.MaterialOverride = material;
		previewCell.Visible = false;

		return previewCell;
	}

	// 清理预览显示。
	private void clearPreview()
	{
		previewCells.ForEach(node => node.Visible = false);
		brush.SetForbiddenIcon(false);
	}

}
