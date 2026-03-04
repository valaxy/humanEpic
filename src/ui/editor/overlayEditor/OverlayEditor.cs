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

	// 笔刷设置组件。
	private BrushForm brushForm = null!;

	// 编辑器状态对象。
	private GroundEditor groundEditor = null!;

	// 地形模型。
	private Ground ground = null!;

	// 地图层级管理器。
	private LayerManagerNode layerManager = null!;

	// 地图交互入口。
	private GroundView groundView = null!;

	public override void _Ready()
	{
		base._Ready();
		overlayRow = GetNode<HBoxContainer>("%OverlayRow");
		brushForm = GetNode<BrushForm>("%BrushForm");
		groundEditor = new GroundEditor();
		AddChild(groundEditor);
		brushForm.Setup(groundEditor);
		GetNode<Label>("%TitleLabel").Text = "覆盖物编辑";

		setupButtons();
		setupSignals();
	}

	/// <summary>
	/// 初始化编辑器依赖并绑定地图点击事件。
	/// </summary>
	public void Setup(Ground groundRef, LayerManagerNode layerManagerRef, GroundView groundViewRef)
	{
		ground = groundRef;
		layerManager = layerManagerRef;
		groundView = groundViewRef;

		groundView.CellClicked -= onGroundCellClicked;
		groundView.CellClicked += onGroundCellClicked;

		triggerSelection();
		triggerBrushSize(groundEditor.BrushSize);
	}

	/// <summary>
	/// 设置编辑器显隐状态。
	/// </summary>
	public void SetEditorVisible(bool visible)
	{
		Visible = visible;
	}

	// 连接并处理子控件信号。
	private void setupSignals()
	{
		groundEditor.BrushSizeChanged += onGroundEditorBrushSizeChanged;
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

	// 响应编辑器笔刷大小变化。
	private void onGroundEditorBrushSizeChanged(int value)
	{
		triggerBrushSize(value);
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

		int brushSize = getBrushSize();
		int startX = cellPos.X - (brushSize / 2);
		int startY = cellPos.Y - (brushSize / 2);

		List<Vector2I> cells = Enumerable
			.Range(0, brushSize)
			.SelectMany(offsetX => Enumerable.Range(0, brushSize).Select(offsetY => new Vector2I(startX + offsetX, startY + offsetY)))
			.Where(ground.IsInsideGround)
			.ToList();

		cells.ForEach(pos =>
		{
			Grid grid = ground.GetGrid(pos.X, pos.Y);
			if (OverlayTemplate.IsValid(grid.SurfaceType, currentOverlayType))
			{
				grid.UpdateOverlay(currentOverlayType);
			}
		});

		Godot.Collections.Array<Vector2I> changedCells = new Godot.Collections.Array<Vector2I>(cells.ToArray());
		layerManager.UpdateCells(changedCells, ground);
	}

	// 获取当前笔刷大小。
	private int getBrushSize()
	{
		return groundEditor.BrushSize;
	}
}
