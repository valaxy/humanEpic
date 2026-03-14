using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// 负责监听关键交互并执行布局自动保存。
/// </summary>
public partial class FlowToolAutosaver : Node
{
	// 默认布局作用域键。
	private const string allLayoutScopeKey = "all";
	// 自动保存节流秒数。
	private const double autoSaveIntervalSeconds = 0.25d;
	// 画布根节点。
	private FlowToolCanvas flowToolCanvas = null!;
	// 布局存储器。
	private CanvasLayout layoutStore = new(allLayoutScopeKey);
	// 保存计时器。
	private double saveClockSeconds;
	// 最近布局指纹。
	private string lastLayoutFingerprint = string.Empty;

	/// <summary>
	/// 初始化并绑定 FlowToolCanvas 信号。
	/// </summary>
	public override void _Ready()
	{
		flowToolCanvas = GetParent<FlowToolCanvas>();
		flowToolCanvas.AutosavePulse += onAutosavePulse;
		flowToolCanvas.AutosaveForced += onAutosaveForced;
		flowToolCanvas.AutosaveSnapshotRequested += onAutosaveSnapshotRequested;
		flowToolCanvas.AutosaveCommitLayout += onAutosaveCommitLayout;
		flowToolCanvas.AutosaveScopeChanged += onAutosaveScopeChanged;
	}

	/// <summary>
	/// 推进自动保存时钟，命中节流窗口后尝试保存。
	/// </summary>
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

	/// <summary>
	/// 若布局发生变化则保存。
	/// </summary>
	private void onAutosaveForced()
	{
		saveIfChanged(forceSave: true);
	}

	// 处理切换前快照保存。
	private void onAutosaveSnapshotRequested()
	{
		if (flowToolCanvas.HasRenderedNodes == false)
		{
			return;
		}

		saveIfChanged(forceSave: true);
	}

	// 处理重载后的已知布局提交。
	private void onAutosaveCommitLayout()
	{
		IReadOnlyDictionary<string, Vector2> currentLayout = flowToolCanvas.CollectCurrentLayout();
		layoutStore.Save(currentLayout);
		lastLayoutFingerprint = createLayoutFingerprint(currentLayout);
	}

	// 处理布局作用域切换。
	private void onAutosaveScopeChanged(string layoutScopeKey)
	{
		layoutStore = new CanvasLayout(layoutScopeKey);
	}

	// 若布局发生变化则保存。
	private void saveIfChanged(bool forceSave = false)
	{
		IReadOnlyDictionary<string, Vector2> currentLayout = flowToolCanvas.CollectCurrentLayout();
		string currentFingerprint = createLayoutFingerprint(currentLayout);
		if (forceSave == false && currentFingerprint == lastLayoutFingerprint)
		{
			return;
		}

		layoutStore.Save(currentLayout);
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
