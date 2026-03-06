using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 覆盖物编辑器 UI。
/// 提供覆盖物类型选择与笔刷放置能力。
/// </summary>
[GlobalClass]
public partial class OverlayEditor : EditorWindow
{
	// 类型按钮场景。
	private static readonly PackedScene TypeButtonScene = GD.Load<PackedScene>("res://src/ui/editor/type_button.tscn");

	// 当前选中的覆盖物类型。
	private OverlayType.Enums currentOverlayType = OverlayType.Enums.NONE;

	// 覆盖物类型到按钮实例的映射。
	private readonly Dictionary<OverlayType.Enums, Button> overlayButtons = new();

	// 覆盖物按钮行容器。
	private HBoxContainer overlayRow = null!;

	// 笔刷控制器组件。
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
		overlayRow = GetNode<HBoxContainer>("%OverlayRow");
		brushController = GetNode<BrushController>("%BrushController");
		GetNode<Label>("%TitleLabel").Text = "覆盖物编辑";

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
		brushController.Setup(brush);

		groundEditor.EditCellRequested -= onGroundCellClicked;
		groundEditor.EditCellRequested += onGroundCellClicked;

		triggerSelection();
		triggerBrushSize(brush.Size);
	}

	/// <summary>
	/// 设置编辑器显隐状态。
	/// </summary>
	public void SetEditorVisible(bool visible)
	{
		Visible = visible;
		groundEditor.SetOverlayMode(visible, currentOverlayType);
	}

	// 动态生成覆盖物类型按钮。
	private void setupButtons()
	{
		overlayButtons.Clear();
		overlayRow.GetChildren().Cast<Node>().ToList().ForEach(child => child.QueueFree());

		Dictionary<OverlayType.Enums, OverlayTemplate> templates = OverlayTemplate.GetTemplates();
		templates.ToList().ForEach(item =>
		{
			Button button = createTypeButton(item.Value.Name, item.Value.Color, item.Key);
			overlayRow.AddChild(button);
			overlayButtons[item.Key] = button;
		});

		updateSelectionVisuals();
	}

	// 同步笔刷大小到视图。
	private void triggerBrushSize(int size)
	{
		if (HasNode("../RightBottomContainer"))
		{
			Node rightBottomContainer = GetNode("../RightBottomContainer");
			rightBottomContainer.SetMeta("OverlayBrushSize", size);
		}
	}

	// 创建类型按钮。
	private Button createTypeButton(string text, Color color, OverlayType.Enums overlayType)
	{
		Button button = TypeButtonScene.Instantiate<Button>();
		button.GetNode<Label>("%Label").Text = text;
		button.GetNode<ColorRect>("%ColorRect").Color = color;
		button.Pressed += () => onOverlaySelected(overlayType);
		return button;
	}

	// 刷新按钮选中视觉。
	private void updateSelectionVisuals()
	{
		overlayButtons.Keys.ToList().ForEach(type =>
		{
			Button button = overlayButtons[type];
			button.GetNode<ReferenceRect>("%SelectionBorder").Visible = type == currentOverlayType;
		});
	}

	// 响应覆盖物类型选择。
	private void onOverlaySelected(OverlayType.Enums overlayType)
	{
		currentOverlayType = overlayType;
		updateSelectionVisuals();
		triggerSelection();
		groundEditor.SetOverlayMode(Visible, currentOverlayType);
	}

	// 同步当前选中的覆盖物类型。
	private void triggerSelection()
	{
		SetMeta("SelectedOverlay", (int)currentOverlayType);
	}

	// 响应地图点击并执行覆盖物放置。
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

		List<Vector2I> validCells = cells
			.Where(pos => OverlayTemplate.IsValid(ground.GetGrid(pos.X, pos.Y).SurfaceType, currentOverlayType))
			.ToList();

		validCells.ForEach(pos =>
		{
			Grid grid = ground.GetGrid(pos.X, pos.Y);
			grid.UpdateOverlay(currentOverlayType);
		});

		Godot.Collections.Array<Vector2I> changedCells = new Godot.Collections.Array<Vector2I>(validCells.ToArray());
		layerManager.UpdateCells(changedCells, ground);
	}
}
