using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// flowtool 自动保存控制器。
/// </summary>
public partial class FlowToolAutosaveController : Node
{
	// 自动保存节流秒数。
	private const double autoSaveIntervalSeconds = 0.25d;
	// 当前作用域键。
	private string currentScopeKey = GameSystem.AllTopologyScopeKey;
	// 画布控制器。
	private FlowToolController flowToolController = null!;
	// 布局存储器。
	private readonly TopologyCanvasLayout layoutStore = new();
	// 保存计时器。
	private double saveClockSeconds;
	// 最近布局指纹。
	private string lastLayoutFingerprint = string.Empty;

	/// <summary>
	/// 初始化并绑定自动保存信号。
	/// </summary>
	public override void _Ready()
	{
		flowToolController = GetParent<FlowToolController>();
		flowToolController.AutosavePulse += onAutosavePulse;
		flowToolController.AutosaveForced += onAutosaveForced;
		flowToolController.AutosaveSnapshotRequested += onAutosaveSnapshotRequested;
		flowToolController.AutosaveCommitLayout += onAutosaveCommitLayout;
		flowToolController.AutosaveScopeChanged += onAutosaveScopeChanged;
	}

	// 推进自动保存时钟。
	private void onAutosavePulse(double delta)
	{
		saveClockSeconds += delta;
		if (saveClockSeconds < autoSaveIntervalSeconds)
		{
			return;
		}

		saveClockSeconds = 0d;
		saveIfChanged();
	}

	// 强制保存。
	private void onAutosaveForced()
	{
		saveIfChanged(forceSave: true);
	}

	// 作用域切换前保存快照。
	private void onAutosaveSnapshotRequested()
	{
		if (flowToolController.HasRenderedNodes == false)
		{
			return;
		}

		saveIfChanged(forceSave: true);
	}

	// 重载后提交已知布局。
	private void onAutosaveCommitLayout()
	{
		IReadOnlyDictionary<string, Vector2> currentLayout = flowToolController.CollectCurrentLayout();
		layoutStore.Save(currentScopeKey, currentLayout);
		lastLayoutFingerprint = createLayoutFingerprint(currentLayout);
	}

	// 处理布局作用域切换。
	private void onAutosaveScopeChanged(string layoutScopeKey)
	{
		currentScopeKey = string.IsNullOrWhiteSpace(layoutScopeKey)
			? GameSystem.AllTopologyScopeKey
			: layoutScopeKey;
	}

	// 若布局发生变化则保存。
	private void saveIfChanged(bool forceSave = false)
	{
		IReadOnlyDictionary<string, Vector2> currentLayout = flowToolController.CollectCurrentLayout();
		string currentFingerprint = createLayoutFingerprint(currentLayout);
		if (forceSave == false && currentFingerprint == lastLayoutFingerprint)
		{
			return;
		}

		layoutStore.Save(currentScopeKey, currentLayout);
		lastLayoutFingerprint = currentFingerprint;
	}

	// 生成布局变更检测指纹。
	private static string createLayoutFingerprint(IReadOnlyDictionary<string, Vector2> nodePositions)
	{
		IReadOnlyList<string> tokens = nodePositions
			.OrderBy(static pair => pair.Key, StringComparer.Ordinal)
			.Select(static pair => $"{pair.Key}:{pair.Value.X.ToString("F2", CultureInfo.InvariantCulture)},{pair.Value.Y.ToString("F2", CultureInfo.InvariantCulture)}")
			.ToList();
		return string.Join("|", tokens);
	}
}
