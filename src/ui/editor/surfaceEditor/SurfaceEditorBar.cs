using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 地表编辑器 UI。
/// 提供地表类型选择、地图尺寸修改与笔刷放置能力。
/// </summary>
[GlobalClass]
public partial class SurfaceEditorBar : EditorWindow
{
	// 类型按钮场景。
	private static readonly PackedScene TypeButtonScene = GD.Load<PackedScene>("res://src/ui/editor/type_button.tscn");

	// 当前选中的地表类型。
	private SurfaceType.Enums currentSurfaceType = SurfaceType.Enums.GRASSLAND;

	// 地表类型到按钮实例的映射。
	private readonly Dictionary<SurfaceType.Enums, Button> surfaceButtons = new();

	// 地表按钮容器。
	private HBoxContainer terrainRow = null!;

	// 地图尺寸控制器。
	private MapResizeController mapResizeController = null!;

	// 笔刷控制器。
	private BrushController brushController = null!;

	// 笔刷对象。
	private Brush brush = null!;

	// 地形模型。
	private Ground ground = null!;

	// 地图层级管理器。
	private LayerManagerNode layerManager = null!;

	// 地图编辑器。
	private GroundEditView groundEditor = null!;

	public override void _Ready()
	{
		base._Ready();
		terrainRow = GetNode<HBoxContainer>("%TerrainRow");
		mapResizeController = GetNode<MapResizeController>("%MapResizeController");
		brushController = GetNode<BrushController>("%BrushController");
		GetNode<Label>("%TitleLabel").Text = "地表编辑";

		setupButtons();
	}

	/// <summary>
	/// 初始化编辑器依赖并绑定地图点击事件。
	/// </summary>
	public void Setup(Ground groundRef, LayerManagerNode layerManagerRef, GroundEditView groundEditorRef, Brush brushRef)
	{
		ground = groundRef;
		layerManager = layerManagerRef;
		groundEditor = groundEditorRef;
		brush = brushRef;

		mapResizeController.Setup(ground);
		brushController.Setup(brush);

		groundEditor.EditCellRequested -= onGroundCellClicked;
		groundEditor.EditCellRequested += onGroundCellClicked;

		triggerSelection();
	}

	/// <summary>
	/// 设置编辑器显隐状态。
	/// </summary>
	public void SetEditorVisible(bool visible)
	{
		Visible = visible;
		groundEditor.SetSurfaceMode(visible, currentSurfaceType);
	}

	// 动态生成地表类型按钮。
	private void setupButtons()
	{
		surfaceButtons.Clear();
		terrainRow.GetChildren().Cast<Node>().ToList().ForEach(child => child.QueueFree());

		Dictionary<SurfaceType.Enums, SurfaceTemplate> templates = SurfaceTemplate.GetTemplates();
		templates.ToList().ForEach(item =>
		{
			Button button = createTypeButton(item.Value.Name, item.Value.Color, item.Key);
			terrainRow.AddChild(button);
			surfaceButtons[item.Key] = button;
		});

		updateSelectionVisuals();
	}

	// 创建类型按钮。
	private Button createTypeButton(string text, Color color, SurfaceType.Enums surfaceType)
	{
		Button button = TypeButtonScene.Instantiate<Button>();
		button.GetNode<Label>("%Label").Text = text;
		button.GetNode<ColorRect>("%ColorRect").Color = color;
		button.Pressed += () => onSurfaceSelected(surfaceType);
		return button;
	}

	// 刷新按钮选中视觉。
	private void updateSelectionVisuals()
	{
		surfaceButtons.Keys.ToList().ForEach(type =>
		{
			Button button = surfaceButtons[type];
			button.GetNode<ReferenceRect>("%SelectionBorder").Visible = type == currentSurfaceType;
		});
	}

	// 响应地表类型选择。
	private void onSurfaceSelected(SurfaceType.Enums surfaceType)
	{
		currentSurfaceType = surfaceType;
		updateSelectionVisuals();
		triggerSelection();
		groundEditor.SetSurfaceMode(Visible, currentSurfaceType);
	}

	// 同步当前选中的地表类型。
	private void triggerSelection()
	{
		SetMeta("SelectedSurface", (int)currentSurfaceType);
	}

	// 响应地图点击并执行地表放置。
	private void onGroundCellClicked(Vector2I cellPos)
	{
		if (!Visible)
		{
			return;
		}

		List<Vector2I> cells = brush
			.GetAffectedCells(cellPos.X, cellPos.Y)
			.Where(ground.IsInsideGround)
			.ToList();

		cells.ForEach(pos =>
		{
			Grid grid = ground.GetGrid(pos.X, pos.Y);
			grid.UpdateSurface(currentSurfaceType);
		});

		Godot.Collections.Array<Vector2I> changedCells = new Godot.Collections.Array<Vector2I>(cells.ToArray());
		layerManager.UpdateCells(changedCells, ground);
	}
}
