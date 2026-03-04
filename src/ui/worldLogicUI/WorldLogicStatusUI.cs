using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 世界逻辑状态监视面板。
/// </summary>
[GlobalClass]
public partial class WorldLogicStatusUI : PanelContainer
{
	// 单行逻辑状态控件缓存。
	private sealed class LogicRow
	{
		// 逻辑引用。
		public IWorldLogic Logic { get; set; }

		// 进度条控件。
		public ProgressBar ProgressBar { get; set; }

		// 百分比文本控件。
		public Label PercentLabel { get; set; }

		public LogicRow(IWorldLogic logic, ProgressBar progressBar, Label percentLabel)
		{
			Logic = logic;
			ProgressBar = progressBar;
			PercentLabel = percentLabel;
		}
	}

	// 世界模拟引用。
	private Simulation? simulation;

	// 气泡消息容器。
	private BubbleMessageContainerUI bubbleMessageContainer = null!;

	// 行缓存列表。
	private readonly List<LogicRow> logicRows = new List<LogicRow>();

	// 条目容器。
	private VBoxContainer listContainer = null!;

	public override void _Ready()
	{
		listContainer = GetNode<VBoxContainer>("Margin/ListContainer");
	}

	/// <summary>
	/// 配置世界模拟引用并构建列表。
	/// </summary>
	public void Setup(Simulation simulationRef)
	{
		if (simulation != null)
		{
			simulation.LogicTriggered -= onLogicTriggered;
		}

		simulation = simulationRef;
		bubbleMessageContainer = (BubbleMessageContainerUI)GetTree().GetFirstNodeInGroup("bubble_message_container");
		simulation.LogicTriggered += onLogicTriggered;
		rebuildRows();
	}

	public override void _Process(double delta)
	{
		if (!Visible)
		{
			return;
		}

		updateProgress();
	}

	// 根据当前逻辑集合重建 UI 行。
	private void rebuildRows()
	{
		logicRows.Clear();
		listContainer.GetChildren().Cast<Node>().ToList().ForEach(child => child.QueueFree());

		if (simulation == null)
		{
			return;
		}

		simulation.GetWorldLogics().ToList().ForEach(addLogicRow);
		updateProgress();
	}

	// 为单个逻辑创建展示行。
	private void addLogicRow(IWorldLogic logic)
	{
		VBoxContainer row = new VBoxContainer();
		row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		listContainer.AddChild(row);

		Label titleLabel = new Label();
		titleLabel.Text = logic.Name;
		titleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		titleLabel.TooltipText = logic.Description;
		row.AddChild(titleLabel);

		HBoxContainer progressRow = new HBoxContainer();
		progressRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(progressRow);

		ProgressBar progressBar = new ProgressBar();
		progressBar.MinValue = 0.0;
		progressBar.MaxValue = 1.0;
		progressBar.Step = 0.001;
		progressBar.ShowPercentage = false;
		progressBar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		progressBar.TooltipText = logic.Description;
		progressRow.AddChild(progressBar);

		Label percentLabel = new Label();
		percentLabel.CustomMinimumSize = new Vector2(46.0f, 0.0f);
		percentLabel.HorizontalAlignment = HorizontalAlignment.Right;
		progressRow.AddChild(percentLabel);

		logicRows.Add(new LogicRow(logic, progressBar, percentLabel));
	}

	// 刷新所有逻辑的进度显示。
	private void updateProgress()
	{
		logicRows.ForEach(updateLogicRow);
	}

	// 刷新单行逻辑进度。
	private void updateLogicRow(LogicRow row)
	{
		float progressValue = row.Logic.GetProgressRatio();
		row.ProgressBar.Value = progressValue;
		row.PercentLabel.Text = $"{(int)(progressValue * 100.0f)}%";
	}

	// 响应世界逻辑触发并展示气泡信息。
	private void onLogicTriggered(IWorldLogic logic)
	{
		bubbleMessageContainer.AddMessage(logic.Name);
	}
}
