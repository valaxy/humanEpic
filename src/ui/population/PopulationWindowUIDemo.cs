using Godot;

/// <summary>
/// PopulationWindowUI 独立演示场景。
/// </summary>
[GlobalClass]
public partial class PopulationWindowUIDemo : Control
{
	// 演示入口按钮。
	private Button openButton = null!;

	// 人口窗口组件。
	private PopulationWindowUI populationWindowUi = null!;

	public override void _Ready()
	{
		openButton = GetNode<Button>("%OpenButton");
		populationWindowUi = GetNode<PopulationWindowUI>("%PopulationWindowUI");

		PopulationCollection demoPopulations = new PopulationCollection();
		demoPopulations.Add(new Population("港区工人", 160));
		demoPopulations.Add(new Population("山地农户", 230));
		demoPopulations.Add(new Population("学院学徒", 95));
		demoPopulations.Add(new Population("河运商贩", 72));

		populationWindowUi.SetupFromPopulations(demoPopulations);
		populationWindowUi.SetWindowVisible(false);

		openButton.Pressed += onOpenButtonPressed;
		populationWindowUi.WindowVisibilityChanged += onWindowVisibilityChanged;
	}

	// 切换人口窗口显示状态。
	private void onOpenButtonPressed()
	{
		populationWindowUi.SetWindowVisible(!populationWindowUi.IsWindowVisible);
	}

	// 同步按钮文案。
	private void onWindowVisibilityChanged(bool visible)
	{
		openButton.Text = visible ? "关闭人口演示" : "打开人口演示";
	}
}
