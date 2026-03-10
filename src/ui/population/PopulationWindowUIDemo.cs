using Godot;

/// <summary>
/// PopulationWindowUI 独立演示场景。
/// </summary>
[GlobalClass]
public partial class PopulationWindowUIDemo : Control
{
	// 人口窗口组件。
	private PopulationWindowUI populationWindowUi = null!;

	public override void _Ready()
	{
		populationWindowUi = GetNode<PopulationWindowUI>("%PopulationWindowUI");

		PopulationCollection demoPopulations = new PopulationCollection();
		demoPopulations.Add(new Population("港区工人", 160));
		demoPopulations.Add(new Population("山地农户", 230));
		demoPopulations.Add(new Population("学院学徒", 95));
		demoPopulations.Add(new Population("河运商贩", 72));

		populationWindowUi.SetupFromPopulations(demoPopulations);
		populationWindowUi.SetWindowVisible(true);
	}
}
