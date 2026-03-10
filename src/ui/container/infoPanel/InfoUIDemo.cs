using Godot;

/// <summary>
/// InfoUi 组件演示入口，展示常见信息面板能力。
/// </summary>
[GlobalClass]
public partial class InfoUIDemo : Control
{
	// InfoUi 组件实例。
	private InfoUI infoUi = null!;

	// 字典数据演示按钮。
	private Button showDictionaryButton = null!;

	// InfoData 演示按钮。
	private Button showInfoDataButton = null!;

	// 隐藏面板按钮。
	private Button hideButton = null!;

	/// <summary>
	/// 初始化 Demo 按钮并默认展示字典数据示例。
	/// </summary>
	public override void _Ready()
	{
		infoUi = GetNode<InfoUI>("%InfoUi");
		showDictionaryButton = GetNode<Button>("%ShowDictionaryButton");
		showInfoDataButton = GetNode<Button>("%ShowInfoDataButton");
		hideButton = GetNode<Button>("%HideButton");

		showDictionaryButton.Pressed += showDictionaryDemo;
		showInfoDataButton.Pressed += showInfoDataDemo;
		hideButton.Pressed += () => infoUi.HideInfo();

		infoUi.SetPositionOffset(20.0f);
		showDictionaryDemo();
	}

	// 展示结构化字典数据示例。
	private void showDictionaryDemo()
	{
		InfoData baseInfo = new InfoData();
		baseInfo.AddText("地块", "A-12");
		baseInfo.AddText("状态", "施工中");
		baseInfo.AddProgress("耐久", 0.73f, "73 / 100");

		InfoData productionInfo = new InfoData();
		productionInfo.AddProgress("粮食", 0.45f, "45%");
		productionInfo.AddProgress("矿石", 0.93f, "108%");

		InfoData payload = new InfoData();
		payload.AddGroup("基础信息", baseInfo);
		payload.AddGroup("产能信息", productionInfo);
		payload.AddText("备注", "点击上方按钮可切换不同数据来源");

		infoUi.ShowInfo("信息面板 Demo（Dictionary）", payload);
	}

	// 展示 InfoData 数据格式示例。
	private void showInfoDataDemo()
	{
		InfoData infoData = new InfoData();
		infoData.AddText("建筑", "仓储中心");
		infoData.AddNumber("等级", 4);

		InfoData workerGroup = new InfoData();
		workerGroup.AddText("负责人", "林涛");
		workerGroup.AddProgress("出勤率", 0.92f, "92%");

		InfoData logisticsGroup = new InfoData();
		logisticsGroup.AddProgress("装载率", 0.68f, "68%");
		logisticsGroup.AddProgress("拥堵指数", 0.12f, "112%");

		infoData.AddGroup("人员", workerGroup);
		infoData.AddGroup("物流", logisticsGroup);

		infoUi.ShowInfo("信息面板 Demo（InfoData）", infoData);
	}
}
