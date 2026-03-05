using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 地图编辑器状态对象。
/// 用于统一管理编辑模式状态、笔刷预览与连绘状态。
/// </summary>
[GlobalClass]
public partial class GroundEditor : Node
{
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
	/// 当前是否处于连续绘制中。
	/// </summary>
	public bool IsDrawing { get; private set; }

	/// <summary>
	/// 初始化编辑器依赖。
	/// </summary>
	public void Setup(Ground groundRef, BuildingCollection buildingCollectionRef, Brush brushRef, Node3D previewRootRef)
	{
		ground = groundRef;
		buildingCollection = buildingCollectionRef;
		brush = brushRef;
		previewRoot = previewRootRef;
		previewMode = PreviewMode.Default;
		IsDrawing = false;
		lastDrawnCell = new Vector2I(-1, -1);
		clearPreview();
		brush.SetForbiddenIcon(false);
	}

	/// <summary>
	/// 设置地表类型。
	/// </summary>
	public void SetSurfaceType(SurfaceType.Enums value)
	{
		SurfaceType = value;
	}

	/// <summary>
	/// 设置覆盖物类型。
	/// </summary>
	public void SetOverlayType(OverlayType.Enums value)
	{
		OverlayType = value;
	}

	/// <summary>
	/// 设置建筑类型。
	/// </summary>
	public void SetBuildingType(BuildingType.Enums value)
	{
		BuildingType = value;
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
			brush.SetForbiddenIcon(false);
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
			brush.SetForbiddenIcon(false);
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
			brush.SetForbiddenIcon(false);
		}
	}

	/// <summary>
	/// 关闭编辑模式并清理视觉。
	/// </summary>
	public void DisableEditMode()
	{
		previewMode = PreviewMode.Default;
		IsDrawing = false;
		lastDrawnCell = new Vector2I(-1, -1);
		brush.Visible = false;
		brush.SetForbiddenIcon(false);
		clearPreview();
	}

	/// <summary>
	/// 开始一次连续绘制。
	/// </summary>
	public void BeginDraw()
	{
		if (!CanDraw)
		{
			return;
		}

		IsDrawing = true;
		lastDrawnCell = new Vector2I(-1, -1);
	}

	/// <summary>
	/// 结束一次连续绘制。
	/// </summary>
	/// <returns>结束前是否处于绘制中。</returns>
	public bool EndDraw()
	{
		bool wasDrawing = IsDrawing;
		IsDrawing = false;
		lastDrawnCell = new Vector2I(-1, -1);
		return wasDrawing;
	}

	/// <summary>
	/// 判断指定格点是否可在当前模式下绘制。
	/// </summary>
	public bool IsDrawableCell(Vector2I cellPos)
	{
		return CanDraw && ground.IsInsideGround(cellPos);
	}

	/// <summary>
	/// 在连绘期间消费一个格点。
	/// </summary>
	/// <returns>是否应对该格点触发绘制。</returns>
	public bool TryConsumeDrawCell(Vector2I cellPos)
	{
		if (!IsDrawing || cellPos == lastDrawnCell)
		{
			return false;
		}

		lastDrawnCell = cellPos;
		return true;
	}

	/// <summary>
	/// 更新笔刷光标视觉位置。
	/// </summary>
	public void UpdateCursorVisual(Vector2I cellPos)
	{
		Vector3 worldPos = ground.GridToWorld(cellPos, YConfig.CursorY + 0.02f);
		float visualOffset = brush.Size % 2 == 0 ? 0.5f : 0.0f;
		brush.GlobalPosition = worldPos + new Vector3(visualOffset, 0, visualOffset);
	}

	/// <summary>
	/// 更新刷子覆盖区预览与非法提示。
	/// </summary>
	public void UpdatePreview(Vector2I centerCell)
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
		bool hasInvalidBuilding = previewMode == PreviewMode.EditBuilding && insideCells.Any(buildingCollection.HasKey);

		brush.SetForbiddenIcon(hasOutOfBounds || hasInvalidOverlay || hasInvalidBuilding);
		renderPreviewCells(insideCells, getPreviewColor());
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
	}

}
