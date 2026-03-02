using System.Collections.Generic;
using Godot;

/// <summary>
/// 用于视觉层渲染的信息承载类，支持文本、进度和分组
/// </summary>
[GlobalClass]
public partial class InfoData : RefCounted
{
	private List<(string key, InfoEntryData value)> entries { get; } = new();

	/// <summary>
	/// 是否为空
	/// </summary>
	public bool IsEmpty => entries.Count == 0;

	/// <summary>
	/// 按顺序返回
	/// </summary>
	public IReadOnlyList<(string key, InfoEntryData value)> Entries => entries;

	/// <summary>
	/// 添加文本型信息
	/// </summary>
	public void AddText(string key, string value)
	{
		entries.Add((key, new InfoTextEntryData(value)));
	}

	/// <summary>
	/// 添加数值型信息
	/// </summary>
	public void AddNumber(string key, int value)
	{
		entries.Add((key, new InfoNumberEntryData(value)));
	}

	/// <summary>
	/// 添加浮点数值型信息。
	/// </summary>
	public void AddNumber(string key, double value)
	{
		entries.Add((key, new InfoNumberEntryData(value)));
	}

	/// <summary>
	/// 添加布尔型信息。
	/// </summary>
	public void AddBoolean(string key, bool value)
	{
		entries.Add((key, new InfoBooleanEntryData(value)));
	}

	/// <summary>
	/// 添加进度型信息
	/// </summary>
	public void AddProgress(string key, float progress, string valueText = "")
	{
		entries.Add((key, new InfoProgressEntryData(progress, valueText)));
	}

	/// <summary>
	/// 添加子分组信息
	/// </summary>
	public void AddGroup(string key, InfoData group)
	{
		entries.Add((key, new InfoGroupEntryData(group)));
	}
}