using Godot;
using System;

/// <summary>
/// FlowTool 画布节点卡片组件，负责节点样式与内容展示。
/// </summary>
[Tool]
[GlobalClass]
public partial class FlowToolCanvasNodeCard : PanelContainer
{
	/// <summary>
	/// 预览用指标名。
	/// </summary>
	[Export]
	public string PreviewMetricName { get; set; } = "人口增长率";

	/// <summary>
	/// 预览用中文名。
	/// </summary>
	[Export]
	public string PreviewDisplayName { get; set; } = "人口增长率";

	/// <summary>
	/// 预览用类型名。
	/// </summary>
	[Export]
	public string PreviewTypeDisplayName { get; set; } = "System.Single";

	// 节点基础背景色。
	private static readonly Color defaultNodeBackgroundColor = new(0.12f, 0.16f, 0.2f);
	// 节点选中背景色。
	private static readonly Color selectedNodeBackgroundColor = new(0.16f, 0.24f, 0.3f);
	// 节点边框色。
	private static readonly Color nodeBorderColor = new(0.39f, 0.69f, 0.92f);

	// 当前节点 ID。
	private string nodeId = string.Empty;
	// 删除按钮引用。
	private Button deleteButton = null!;
	// 标题文本引用。
	private Label titleLabel = null!;
	// 详情文本引用。
	private Label detailLabel = null!;
	// 删除节点回调。
	private Action<string> deleteNodeRequested = static _ => { };

	/// <summary>
	/// 组件初始化并绑定内部节点。
	/// </summary>
	public override void _Ready()
	{
		ensureNodeReferences();
		AddThemeStyleboxOverride("panel", createCardStyle(isSelected: false));

		if (Engine.IsEditorHint() && string.IsNullOrWhiteSpace(nodeId))
		{
			refreshPreviewContent();
		}
	}

	/// <summary>
	/// 配置节点显示内容与删除行为。
	/// </summary>
	public void Configure(FlowToolMetricNode metricNode, Action<string> onDeleteNodeRequested)
	{
		ensureNodeReferences();
		nodeId = metricNode.NodeId;
		deleteNodeRequested = onDeleteNodeRequested;
		titleLabel.Text = metricNode.MetricName;
		detailLabel.Text = createNodeDetailText(metricNode);
	}

	/// <summary>
	/// 设置删除按钮显示状态。
	/// </summary>
	public void SetDeleteVisible(bool isVisible)
	{
		deleteButton.Visible = isVisible;
	}

	/// <summary>
	/// 设置卡片选中样式。
	/// </summary>
	public void SetSelected(bool isSelected)
	{
		AddThemeStyleboxOverride("panel", createCardStyle(isSelected));
	}

	// 处理删除按钮点击。
	private void onDeletePressed()
	{
		deleteNodeRequested(nodeId);
	}

	// 绑定节点引用并确保删除事件只注册一次。
	private void ensureNodeReferences()
	{
		if (deleteButton != null)
		{
			return;
		}

		deleteButton = GetNode<Button>("Body/ActionRow/DeleteButton");
		titleLabel = GetNode<Label>("Body/TitleLabel");
		detailLabel = GetNode<Label>("Body/DetailLabel");
		deleteButton.Pressed += onDeletePressed;
	}

	// 使用导出参数刷新编辑器预览内容。
	private void refreshPreviewContent()
	{
		titleLabel.Text = PreviewMetricName;
		detailLabel.Text = createDetailText(PreviewTypeDisplayName, PreviewDisplayName, PreviewMetricName);
	}

	// 创建节点详情文本。
	private static string createNodeDetailText(FlowToolMetricNode metricNode)
	{
		return createDetailText(metricNode.TypeDisplayName, metricNode.DisplayName, metricNode.MetricName);
	}

	// 创建节点详情文本主体。
	private static string createDetailText(string typeDisplayName, string displayName, string metricName)
	{
		string optionalDisplayLine = string.Equals(displayName, metricName, StringComparison.Ordinal)
			? string.Empty
			: $"\n中文名: {displayName}";
		return $"类型: {typeDisplayName}{optionalDisplayLine}";
	}

	// 创建节点卡片样式。
	private static StyleBoxFlat createCardStyle(bool isSelected)
	{
		return new StyleBoxFlat
		{
			BgColor = isSelected ? selectedNodeBackgroundColor : defaultNodeBackgroundColor,
			BorderColor = nodeBorderColor,
			BorderWidthBottom = 2,
			BorderWidthTop = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8
		};
	}
}
